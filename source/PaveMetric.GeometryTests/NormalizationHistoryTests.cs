using PaveMetric;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;

internal static class NormalizationHistoryTests
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
        Console.WriteLine("Normalization history: automatic UI reprojection and undo/redo across prior edits passed.");
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
            var old = ErrorReprojectionTests.Correction(true);
            var photo = new PavementPhoto { Section = 1000, PerspectiveCorrection = old };
            var error = new SurfaceError { ErrorCode = 12, Left = -1.5, Right = .75, StartSection = 1002, EndSection = 1008 };
            photo.Errors.Add(error); project.Photos.Add(photo); project.MoveToFirstPhoto(); drawing.PavementPhoto = photo;
            drawing.LeftEdgeCurvePoints.AddRange(old.LeftEdgePoints.Select(p => new Pos(p.X, p.Y)));
            drawing.RightEdgeCurvePoints.AddRange(old.RightEdgePoints.Select(p => new Pos(p.X, p.Y)));
            drawing.Lines.Add(new Line { P0 = old.LeftEdge.P1, P1 = old.RightEdge.P1 });
            drawing.Lines.Add(new Line { P0 = old.LeftEdge.P0, P1 = old.RightEdge.P0 });
            drawing.Lines.Add(new Line { P0 = old.LeftEdge.P0, P1 = old.LeftEdge.P1 });
            drawing.Lines.Add(new Line { P0 = old.RightEdge.P0, P1 = old.RightEdge.P1 });
            ((TextBox)Field(main, "textBox_SectionLength")).Text = "10";
            ((TextBox)Field(main, "textBox_PavementWidth")).Text = "4";
            SurfaceError beforeEdit = error.Clone();
            error.Right = .8;
            SurfaceError afterEdit = error.Clone();
            Invoke(main, "RecordOperation", (Action)(() => error.CopyBoundsFrom(beforeEdit)),
                (Action)(() => error.CopyBoundsFrom(afterEdit)));
            Pos[] footprint = error.GetImageOutline(old, photo.Section);
            Check((bool)Invoke(main, "Normalize")!, "UI normalization must succeed.");
            var current = photo.PerspectiveCorrection;
            Check(!ReferenceEquals(old, current) && current.RoadModelVersion == 1 && current.Normalized, "New geometry must commit.");
            Check(error.ImageOutline?.Count == footprint.Length, "UI must automatically retain the old footprint.");
            for (int i = 0; i < footprint.Length; i++)
                Check(Math.Abs(footprint[i].X - error.ImageOutline![i].X) < 1e-9
                    && Math.Abs(footprint[i].Y - error.ImageOutline[i].Y) < 1e-9, "UI migration changed the image boundary.");
            // A later edge edit can invalidate the live correction. Redo must restore the
            // captured normalization state rather than this later mutable flag.
            current.Normalized = false;
            Invoke(main, "Undo");
            Check(ReferenceEquals(old, photo.PerspectiveCorrection) && old.Normalized
                && error.ImageOutline == null && error.Right == .8, "Undo must restore the old model and old error bounds.");
            Invoke(main, "Undo");
            Check(error.Right == .75, "Prior edit history must still use its original coordinate system.");
            Invoke(main, "Redo");
            Check(error.Right == .8, "Prior edit redo must remain available.");
            Invoke(main, "Redo");
            Check(ReferenceEquals(current, photo.PerspectiveCorrection) && current.Normalized
                && error.ImageOutline?.Count == footprint.Length, "Redo must restore the complete new normalization state.");
            SurfaceError normalized = error.Clone();
            Check((bool)Invoke(main, "Normalize")!, "Repeated UI normalization must succeed.");
            Check(Math.Abs(error.Left - normalized.Left) < 1e-8 && Math.Abs(error.StartSection - normalized.StartSection) < 1e-8,
                "Repeated UI normalization must not drift.");
            // Switching the checked button off must unlock boundary editing, also
            // when the displayed lines currently belong to the top-view bitmap.
            drawing.TopViewEnabled = true;
            drawing.Lines.Clear();
            for (int i = 0; i < 4; i++) drawing.Lines.Add(new Line { P0 = new Pos(-100, -100), P1 = new Pos(-50, -50) });
            Invoke(main, "button_Normalize_Click", main, EventArgs.Empty);
            Check(!photo.PerspectiveCorrection.Normalized && !drawing.TopViewEnabled,
                "Switching normalization off must return to editable perspective view.");
            Check(!((ToolStripButton)Field(main, "button_Normalize")).Checked
                && !((ToolStripButton)Field(main, "button_ToggleView")).Enabled,
                "Unchecked normalization must disable the stale top view.");
            Check(drawing.Lines.Count == 4 && drawing.Lines[0].P0.X == photo.PerspectiveCorrection.LeftEdge.P1.X,
                "Editing must retain photo-coordinate boundaries, not top-view lines.");
            Check(((ToolStripButton)Field(main, "button_NearDistance")).Enabled
                && ((ToolStripButton)Field(main, "button_FarDistance")).Enabled
                && ((ToolStripButton)Field(main, "button_LeftEdge")).Enabled
                && ((ToolStripButton)Field(main, "button_RightEdge")).Enabled,
                "Every boundary tool must be enabled.");
            Invoke(main, "Undo");
            Check(photo.PerspectiveCorrection.Normalized, "Switching off must be undoable.");
            Invoke(main, "Redo");
            Check(!photo.PerspectiveCorrection.Normalized, "Switching off must be redoable.");
            Invoke(main, "button_NearDistance_Click", main, EventArgs.Empty);
            Check(drawing.Command == 2, "Near boundary must be selectable after switching off.");
            drawing.Command = 0;
            drawing.LeftEdgeCurvePoints[2].X += 1;
            Invoke(main, "button_Normalize_Click", main, EventArgs.Empty);
            Check(photo.PerspectiveCorrection.Normalized && drawing.Lines.Count > 4
                && ((ToolStripButton)Field(main, "button_ToggleView")).Enabled,
                "The next click must rebuild the grid and enable top view.");
            for (int i = 0; i < footprint.Length; i++)
                Check(footprint[i].X == error.ImageOutline![i].X && footprint[i].Y == error.ImageOutline[i].Y,
                    "Switch off, edit and rebuild must preserve defect footprints.");
        }
        finally { ((Form)Field(main, "_errorTypeToolbox")).Dispose(); }
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
