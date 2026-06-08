using PPR;
using System.Drawing;

const double tolerance = 1e-8;

RunRoundTripTest(CreateCorrection(100, 400, 1100, 800), "inward edges");
RunRoundTripTest(CreateCorrection(400, 100, 800, 1100), "outward edges");
RunNetTest();
RunInvalidGeometryTest();
RunInvalidNormalizationTest();
RunTopViewTest();
RunTopViewWithReversedEdgesTest();

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
    PerspectiveCorrection correction = CreateCorrection(100, 350, 1100, 820);
    correction.LeftEdge = new Line { P0 = correction.LeftEdge.P1, P1 = correction.LeftEdge.P0 };
    correction.RightEdge = new Line { P0 = correction.RightEdge.P1, P1 = correction.RightEdge.P0 };

    using Bitmap source = new Bitmap(1200, 1100);
    using (Graphics graphics = Graphics.FromImage(source))
    {
        graphics.Clear(Color.Black);
        graphics.FillPolygon(Brushes.White, new Point[]
        {
            new Point(100, 1000),
            new Point(1100, 1000),
            new Point(820, 100),
            new Point(350, 100)
        });
    }

    using Bitmap? topView = correction.CreateTopView(source, 10);
    Assert(topView != null, "Top view must support reversed edge point order.");
    if (topView == null)
        return;

    Color center = topView.GetPixel(topView.Width / 2, topView.Height / 2);
    Assert(center.R > 200, "Reversed edge point order must still map the road center.");
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
