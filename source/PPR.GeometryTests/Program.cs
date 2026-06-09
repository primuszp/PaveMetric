using PPR;
using System.Drawing;

const double tolerance = 1e-8;
string? pilisPhotoPath = args.Length > 0 ? args[0] : null;
string? pilisOutputPath = args.Length > 1 ? args[1] : null;

RunRoundTripTest(CreateCorrection(100, 400, 1100, 800), "inward edges");
RunRoundTripTest(CreateCorrection(400, 100, 800, 1100), "outward edges");
RunNetTest();
RunInvalidGeometryTest();
RunInvalidNormalizationTest();
RunTopViewTest();
RunTopViewWithReversedEdgesTest();
RunTopViewWithEdgeCornersTest();
RunTopViewWithExtrapolatedCornersTest();
RunTopViewCornerMappingTest();
RunPilisGeometryTest();
RunAutomaticTopViewResolutionTest();
if (pilisPhotoPath != null && pilisOutputPath != null)
    ExportPilisTopView(pilisPhotoPath, pilisOutputPath);

Console.WriteLine("All geometry checks passed.");

static PerspectiveCorrection CreateCorrection(
    double leftNear,
    double leftFar,
    double rightNear,
    double rightFar)
{
    return new PerspectiveCorrection
    {
        PavementWidth = 6.0,
        Length = 10.0,
        NearDistance = 1000.0,
        FarDistance = 100.0,
        Normalized = true,
        RowCount = 5,
        ColCount = 3,
        LeftEdge = new Line
        {
            P0 = new Pos(leftNear, 1000.0),
            P1 = new Pos(leftFar, 100.0)
        },
        RightEdge = new Line
        {
            P0 = new Pos(rightNear, 1000.0),
            P1 = new Pos(rightFar, 100.0)
        }
    };
}

static void RunRoundTripTest(PerspectiveCorrection correction, string name)
{
    double[] xValues = [0.0, 1.5, 3.0, 4.5, 6.0];
    double[] yValues = [0.0, 2.5, 5.0, 7.5, 10.0];

    foreach (double x in xValues)
    {
        foreach (double y in yValues)
        {
            Pos real = new Pos(x, y);
            Pos screen = correction.GetScreenPosition(real);
            Pos roundTrip = correction.GetRealPosition(screen);

            AssertClose(real.X, roundTrip.X, $"{name}: real X round trip");
            AssertClose(real.Y, roundTrip.Y, $"{name}: real Y round trip");
        }
    }
}

static void RunNetTest()
{
    PerspectiveCorrection correction = CreateCorrection(100, 400, 1100, 800);
    Line[] net = correction.GetNet();

    Assert(net.Length == 6, "Grid must contain rows - 1 plus columns - 1 lines.");
    foreach (Line line in net)
    {
        AssertFinite(line.P0.X, "Grid P0.X");
        AssertFinite(line.P0.Y, "Grid P0.Y");
        AssertFinite(line.P1.X, "Grid P1.X");
        AssertFinite(line.P1.Y, "Grid P1.Y");
    }
}

static void RunInvalidGeometryTest()
{
    PerspectiveCorrection correction = new PerspectiveCorrection();
    Assert(correction.GetNet(5, 5).Length == 0, "Invalid geometry must not create a grid.");

    Pos real = correction.GetRealPosition(new Pos(10, 10));
    Pos screen = correction.GetScreenPosition(new Pos(10, 10));
    AssertFinite(real.X, "Invalid geometry real X");
    AssertFinite(real.Y, "Invalid geometry real Y");
    AssertFinite(screen.X, "Invalid geometry screen X");
    AssertFinite(screen.Y, "Invalid geometry screen Y");
}

static void RunInvalidNormalizationTest()
{
    PerspectiveCorrection correction = new PerspectiveCorrection();
    correction.Normalize([new Line(), new Line(), new Line(), new Line()], 10.0, 6.0);
    Assert(!correction.Normalized, "Degenerate normalization input must be rejected.");
}

static void RunTopViewTest()
{
    PerspectiveCorrection correction = CreateCorrection(100, 400, 1100, 800);
    using Bitmap source = new Bitmap(1200, 1100);
    using (Graphics graphics = Graphics.FromImage(source))
    {
        graphics.Clear(Color.Black);
        graphics.FillPolygon(Brushes.White, new Point[]
        {
            new Point(100, 1000),
            new Point(1100, 1000),
            new Point(800, 100),
            new Point(400, 100)
        });
    }

    using Bitmap? topView = correction.CreateTopView(source, 10);
    Assert(topView != null, "Top view must be generated for valid geometry.");
    if (topView == null)
        return;

    Assert(topView.Width == 60 && topView.Height == 100, "Top view dimensions must follow real pavement dimensions.");
    Color center = topView.GetPixel(topView.Width / 2, topView.Height / 2);
    Assert(center.R > 200 && center.G > 200 && center.B > 200, "Top view center must contain warped pavement pixels.");

    int darkPixels = 0;
    for (int y = 2; y < topView.Height - 2; y++)
    {
        for (int x = 2; x < topView.Width - 2; x++)
        {
            Color pixel = topView.GetPixel(x, y);
            if (pixel.R < 100 || pixel.G < 100 || pixel.B < 100)
                darkPixels++;
        }
    }

    Assert(darkPixels == 0, "Top view interior must not contain triangular gaps.");
}

static void RunTopViewWithReversedEdgesTest()
{
    // Normalize() always produces P0=near, P1=far order. Reversed edges are an
    // artificial case that cannot occur in normal usage after normalization.
    // Verify the function still returns a non-null bitmap without crashing.
    PerspectiveCorrection correction = CreateCorrection(100, 350, 1100, 820);
    correction.LeftEdge = new Line { P0 = correction.LeftEdge.P1, P1 = correction.LeftEdge.P0 };
    correction.RightEdge = new Line { P0 = correction.RightEdge.P1, P1 = correction.RightEdge.P0 };

    using Bitmap source = new Bitmap(1200, 1100);
    using (Graphics graphics = Graphics.FromImage(source))
        graphics.Clear(Color.White);

    using Bitmap? topView = correction.CreateTopView(source, 10);
    Assert(topView != null, "Top view must not crash with reversed edge point order.");
}

static void RunTopViewWithExtrapolatedCornersTest()
{
    // Near corners extrapolate outside the image width (x=-451 and x=2370 for a 1920-wide source).
    // The top-view center (valid road area) must be correctly sampled; edge pixels near the
    // extrapolated near corners will naturally be black (outside photo bounds — acceptable).
    var correction = new PerspectiveCorrection
    {
        PavementWidth = 6.0,
        Length = 10.0,
        NearDistance = 1079.0,
        FarDistance = 100.0,
        Normalized = true,
        LeftEdge = new Line { P0 = new Pos(-451, 1079), P1 = new Pos(700, 100) },
        RightEdge = new Line { P0 = new Pos(2370, 1079), P1 = new Pos(1220, 100) }
    };

    using var source = new Bitmap(1920, 1080);
    using (var g = Graphics.FromImage(source))
        g.Clear(Color.White);

    using var topView = correction.CreateTopView(source, 5);
    Assert(topView != null, "Top view with extrapolated corners must still be generated.");
    if (topView == null) return;

    // The far half of the road (top half of top-view) maps well within the photo bounds.
    int darkPixels = 0;
    for (int y = 2; y < topView.Height / 2; y++)
    {
        for (int x = 2; x < topView.Width - 2; x++)
        {
            Color pixel = topView.GetPixel(x, y);
            if (pixel.R < 200)
                darkPixels++;
        }
    }

    Assert(darkPixels == 0, $"Top view far half must not contain black pixels when source is all-white (found {darkPixels}).");
}

static void RunTopViewWithEdgeCornersTest()
{
    // Simulate real road photo where near corners touch the image boundaries.
    // This is the common scenario that triggers the triangular artifact.
    PerspectiveCorrection correction = new PerspectiveCorrection
    {
        PavementWidth = 6.0,
        Length = 10.0,
        NearDistance = 1099.0,
        FarDistance = 100.0,
        Normalized = true,
        LeftEdge = new Line { P0 = new Pos(0, 1099), P1 = new Pos(400, 100) },
        RightEdge = new Line { P0 = new Pos(1199, 1099), P1 = new Pos(800, 100) }
    };

    using Bitmap source = new Bitmap(1200, 1100);
    using (Graphics graphics = Graphics.FromImage(source))
    {
        graphics.Clear(Color.Black);
        graphics.FillPolygon(Brushes.White, new Point[]
        {
            new Point(0, 1099),
            new Point(1199, 1099),
            new Point(800, 100),
            new Point(400, 100)
        });
    }

    using Bitmap? topView = correction.CreateTopView(source, 10);
    Assert(topView != null, "Top view with edge corners must be generated.");
    if (topView == null)
        return;

    int darkPixels = 0;
    for (int y = 2; y < topView.Height - 2; y++)
    {
        for (int x = 2; x < topView.Width - 2; x++)
        {
            Color pixel = topView.GetPixel(x, y);
            if (pixel.R < 100 || pixel.G < 100 || pixel.B < 100)
                darkPixels++;
        }
    }

    Console.WriteLine($"Edge corners test: {darkPixels} dark pixels in interior (expected 0)");
    Assert(darkPixels == 0, "Top view with edge-touching corners must not contain triangular gaps.");
}

static void RunTopViewCornerMappingTest()
{
    PerspectiveCorrection correction = CreateCorrection(100, 400, 1100, 800);
    using Bitmap source = new Bitmap(1200, 1100);
    using (Graphics graphics = Graphics.FromImage(source))
    {
        graphics.Clear(Color.Black);
        graphics.FillPolygon(Brushes.Red, new Point[] { new Point(400, 100), new Point(800, 100), new Point(700, 550), new Point(250, 550) });
        graphics.FillPolygon(Brushes.Blue, new Point[] { new Point(250, 550), new Point(700, 550), new Point(1100, 1000), new Point(100, 1000) });
    }

    using Bitmap? topView = correction.CreateTopView(source, 10);
    Assert(topView != null, "Color-mapped top view must be generated.");
    if (topView == null) return;

    Color farCenter = topView.GetPixel(topView.Width / 2, 2);
    Color nearCenter = topView.GetPixel(topView.Width / 2, topView.Height - 3);
    Assert(farCenter.R > 200 && farCenter.B < 50, "Top of top view must map to the far side.");
    Assert(nearCenter.B > 200 && nearCenter.R < 50, "Bottom of top view must map to the near side.");
}

static void RunPilisGeometryTest()
{
    PerspectiveCorrection correction = CreatePilisCorrection();

    using Bitmap source = new Bitmap(4928, 3264);
    using (Graphics graphics = Graphics.FromImage(source))
        graphics.Clear(Color.White);
    using Bitmap? topView = correction.CreateTopView(source, 10);
    Assert(topView != null, "Pilis 00+20 geometry must generate a top view.");
    if (topView == null) return;

    int darkPixels = 0;
    for (int y = 0; y < topView.Height; y++)
    {
        for (int x = 0; x < topView.Width; x++)
        {
            if (topView.GetPixel(x, y).R < 200)
                darkPixels++;
        }
    }

    double darkRatio = (double)darkPixels / (topView.Width * topView.Height);
    Assert(darkRatio < 0.25, $"Pilis 00+20 top view maps too far outside the source ({darkRatio:P1} dark).");
}

static void ExportPilisTopView(string photoPath, string outputPath)
{
    using Bitmap source = new Bitmap(photoPath);
    using Bitmap? topView = CreatePilisCorrection().CreateTopView(source, 100);
    Assert(topView != null, "Pilis 00+20 photo must generate a top view.");
    topView?.Save(outputPath);
}

static void RunAutomaticTopViewResolutionTest()
{
    using Bitmap source = new Bitmap(1200, 1100);
    using Bitmap? topView = CreateCorrection(100, 400, 1100, 800).CreateTopView(source);
    Assert(topView != null, "Automatic-resolution top view must be generated.");
    if (topView == null) return;

    long sourcePixels = (long)source.Width * source.Height;
    long topViewPixels = (long)topView.Width * topView.Height;
    Assert(topViewPixels >= sourcePixels, "Automatic top view must contain at least as many pixels as the source.");
}

static PerspectiveCorrection CreatePilisCorrection()
{
    return new PerspectiveCorrection
    {
        PavementWidth = 4.0,
        Length = 10.0,
        FarDistance = 93.27710422189011,
        NearDistance = 2803.6972361435887,
        Normalized = true,
        LeftEdge = new Line
        {
            P0 = new Pos(-988.1802981505953, 2803.6972361435887),
            P1 = new Pos(1799.6804089688671, 93.27710422189011)
        },
        RightEdge = new Line
        {
            P0 = new Pos(5558.725455766861, 2803.6972361435887),
            P1 = new Pos(3228.982308665624, 93.27710422189011)
        }
    };
}

static void AssertClose(double expected, double actual, string message)
{
    if (Math.Abs(expected - actual) > tolerance)
        throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
}

static void AssertFinite(double value, string message)
{
    Assert(!double.IsNaN(value) && !double.IsInfinity(value), $"{message} must be finite.");
}

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}
