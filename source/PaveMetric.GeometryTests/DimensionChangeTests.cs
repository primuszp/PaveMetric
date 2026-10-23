using PaveMetric;

/// <summary>
/// Re-normalizing with a new section length / pavement width must keep every recorded
/// error on the same photo pixels (the old geometry may not be edited in place first).
/// </summary>
internal static class DimensionChangeTests
{
    public static void Run()
    {
        // curved road, error stored only as road coordinates (no outline yet)
        var curved = ErrorReprojectionTests.Correction(false);
        CheckKeepsFootprint(curved, new SurfaceError { ErrorCode = 12, Left = -1.5, Right = .75, StartSection = 1002, EndSection = 1008 });

        // straight road
        var straight = new PerspectiveCorrection { RowCount = 20, ColCount = 24 };
        straight.Normalize(new[]
        {
            new Line { P0 = new Pos(820, 300), P1 = new Pos(1100, 300) },   // far
            new Line { P0 = new Pos(300, 1000), P1 = new Pos(1600, 1000) }, // near
            new Line { P0 = new Pos(300, 1000), P1 = new Pos(820, 300) },   // left
            new Line { P0 = new Pos(1600, 1000), P1 = new Pos(1100, 300) }, // right
        }, 10, 4);
        Check(straight.Normalized, "Straight fixture must normalize.");
        CheckKeepsFootprint(straight, new SurfaceError { ErrorCode = 14, Left = -1, Right = 1, StartSection = 1003, EndSection = 1006 });

        // the cached curved model must follow a direct dimension change
        var edited = ErrorReprojectionTests.Correction(false);
        var probe = new Pos(3, 6);
        edited.GetScreenPosition(probe); // build the cache with the old size
        edited.PavementWidth = 5;
        edited.Length = 12;
        Pos direct = edited.GetScreenPosition(probe);
        Pos rebuilt = ErrorReprojectionTests.Correction(false).WithDimensions(12, 5).GetScreenPosition(probe);
        Close(rebuilt.X, direct.X, 1e-6, "Cached model X after size change");
        Close(rebuilt.Y, direct.Y, 1e-6, "Cached model Y after size change");
    }

    private static void CheckKeepsFootprint(PerspectiveCorrection old, SurfaceError error)
    {
        var photo = new PavementPhoto { Section = 1000, PerspectiveCorrection = old };
        photo.Errors.Add(error);
        Pos[] before = error.GetImageOutline(old, photo.Section);
        double oldWidth = error.Right - error.Left;

        PerspectiveCorrection replacement = old.WithDimensions(10, 5);
        Check(old.PavementWidth == 4 && old.Length == 10, "WithDimensions must not edit the current geometry.");
        Check(photo.TryReplaceCorrection(replacement, out string reason), reason);

        Pos[] after = error.GetImageOutline(photo.PerspectiveCorrection, photo.Section);
        Check(before.Length == after.Length, "Footprint vertex count changed.");
        for (int i = 0; i < before.Length; i++)
        {
            Close(before[i].X, after[i].X, 1e-6, "Footprint X after re-normalization");
            Close(before[i].Y, after[i].Y, 1e-6, "Footprint Y after re-normalization");
        }
        Check(error.Right - error.Left > oldWidth * 1.1, "Road coordinates must be rescaled to the new width.");
    }

    private static void Close(double expected, double actual, double tolerance, string message)
    {
        if (!double.IsFinite(actual) || Math.Abs(expected - actual) > tolerance)
            throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
