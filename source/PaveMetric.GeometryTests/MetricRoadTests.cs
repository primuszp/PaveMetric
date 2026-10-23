using PaveMetric;
using System.Xml.Serialization;

internal static class MetricRoadTests
{
    private const double Width = 4, Length = 10;

    public static void Run()
    {
        foreach (double radius in new[] { 100.0, 40.0, 20.0, 10.0 })
            foreach (double direction in new[] { -1.0, 1.0 })
            {
                Pos Road(double x, double s)
                {
                    double r = radius + direction * (Width / 2 - x);
                    return new Pos(direction * (radius - r * Math.Cos(s / radius)), 6 + r * Math.Sin(s / radius));
                }
                var correction = Create(Road, 13, 9);
                Check(correction.UsesMetricRoadPlane, "Circular road must use the fitted metric plane.");
                double maxStation = 0;
                foreach (double x in new[] { 0.0, 0.5, 2.0, 3.5, 4.0 })
                    for (int i = 1; i < 20; i++)
                    {
                        double s = i / 2.0;
                        Pos p = correction.GetRealPosition(Project(Road(x, s)));
                        maxStation = Math.Max(maxStation, Math.Abs(p.Y - s));
                        Close(x, p.X, 0.04, "Circular lateral position");
                        Close(s, p.Y, 0.09, "Circular station");
                        Pos roundTrip = correction.GetRealPosition(correction.GetScreenPosition(new Pos(x, s)));
                        Close(x, roundTrip.X, 1e-4, "Metric lateral round trip");
                        Close(s, roundTrip.Y, 1e-4, $"Metric station round trip x={x}, s={s}, R={radius}, turn={direction}");
                    }
                foreach (double x0 in new[] { 0.0, 1.0, 2.0, 3.0 })
                {
                    double expected = 6 * (1 + direction * (1.5 - x0) / radius);
                    Close(expected, correction.GetRoadArea(x0, x0 + 1, 2, 8), expected * 0.02, "Circular physical patch area");
                }
                double full = correction.GetRoadArea(0, Width, 0, Length);
                Close(Width * Length, full, 0.4, "Circular full-road area");
                double sum = correction.GetRoadArea(0, 1, 2, 5) + correction.GetRoadArea(0, 1, 5, 8);
                Close(correction.GetRoadArea(0, 1, 2, 8), sum, 0.001, "Area subdivision");
                Console.WriteLine($"Metric circle R={radius}, direction={direction}: max station error={maxStation:F4} m");
                foreach (double initialWidth in new[] { 2.0, 6.0 })
                {
                    var calibration = new PerspectiveCorrection();
                    calibration.NormalizeCurved(correction.LeftEdgePoints, correction.RightEdgePoints, Length, initialWidth);
                    Check(calibration.TryCalibrateWidthFromImageReference(Project(Road(1, 5)), Project(Road(2, 5)), 1, out double inferred),
                        $"Transverse calibration must remain available (R={radius}, turn={direction}, initial width={initialWidth}).");
                    Close(Width, inferred, 0.15, "Width calibration with an approximate initial width");
                }
                Check(!correction.TryCalibrateWidthFromImageReference(Project(Road(1, 5)), Project(Road(1, 6)), 1, out _),
                    "A longitudinal reference must not calibrate width.");
                if (radius == 10 && direction == 1) CheckExportAndSerialization(correction);
            if (radius == 10 && direction == 1)
            {
                using var image = new System.Drawing.Bitmap(1920, 1280);
                Pos marker = Project(Road(2, 5));
                using (var graphics = System.Drawing.Graphics.FromImage(image))
                {
                    graphics.Clear(System.Drawing.Color.Black);
                    graphics.FillEllipse(System.Drawing.Brushes.Magenta, (float)marker.X - 12, (float)marker.Y - 12, 24, 24);
                }
                using var topView = correction.CreateTopView(image, 20, sharpen: false);
                Check(topView != null, "Metric top view must render.");
                var color = topView!.GetPixel(topView.Width / 2, topView.Height / 2);
                Check(color.R > 200 && color.B > 200 && color.G < 30, "Metric top view must place the known center marker correctly.");
            }
            }
        RunSShape();
        RunLegacyAndInvalidChecks();
    }

    private static PerspectiveCorrection Create(Func<double, double, Pos> road, int leftCount, int rightCount)
    {
        List<Pos> Edge(double x, int count) => Enumerable.Range(0, count)
            .Select(i => Project(road(x, Length * i / (count - 1)))).ToList();
        var correction = new PerspectiveCorrection();
        correction.NormalizeCurved(Edge(0, leftCount), Edge(Width, rightCount), Length, Width);
        return correction;
    }

    private static Pos Project(Pos p)
    {
        const double pitch = 0.25, yaw = 0.12, roll = -0.15;
        double x = p.X * Math.Cos(yaw) - p.Y * Math.Sin(yaw);
        double z = p.X * Math.Sin(yaw) + p.Y * Math.Cos(yaw);
        double yCamera = -2.5 * Math.Cos(pitch) + z * Math.Sin(pitch);
        double zCamera = 2.5 * Math.Sin(pitch) + z * Math.Cos(pitch);
        double u = 1500 * x / zCamera, v = -1500 * yCamera / zCamera;
        return new Pos(960 + u * Math.Cos(roll) - v * Math.Sin(roll),
            640 + u * Math.Sin(roll) + v * Math.Cos(roll));
    }

    private static void RunSShape()
    {
        double Heading(double s) => 0.45 * Math.Sin(2 * Math.PI * s / Length);
        Pos Road(double x, double s)
        {
            double cx = 0, cy = 6;
            const int count = 1024;
            double step = s / count;
            for (int i = 0; i < count; i++)
            {
                double heading = Heading((i + 0.5) * step);
                cx += Math.Sin(heading) * step; cy += Math.Cos(heading) * step;
            }
            return new Pos(cx + (x - Width / 2) * Math.Cos(Heading(s)),
                cy - (x - Width / 2) * Math.Sin(Heading(s)));
        }
        var correction = Create(Road, 33, 25);
        Check(correction.UsesMetricRoadPlane, "S-shaped offset curves must use the metric plane.");
        double maxStation = 0;
        for (int i = 1; i < 20; i++)
            foreach (double x in new[] { 0.5, 2, 3.5 })
            {
                double s = i / 2.0;
                Pos real = correction.GetRealPosition(Project(Road(x, s)));
                maxStation = Math.Max(maxStation, Math.Abs(real.Y - s));
                Close(s, real.Y, 0.1, "S-shaped station");
                Close(x, real.X, 0.04, "S-shaped lateral position");
            }
        double expected = 6 + 1.5 * (Heading(8) - Heading(2));
        Close(expected, correction.GetRoadArea(0, 1, 2, 8), expected * 0.02, "S-shaped physical patch area");
        Console.WriteLine($"Metric S-shaped road: max station error={maxStation:F4} m");
    }

    private static void CheckExportAndSerialization(PerspectiveCorrection correction)
    {
        var project = new Project();
        var photo = new PavementPhoto { Section = 100, PerspectiveCorrection = correction };
        photo.Errors.Add(new SurfaceError { ErrorCode = 10, Left = -2, Right = -1, StartSection = 102, EndSection = 108 });
        project.Photos.Add(photo);
        string path = Path.GetTempFileName();
        try
        {
            project.ExportErrors(path);
            string[] columns = File.ReadAllLines(path)[1].Split('\t');
            Check(columns[2] == "7", "Export must round the physical outer-curve area (6.9 m²) to 7, rather than 6.");
        }
        finally { File.Delete(path); }

        var serializer = new XmlSerializer(typeof(PerspectiveCorrection));
        using var writer = new StringWriter(); serializer.Serialize(writer, correction);
        using var reader = new StringReader(writer.ToString());
        var restored = (PerspectiveCorrection)serializer.Deserialize(reader)!;
        Check(restored.RoadModelVersion == 1 && restored.UsesMetricRoadPlane, "Metric model version must survive XML round trip.");
        Close(correction.GetRoadArea(0, 1, 2, 8), restored.GetRoadArea(0, 1, 2, 8), 1e-8, "Serialized metric area");
        string legacyXml = writer.ToString().Replace("<RoadModelVersion>1</RoadModelVersion>", "");
        using var legacyReader = new StringReader(legacyXml);
        var legacy = (PerspectiveCorrection)serializer.Deserialize(legacyReader)!;
        Check(legacy.RoadModelVersion == 0 && !legacy.UsesMetricRoadPlane, "Unversioned projects must retain legacy coordinates.");
        Close(6, legacy.GetRoadArea(0, 1, 2, 8), 1e-8, "Legacy area compatibility");
        legacy.NormalizeCurved(legacy.LeftEdgePoints, legacy.RightEdgePoints, Length, Width);
        Check(legacy.RoadModelVersion == 1 && legacy.UsesMetricRoadPlane, "Explicit normalization must upgrade a legacy photo.");
    }

    private static void RunLegacyAndInvalidChecks()
    {
        var correction = new PerspectiveCorrection();
        correction.NormalizeCurved(new List<Pos> { new(0, 100), new(200, 0) },
            new List<Pos> { new(200, 100), new(0, 0) }, Length, Width);
        Check(!correction.UsesMetricRoadPlane, "Crossing edges must not be accepted as a metric fit.");
        Close(0, correction.GetRoadArea(1, 0, 0, 1), 0, "Reversed area bounds");
        Close(0, correction.GetRoadArea(0, 1, double.NaN, 1), 0, "Non-finite area bounds");
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Close(double expected, double actual, double tolerance, string message)
    {
        if (!double.IsFinite(actual) || Math.Abs(expected - actual) > tolerance)
            throw new InvalidOperationException($"{message}: expected {expected}, got {actual}, tolerance {tolerance}.");
    }
}

