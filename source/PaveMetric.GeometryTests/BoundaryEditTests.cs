using PaveMetric;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;

/// <summary>
/// Setting the far/near boundary again on a normalized photo must mark it for
/// re-normalization, and the next Normalize click must rebuild the geometry with the new
/// boundary while keeping recorded errors on the same photo pixels.
/// </summary>
internal static class BoundaryEditTests
{
    public static void Run()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { RunOnUiThread(); } catch (Exception exception) { failure = exception; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start(); thread.Join();
        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
        Console.WriteLine("Boundary edit: re-set far boundary, re-normalize and error reprojection passed.");
    }

    private static object Field(object target, string name) => target.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;
    private static object? Invoke(object target, string name, params object[] args) => target.GetType()
        .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, args);

    private static void RunOnUiThread()
    {
        using var main = new MainForm();
        Invoke(main, "MainForm_Load", main, EventArgs.Empty);
        try
        {
            var project = (Project)Field(main, "_project");
            var drawing = (DrawingArea)Field(main, "drawingArea");
            var old = ErrorReprojectionTests.Correction(false);
            var photo = new PavementPhoto { Section = 1000, PerspectiveCorrection = old };
            var error = new SurfaceError { ErrorCode = 12, Left = -1.5, Right = .75, StartSection = 1002, EndSection = 1008 };
            photo.Errors.Add(error); project.Photos.Add(photo); project.MoveToFirstPhoto(); drawing.PavementPhoto = photo;
            drawing.LeftEdgeCurvePoints.AddRange(old.LeftEdgePoints.Select(p => new Pos(p.X, p.Y)));
            drawing.RightEdgeCurvePoints.AddRange(old.RightEdgePoints.Select(p => new Pos(p.X, p.Y)));
            double farY = (old.LeftEdge.P1.Y + old.RightEdge.P1.Y) / 2, nearY = (old.LeftEdge.P0.Y + old.RightEdge.P0.Y) / 2;
            drawing.Lines.Add(new Line { P0 = new Pos(0, farY), P1 = new Pos(1920, farY) });
            drawing.Lines.Add(new Line { P0 = new Pos(0, nearY), P1 = new Pos(1920, nearY) });
            drawing.Lines.Add(new Line { P0 = old.LeftEdge.P0, P1 = old.LeftEdge.P1 });
            drawing.Lines.Add(new Line { P0 = old.RightEdge.P0, P1 = old.RightEdge.P1 });
            ((TextBox)Field(main, "textBox_SectionLength")).Text = "10";
            ((TextBox)Field(main, "textBox_PavementWidth")).Text = "4";
            Pos[] footprint = error.GetImageOutline(old, photo.Section);

            // the user picks "Távoli határ" and clicks a bit further up the photo
            double newFarY = farY - 15;
            drawing.Lines[0].P0.Y = newFarY;
            drawing.Lines[0].P1.Y = newFarY;
            Invoke(main, "drawingArea_OnCommandStateChanged", drawing, new CommandEventArgs { Command = 1, SubCommand = 1 });
            Check(!photo.PerspectiveCorrection.Normalized,
                "A new far boundary must mark the photo for re-normalization.");
            Check(Math.Abs(drawing.LeftEdgeCurvePoints[^1].Y - newFarY) < 1e-6
                && Math.Abs(drawing.RightEdgeCurvePoints[^1].Y - newFarY) < 1e-6,
                "Edges must end on the moved far boundary.");

            Invoke(main, "button_Normalize_Click", main, EventArgs.Empty);
            var current = photo.PerspectiveCorrection;
            Check(current.Normalized && !ReferenceEquals(old, current), "The next Normalize click must rebuild the geometry.");
            Check(Math.Abs(current.FarDistance - newFarY) < 1e-6, "The new geometry must use the new far boundary.");
            Check(error.ImageOutline?.Count == footprint.Length, "Errors must be reprojected.");
            for (int i = 0; i < footprint.Length; i++)
                Check(Math.Abs(footprint[i].X - error.ImageOutline![i].X) < 1e-9
                    && Math.Abs(footprint[i].Y - error.ImageOutline[i].Y) < 1e-9,
                    "Re-normalizing with a new boundary moved an error on the photo.");
            Check(Math.Abs(error.StartSection - 1002) > 1e-3, "Stations must follow the new boundary.");
        }
        finally { ((Form)Field(main, "_errorTypeToolbox")).Dispose(); }
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
