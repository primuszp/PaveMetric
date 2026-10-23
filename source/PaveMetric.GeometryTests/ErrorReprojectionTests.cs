using PaveMetric;
using System.Xml.Serialization;

internal static class ErrorReprojectionTests
{
    internal static Pos Project(double lateral, double station)
    {
        double radius = 12 - lateral;
        double x = 10 - radius * Math.Cos(station / 10), z = 6 + radius * Math.Sin(station / 10);
        double yc = -2.5 * Math.Cos(.25) + z * Math.Sin(.25), zc = 2.5 * Math.Sin(.25) + z * Math.Cos(.25);
        return new Pos(960 + 1500 * x / zc, 640 - 1500 * yc / zc);
    }

    internal static PerspectiveCorrection Correction(bool legacy)
    {
        var correction = new PerspectiveCorrection { RowCount = 20, ColCount = 24 };
        correction.NormalizeCurved(Enumerable.Range(0, 13).Select(i => Project(0, 10.0 * i / 12)).ToList(),
            Enumerable.Range(0, 9).Select(i => Project(4, 10.0 * i / 8)).ToList(), 10, 4);
        if (legacy) correction.RoadModelVersion = 0;
        return correction;
    }

    public static void Run()
    {
        var old = Correction(true);
        var photo = new PavementPhoto { Section = 1000, PerspectiveCorrection = old };
        var error = new SurfaceError { ErrorCode = 12, Left = -1.5, Right = .75, StartSection = 1002, EndSection = 1008 };
        photo.Errors.Add(error);
        Pos[] originalImage = error.GetImageOutline(old, photo.Section);
        var replacement = Correction(false);
        // Editing edges marks the old model stale, but it still defines the old footprint.
        old.Normalized = false;
        Check(photo.TryReplaceCorrection(replacement, out string reason), reason);
        Check(ReferenceEquals(error, photo.Errors[0]) && error.ErrorCode == 12, "Migration must preserve error identity and code.");
        SamePolygon(originalImage, error.GetImageOutline(replacement, photo.Section));
        Check(error.ImageOutline?.Count >= 3, "Migrated errors need a saved footprint.");
        Check(Math.Abs(error.StartSection - 1002) > .05, "Legacy stations must actually be reprojected.");
        double trueArea = GroundArea(originalImage);
        double migratedArea = error.GetArea(replacement, photo.Section);
        Close(trueArea, migratedArea, trueArea * .02, "Migrated physical area");
        Check(error.Contains(replacement, photo.Section, old.GetScreenPosition(new Pos(1, 5))), "Original interior must remain selectable.");
        bool foundOutside = false;
        foreach (double x in new[] { .0001, .001, .01, .1, .5, .9, .99, .999, .9999 })
            foreach (double y in new[] { .0001, .001, .01, .1, .5, .9, .99, .999, .9999 })
            {
                var p = new Pos(error.Left + 2 + (error.Right - error.Left) * x,
                    error.StartSection - photo.Section + (error.EndSection - error.StartSection) * y);
                foundOutside |= !error.Contains(replacement, photo.Section, replacement.GetScreenPosition(p));
            }
        Check(foundOutside, "Selection must follow the outline, not its larger bounding rectangle.");

        SurfaceError firstMigration = error.Clone();
        Check(photo.TryReplaceCorrection(Correction(false), out reason), reason);
        SamePolygon(originalImage, error.ImageOutline!);
        SameBounds(firstMigration, error);
        Close(migratedArea, error.GetArea(photo.PerspectiveCorrection, photo.Section), 1e-8, "Repeated migration area");

        var serializer = new XmlSerializer(typeof(PavementPhoto));
        using var writer = new StringWriter(); serializer.Serialize(writer, photo);
        using var reader = new StringReader(writer.ToString());
        var restored = (PavementPhoto)serializer.Deserialize(reader)!;
        SamePolygon(originalImage, restored.Errors[0].ImageOutline!);
        SameBounds(error, restored.Errors[0]);
        Close(migratedArea, restored.Errors[0].GetArea(restored.PerspectiveCorrection, restored.Section), 1e-8, "Saved migration area");

        // A triangular footprint makes it unambiguous whether the export uses the
        // preserved polygon or its much larger road-coordinate bounding rectangle.
        var triangle = new SurfaceError { ErrorCode = 10,
            ImageOutline = new List<Pos> { originalImage[0], originalImage[24], originalImage[25] } };
        var exportPhoto = new PavementPhoto { Section = photo.Section, PerspectiveCorrection = photo.PerspectiveCorrection };
        exportPhoto.Errors.Add(triangle);
        Check(exportPhoto.TryReplaceCorrection(Correction(false), out reason), reason);
        double triangleArea = triangle.GetArea(exportPhoto.PerspectiveCorrection, exportPhoto.Section);
        Close(GroundArea(triangle.ImageOutline!), triangleArea, triangleArea * .02, "Export footprint ground area");
        double boundingArea = exportPhoto.PerspectiveCorrection.GetRoadArea(triangle.Left + 2, triangle.Right + 2,
            triangle.StartSection - exportPhoto.Section, triangle.EndSection - exportPhoto.Section);
        Check(boundingArea > triangleArea * 1.5, "Triangle fixture must distinguish outline area from bounding area.");
        var project = new Project(); project.Photos.Add(exportPhoto);
        string exportPath = Path.GetTempFileName();
        try
        {
            project.ExportErrors(exportPath);
            double totalArea = exportPhoto.PerspectiveCorrection.GetRoadArea(0, 4, 0, 10);
            string expected = (40 * triangleArea / totalArea).ToString("0");
            Check(File.ReadAllLines(exportPath)[1].Split('\t')[2] == expected, "Export must use the preserved footprint's physical area.");
        }
        finally { File.Delete(exportPath); }

        var moved = error.Clone();
        moved.Left += .1; moved.Right += .1; moved.StartSection += .15; moved.EndSection += .15;
        moved.TransformOutlineFrom(error, photo.PerspectiveCorrection, photo.Section);
        for (int i = 0; i < error.ImageOutline!.Count; i++)
        {
            Pos a = photo.PerspectiveCorrection.GetRealPosition(error.ImageOutline[i]);
            Pos b = photo.PerspectiveCorrection.GetRealPosition(moved.ImageOutline![i]);
            Close(a.X + .1, b.X, 1e-4, "Moved outline lateral position");
            Close(a.Y + .15, b.Y, 1e-4, "Moved outline station");
        }
        moved.CopyBoundsFrom(error);
        SamePolygon(error.ImageOutline, moved.ImageOutline!);
        moved.ImageOutline![0].X += 1;
        Check(Math.Abs(moved.ImageOutline[0].X - error.ImageOutline[0].X - 1) < 1e-8, "Undo snapshots must deep-copy outlines.");

        var failed = new PavementPhoto { Section = 1000, PerspectiveCorrection = Correction(true) };
        var intact = new SurfaceError { ErrorCode = 10, Left = -1, Right = 0, StartSection = 1002, EndSection = 1005 };
        failed.Errors.Add(intact);
        failed.Errors.Add(new SurfaceError
        {
            Left = 0,
            Right = 1,
            StartSection = 1002,
            EndSection = 1005,
            ImageOutline = new List<Pos> { new(double.NaN, 10), new(1, 2), new(3, 4) }
        });
        var previous = failed.PerspectiveCorrection;
        SurfaceError snapshot = intact.Clone();
        Check(!failed.TryReplaceCorrection(Correction(false), out reason), "An invalid late error must abort the migration.");
        Check(ReferenceEquals(previous, failed.PerspectiveCorrection) && intact.ImageOutline == null,
            "Aborted migration must leave the model and every earlier error untouched.");
        SameBounds(snapshot, intact);
        Check(!failed.TryReplaceCorrection(new PerspectiveCorrection(), out reason), "Invalid replacement must be rejected.");
        RunLegacyBoundaryChecks();
        RunOutsideCurvedEdgesChecks();
        Console.WriteLine("Error reprojection: footprint, area, selection, repeat, XML, edit, boundary overshoots, zero-area entries and atomic failure passed.");
    }

    private static void RunOutsideCurvedEdgesChecks()
    {
        // 1+230: formerly straight calibration, then a left-hand bend traced on
        // the photo. The old shoulder footprint remains outside the new left edge.
        Pos Pixel(double x, double y) => new((x - 309) * 3696 / 1344, (y - 99) * 2448 / 889);
        var old = new PerspectiveCorrection();
        old.NormalizeCurved(new() { new(438.5296028089339, 2293.4410874035852), new(1459.9368861842281, 1211.0542647223035) },
            new() { new(3548.410646843622, 2293.4410874035852), new(2281.033892989921, 1211.0542647223035) }, 10, 3.8);
        var photo = new PavementPhoto { Section = 1230, PerspectiveCorrection = old };
        photo.Errors.Add(new SurfaceError { ErrorCode = 14, Left = -1.891, Right = -1.540, StartSection = 1230.50, EndSection = 1239.51 });
        photo.Errors.Add(new SurfaceError { ErrorCode = 10, Left = -1.895, Right = -1.536, StartSection = 1239.52, EndSection = 1239.94 });
        photo.Errors.Add(new SurfaceError { ErrorCode = 10, Left = .635, Right = 1.887, StartSection = 1229.995, EndSection = 1239.913 });
        var next = new PerspectiveCorrection();
        next.NormalizeCurved(new() { old.LeftEdge.P0, Pixel(650, 840), Pixel(733, 736), Pixel(800, 635), Pixel(839, 547) },
            new() { old.RightEdge.P0, Pixel(1495, 835), Pixel(1410, 736), Pixel(1275, 630), Pixel(1138, 533) }, 10, 3.8);
        Check(!next.UsesMetricRoadPlane, "Fixture must cover the non-metric curve fallback as well.");
        var outlines = photo.Errors.Select(e => e.GetImageOutline(old, photo.Section)).ToArray();
        Check(photo.TryReplaceCorrection(next, out string reason), reason);
        for (int i = 0; i < outlines.Length; i++)
        {
            SamePolygon(outlines[i], photo.Errors[i].ImageOutline!);
            foreach (Pos point in outlines[i])
            {
                Pos back = next.GetScreenPosition(next.GetRealPosition(point));
                Close(point.X, back.X, 1e-6, "Outside curved edge photo X");
                Close(point.Y, back.Y, 1e-6, "Outside curved edge photo Y");
            }
            Check(photo.Errors[i].StartSection > 1229 && photo.Errors[i].EndSection < 1242,
                "Terminal extension must not select a remote collapsed-width branch.");
            Check(double.IsFinite(photo.Errors[i].GetArea(next, photo.Section)), "Outside area must remain finite.");
        }
        var serializer = new XmlSerializer(typeof(PavementPhoto));
        using var writer = new StringWriter(); serializer.Serialize(writer, photo);
        using var reader = new StringReader(writer.ToString());
        var restored = (PavementPhoto)serializer.Deserialize(reader)!;
        Check(restored.TryReplaceCorrection(next, out reason), reason);
        for (int i = 0; i < outlines.Length; i++)
        {
            SamePolygon(outlines[i], restored.Errors[i].ImageOutline!);
            SameBounds(photo.Errors[i], restored.Errors[i]);
            Close(photo.Errors[i].GetArea(next, photo.Section), restored.Errors[i].GetArea(next, photo.Section), 1e-8,
                "Outside area after save/reload/re-normalize");
        }
        var metric = Correction(false);
        Check(metric.UsesMetricRoadPlane, "Metric curve fixture must fit.");
        foreach (double x in new[] { -1.0, -.25, 4.25, 5.0 })
        foreach (double y in new[] { -.15, 0, 2, 5, 8, 10, 10.15 })
        {
            Pos point = metric.GetScreenPosition(new Pos(x, y));
            Pos result = metric.GetRealPosition(point);
            // Extended transverse lines can intersect outside the road. The saved
            // photo footprint, rather than a particular exterior station, is invariant.
            Pos back = metric.GetScreenPosition(result);
            Close(point.X, back.X, 1e-6, "Metric outside photo X");
            Close(point.Y, back.Y, 1e-6, "Metric outside photo Y");
        }
        Console.WriteLine("Strong curve exterior footprints: 1+230 fallback, metric offsets, end tangents and XML repeat passed.");
    }

    private static void RunLegacyBoundaryChecks()
    {
        var old = new PerspectiveCorrection { PavementWidth = 4, Length = 10, NearDistance = 1000, FarDistance = 100,
            LeftEdge = new Line { P0 = new Pos(100, 1000), P1 = new Pos(400, 100) },
            RightEdge = new Line { P0 = new Pos(1100, 1000), P1 = new Pos(800, 100) }, Normalized = true };
        var photo = new PavementPhoto { Section = 10, PerspectiveCorrection = old };
        photo.Errors.Add(new SurfaceError { ErrorCode = 10, Left = -1, Right = 1, StartSection = 9.86, EndSection = 20.14 });
        photo.Errors.Add(new SurfaceError { ErrorCode = 15, Left = .3, Right = .3, StartSection = 16, EndSection = 16 });
        photo.Errors.Add(new SurfaceError { ErrorCode = 10, Left = -.5, Right = .5, StartSection = 19, EndSection = 19 });
        var footprints = photo.Errors.Select(e => e.GetImageOutline(old, photo.Section)).ToArray();
        var next = new PerspectiveCorrection();
        next.Normalize(new[] { new Line { P0 = old.LeftEdge.P1, P1 = old.RightEdge.P1 },
            new Line { P0 = old.LeftEdge.P0, P1 = old.RightEdge.P0 }, old.LeftEdge, old.RightEdge }, 10, 4);
        Check(photo.TryReplaceCorrection(next, out string reason), reason);
        for (int i = 0; i < photo.Errors.Count; i++) SamePolygon(footprints[i], photo.Errors[i].ImageOutline!);
        Close(9.86, photo.Errors[0].StartSection, 1e-6, "Legacy start overshoot");
        Close(20.14, photo.Errors[0].EndSection, 1e-6, "Legacy end overshoot");
        Close(0, photo.Errors[1].GetArea(next, photo.Section), 1e-8, "Legacy point area");
        Close(0, photo.Errors[2].GetArea(next, photo.Section), 1e-8, "Legacy line area");
        Check(photo.Errors.Count == 3, "Zero-area legacy entries must be retained, not deleted.");
    }

    private static double GroundArea(IReadOnlyList<Pos> polygon)
    {
        Pos Unproject(Pos p)
        {
            double v = (640 - p.Y) / 1500;
            double z = 2.5 * (Math.Cos(.25) + v * Math.Sin(.25)) / (Math.Sin(.25) - v * Math.Cos(.25));
            return new Pos((p.X - 960) / 1500 * (2.5 * Math.Sin(.25) + z * Math.Cos(.25)), z);
        }
        double area = 0;
        for (int i = 0; i < polygon.Count; i++)
        {
            Pos a = Unproject(polygon[i]), b = Unproject(polygon[(i + 1) % polygon.Count]);
            area += a.X * b.Y - a.Y * b.X;
        }
        return Math.Abs(area) / 2;
    }

    private static void SamePolygon(IReadOnlyList<Pos> a, IReadOnlyList<Pos> b)
    {
        Check(a.Count == b.Count, "Footprint vertex count changed.");
        for (int i = 0; i < a.Count; i++) { Close(a[i].X, b[i].X, 1e-9, "Photo X"); Close(a[i].Y, b[i].Y, 1e-9, "Photo Y"); }
    }
    private static void SameBounds(SurfaceError a, SurfaceError b)
    {
        Close(a.Left, b.Left, 1e-8, "Left"); Close(a.Right, b.Right, 1e-8, "Right");
        Close(a.StartSection, b.StartSection, 1e-8, "Start"); Close(a.EndSection, b.EndSection, 1e-8, "End");
    }
    private static void Close(double expected, double actual, double tolerance, string message)
    {
        if (!double.IsFinite(actual) || Math.Abs(expected - actual) > tolerance)
            throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
