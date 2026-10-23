using PaveMetric;
using System.Globalization;

// Independent ground truth: a flat circular road projected through a pinhole camera.
internal static class MeasurementAudit
{
    private const double Width = 4.0;
    private const double Length = 10.0;

    public static void Run()
    {
        Console.WriteLine("Synthetic audit: width=4 m, centerline length=10 m, camera height=2.5 m,");
        Console.WriteLine("near station=6 m ahead, pitch=0.25 rad, focal length=1500 px.");
        Console.WriteLine("Control points: left=13, right=9. Stations tested: 0.5..9.5 m, step=0.5 m.");
        Console.WriteLine("Position errors: maxima over x=0.5, 2, 3.5 m, not guaranteed bounds.");
        Console.WriteLine("Longitudinal check compares 1 m centerline station intervals.");
        Console.WriteLine("Legacy width uses the original pixel-ratio formula and a transverse 1 m base.");
        Console.WriteLine("radius_m,roll_rad,max_x_error_m,max_station_error_m,max_1m_transverse_error_m,max_1m_station_interval_error_m,legacy_width_min_m,legacy_width_max_m,new_width_min_m,new_width_max_m,rejected_references,legacy_max_station_error_m,max_patch_area_relative_error");
        foreach (double radius in new[] { double.PositiveInfinity, 100.0, 40.0, 20.0, 10.0 })
        foreach (double roll in new[] { 0.0, 0.15 })
        {
            var correction = new PerspectiveCorrection();
            correction.NormalizeCurved(Edge(0, 13, radius, roll), Edge(Width, 9, radius, roll), Length, Width);
            if (!correction.Normalized)
                throw new InvalidOperationException("Audit geometry did not normalize.");
            var legacy = new PerspectiveCorrection();
            legacy.NormalizeCurved(Edge(0, 13, radius, roll), Edge(Width, 9, radius, roll), Length, Width);
            legacy.RoadModelVersion = 0;
            double legacyStationError = 0, maxAreaError = 0;
            double maxX = 0, maxStation = 0, maxTransverse = 0, maxInterval = 0;
            var legacyWidths = new List<double>();
            var calibratedWidths = new List<double>();
            int rejected = 0;
            for (int i = 1; i < 20; i++)
            {
                double station = i / 2.0;
                foreach (double x in new[] { 0.5, 2, 3.5 })
                {
                    Pos actual = correction.GetRealPosition(Project(x, station, radius, roll));
                    RequireFinite(actual.X, actual.Y);
                    maxX = Math.Max(maxX, Math.Abs(actual.X - x));
                    maxStation = Math.Max(maxStation, Math.Abs(actual.Y - station));
                    legacyStationError = Math.Max(legacyStationError,
                        Math.Abs(legacy.GetRealPosition(Project(x, station, radius, roll)).Y - station));
                }
                Pos a = Project(1, station, radius, roll);
                Pos b = Project(2, station, radius, roll);
                double legacyWidth = Math.Abs(correction.RightEdge.P1.X - correction.LeftEdge.P1.X) / Distance(a, b);
                RequireFinite(legacyWidth);
                legacyWidths.Add(legacyWidth);
                Pos realA = correction.GetRealPosition(a);
                Pos realB = correction.GetRealPosition(b);
                if (correction.TryGetWidthFromTransverseReference(realA, realB, 1, out double calibratedWidth))
                    calibratedWidths.Add(calibratedWidth);
                else
                    rejected++;
                maxTransverse = Math.Max(maxTransverse, Math.Abs(Distance(realA, realB) - 1));
                if (station <= 8.5)
                {
                    Pos next = correction.GetRealPosition(Project(2, station + 1, radius, roll));
                    maxInterval = Math.Max(maxInterval, Math.Abs(Distance(realB, next) - 1));
                }
            }
            for (int x = 0; x < 4; x++)
            {
                double expected = 6 * (1 + (double.IsPositiveInfinity(radius) ? 0 : (1.5 - x) / radius));
                maxAreaError = Math.Max(maxAreaError, Math.Abs(correction.GetRoadArea(x, x + 1, 2, 8) / expected - 1));
            }
            // Curved errors are diagnostic results, not asserted as acceptable accuracy.
            if (double.IsPositiveInfinity(radius) && Math.Max(maxX, maxStation) > 0.001)
                throw new InvalidOperationException("Straight reference exceeds 1 mm position error.");
            Console.WriteLine(string.Join(",", new[]
            {
                radius, roll, maxX, maxStation, maxTransverse, maxInterval,
                legacyWidths.Min(), legacyWidths.Max(),
                calibratedWidths.DefaultIfEmpty(double.NaN).Min(),
                calibratedWidths.DefaultIfEmpty(double.NaN).Max(), rejected, legacyStationError, maxAreaError
            }.Select(value => value.ToString("F6", CultureInfo.InvariantCulture))));
        }
        Console.WriteLine("Audit completed. Finite outputs and the straight reference passed; curved accuracy is reported, not certified.");
    }

    private static List<Pos> Edge(double x, int count, double radius, double roll) =>
        Enumerable.Range(0, count).Select(i => Project(x, Length * i / (count - 1), radius, roll)).ToList();

    private static Pos Project(double x, double station, double radius, double roll)
    {
        if (double.IsPositiveInfinity(radius))
            return Ground(x - Width / 2, 6 + station, roll);
        double edgeRadius = radius + Width / 2 - x;
        return Ground(radius - edgeRadius * Math.Cos(station / radius),
            6 + edgeRadius * Math.Sin(station / radius), roll);
    }

    private static Pos Ground(double x, double z, double roll)
    {
        const double pitch = 0.25, height = 2.5, focal = 1500;
        double yCamera = -height * Math.Cos(pitch) + z * Math.Sin(pitch);
        double zCamera = height * Math.Sin(pitch) + z * Math.Cos(pitch);
        double u = focal * x / zCamera, v = -focal * yCamera / zCamera;
        return new Pos(960 + u * Math.Cos(roll) - v * Math.Sin(roll),
            640 + u * Math.Sin(roll) + v * Math.Cos(roll));
    }

    private static double Distance(Pos a, Pos b) =>
        Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

    private static void RequireFinite(params double[] values)
    {
        if (values.Any(value => !double.IsFinite(value)))
            throw new InvalidOperationException("Audit produced a non-finite value.");
    }
}
