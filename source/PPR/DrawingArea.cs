using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Collections.Generic;

namespace PPR
{
    public partial class DrawingArea : UserControl
    {
        private Graphics controlDC;

        double x0 = 0.0;
        double y0 = 0.0;
        double zoom = 1.0;
        double x_Mouse = 0.0;
        double y_Mouse = 0.0;
        double x_MouseDown = 0.0;
        double y_MouseDown = 0.0;
        double x0_MouseDown = 0.0;
        double y0_MouseDown = 0.0;

        public Pos AreaPos0 = new Pos();
        public Pos AreaPos1 = new Pos();
        public Color AreaColor = Color.FromArgb(64, 255, 128, 128);
        public SurfaceError ActualError = null;
        public PavementPhoto PavementPhoto { get; set; }

        private Image photo = null;
        private Line measure_ruler = null;
        private MouseButtons mouseButton = MouseButtons.None;
        private int activeErrorHandle = -1;
        private List<Pos> activeCurveEditPoints;
        private int activeCurveHandle = -1;
        private int activeCurveInsertSegment = -1;
        private Pos activeCurveInsertMidpoint;
        private List<Pos> hoverCurveEditPoints;
        private int hoverCurveHandle = -1;
        private bool hoverCurveMidpoint;
        private SurfaceError errorBeforeEdit;
        private List<Pos> curvePointsBeforeEdit;
        private bool topViewEnabled;
        private bool fastPaint;
        private readonly Timer fastPaintTimer;

        private int ErrorHandleSize => Math.Max(9, (int)Math.Round(9 * DeviceDpi / 96.0));
        private int CurveHandleRadius => Math.Max(4, (int)Math.Round(4 * DeviceDpi / 96.0));
        private int CurveHitRadius => Math.Max(8, (int)Math.Round(8 * DeviceDpi / 96.0));

        public List<Line> Lines = new List<Line>();
        public List<Pos> LeftEdgeCurvePoints = new List<Pos>();
        public List<Pos> RightEdgeCurvePoints = new List<Pos>();
        public List<ErrorLayerControl> ErrorLayers = new List<ErrorLayerControl>();

        int command = 0;
        int subCommand = 0;

        public int Command
        {
            get { return command; }
            set
            {
                command = value;
                subCommand = 0;
                if (command != 0)
                    ClearErrorSelection();
            }
        }

        public bool RenderNeeded
        {
            get => false;
            set
            {
                if (value)
                    Invalidate();
            }
        }

        public Image Photo
        {
            get { return photo; }
            set
            {
                photo = value;
                Render();
            }
        }

        public Action<double> MeasureAction { get; set; }

        public delegate void MouseMoveEventHandler(object sender, MouseMoveEventArgs e);
        public event MouseMoveEventHandler OnDrawingAreaMouseMove;

        public delegate void CommandEventHAndler(object sender, CommandEventArgs e);
        public event CommandEventHAndler OnCommandStateChanged;
        public event EventHandler<ErrorEditEventArgs> ErrorEditCompleted;
        public event EventHandler<CurveEditEventArgs> CurveEditCompleted;

        public SurfaceError SelectedError { get; private set; }
        public bool TopViewEnabled
        {
            get => topViewEnabled;
            set
            {
                topViewEnabled = value;
                ClearErrorSelection();
                Invalidate();
            }
        }

        public DrawingArea()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint, true);
            fastPaintTimer = new Timer { Interval = 200 };
            fastPaintTimer.Tick += (sender, e) =>
            {
                fastPaintTimer.Stop();
                fastPaint = false;
                Invalidate();
            };
            Resize += new EventHandler(DrawingArea_Resize);
            MouseDown += new MouseEventHandler(DrawingArea_MouseDown);
            MouseUp += new MouseEventHandler(DrawingArea_MouseUp);
            MouseWheel += new MouseEventHandler(DrawingArea_MouseWheel);
            MouseMove += new MouseEventHandler(DrawingArea_MouseMove);
            Load += new EventHandler(DrawingArea_Load);
            Disposed += new EventHandler(DrawingArea_Disposed);
        }

        void DrawingArea_Load(object sender, EventArgs e)
        {
            ClearLines();
            Invalidate();
        }

        public void ClearLines()
        {
            Lines.Clear();
            for (int i = 0; i < 4; i++)
                Lines.Add(new Line());

            Lines[0].LineColor = Color.LightGreen;
            Lines[0].LineWidth = 2;
            Lines[1].LineColor = Color.LightGreen;
            Lines[1].LineWidth = 2;
            Lines[2].LineColor = Color.Pink;
            Lines[2].LineWidth = 2;
            Lines[3].LineColor = Color.Pink;
            Lines[3].LineWidth = 2;
        }

        public void ResetView()
        {
            x0 = 0.0;
            y0 = 0.0;
            zoom = 1.0;
            Invalidate();
        }

        public void FitImageToView()
        {
            if (photo == null || Width <= 0 || Height <= 0)
            {
                ResetView();
                return;
            }

            zoom = Math.Min((double)Width / photo.Width, (double)Height / photo.Height);
            if (zoom <= 0.0 || double.IsNaN(zoom) || double.IsInfinity(zoom))
                zoom = 1.0;

            x0 = -(Width / zoom - photo.Width) / 2.0;
            y0 = -(Height / zoom - photo.Height) / 2.0;
            Invalidate();
        }

        public Pos ViewToRealPosition(Pos viewPosition)
        {
            if (!topViewEnabled)
                return PavementPhoto?.PerspectiveCorrection.GetRealPosition(viewPosition) ?? new Pos();

            if (PavementPhoto == null || photo == null)
                return new Pos();

            PerspectiveCorrection correction = PavementPhoto.PerspectiveCorrection;
            double w = Math.Max(1, photo.Width - 1);
            double h = Math.Max(1, photo.Height - 1);
            return new Pos(
                correction.PavementWidth * viewPosition.X / w,
                correction.TopViewFarRealY
                    - (correction.TopViewFarRealY - correction.TopViewNearRealY) * viewPosition.Y / h);
        }

        public Pos RealToViewPosition(Pos realPosition)
        {
            if (!topViewEnabled)
                return PavementPhoto?.PerspectiveCorrection.GetScreenPosition(realPosition) ?? new Pos();

            if (PavementPhoto == null || photo == null)
                return new Pos();

            PerspectiveCorrection correction = PavementPhoto.PerspectiveCorrection;
            double w = Math.Max(1, photo.Width - 1);
            double h = Math.Max(1, photo.Height - 1);
            return new Pos(
                w * realPosition.X / correction.PavementWidth,
                h * (correction.TopViewFarRealY - realPosition.Y)
                    / (correction.TopViewFarRealY - correction.TopViewNearRealY));
        }

        void DrawingArea_MouseUp(object sender, MouseEventArgs e)
        {
            if (mouseButton == MouseButtons.Left && activeErrorHandle >= 0 && SelectedError != null)
            {
                ErrorEditCompleted?.Invoke(this, new ErrorEditEventArgs(
                    SelectedError,
                    errorBeforeEdit,
                    SelectedError.Clone()));
            }
            else if (mouseButton == MouseButtons.Left && command == 10 && subCommand == 1)
            {
                AreaPos1.X = x0 + e.X / zoom;
                AreaPos1.Y = y0 + e.Y / zoom;
                subCommand = 0;
                RaiseCommandStateChanged(10, 1, x0 + e.X / zoom, y0 + e.Y / zoom);
            }
            else if (mouseButton == MouseButtons.Left
                && activeCurveInsertSegment >= 0
                && activeCurveEditPoints != null)
            {
                activeCurveEditPoints.Insert(activeCurveInsertSegment + 1, new Pos(x0 + e.X / zoom, y0 + e.Y / zoom));
                RaiseCurveEditCompleted();
                RenderNeeded = true;
            }
            else if (mouseButton == MouseButtons.Left && activeCurveHandle >= 0 && activeCurveEditPoints != null)
            {
                RaiseCurveEditCompleted();
            }

            ClearActiveCurveEdit();
            activeErrorHandle = -1;
            errorBeforeEdit = null;
            mouseButton = MouseButtons.None;
        }

        private void RaiseCurveEditCompleted()
        {
            if (curvePointsBeforeEdit == null || activeCurveEditPoints == null)
                return;

            if (!CurvePointsEqual(curvePointsBeforeEdit, activeCurveEditPoints))
                CurveEditCompleted?.Invoke(this, new CurveEditEventArgs(
                    activeCurveEditPoints,
                    curvePointsBeforeEdit,
                    SnapshotPoints(activeCurveEditPoints)));
        }

        private void ClearActiveCurveEdit()
        {
            activeCurveHandle = -1;
            activeCurveInsertSegment = -1;
            activeCurveInsertMidpoint = null;
            activeCurveEditPoints = null;
            curvePointsBeforeEdit = null;
        }

        /// <summary>
        /// Cancels the drag in progress (curve handle, curve insert, or error handle) and
        /// restores the pre-drag state. Bound to Escape in the main form.
        /// </summary>
        public void CancelActiveEdit()
        {
            bool changed = false;
            if (activeCurveEditPoints != null && curvePointsBeforeEdit != null)
            {
                activeCurveEditPoints.Clear();
                activeCurveEditPoints.AddRange(curvePointsBeforeEdit);
                changed = true;
            }

            if (activeErrorHandle >= 0 && SelectedError != null && errorBeforeEdit != null)
            {
                SelectedError.CopyBoundsFrom(errorBeforeEdit);
                changed = true;
            }

            ClearActiveCurveEdit();
            activeErrorHandle = -1;
            errorBeforeEdit = null;
            mouseButton = MouseButtons.None;
            if (changed)
                Invalidate();
        }

        private static List<Pos> SnapshotPoints(List<Pos> points)
        {
            List<Pos> result = new List<Pos>(points.Count);
            foreach (Pos point in points)
                result.Add(new Pos(point.X, point.Y));
            return result;
        }

        private static bool CurvePointsEqual(List<Pos> first, List<Pos> second)
        {
            if (first.Count != second.Count)
                return false;

            for (int i = 0; i < first.Count; i++)
            {
                if (Math.Abs(first[i].X - second[i].X) > 1e-9 || Math.Abs(first[i].Y - second[i].Y) > 1e-9)
                    return false;
            }

            return true;
        }

        void DrawingArea_MouseMove(object sender, MouseEventArgs e)
        {
            switch (mouseButton)
            {
                case MouseButtons.None:
                    x_Mouse = x0 + e.X / zoom;
                    y_Mouse = y0 + e.Y / zoom;

                    if (OnDrawingAreaMouseMove != null)
                    {
                        OnDrawingAreaMouseMove(this, new MouseMoveEventArgs(x_Mouse, y_Mouse));
                    }

                    if (command == 0)
                    {
                        int hoveredHandle = HitTestSelectedHandle(e.Location);
                        int curveHandle = HitTestCurveHandle(e.Location, out List<Pos> hoveredPoints, out bool hoverMidpoint);
                        if (!ReferenceEquals(hoveredPoints, hoverCurveEditPoints)
                            || curveHandle != hoverCurveHandle
                            || hoverMidpoint != hoverCurveMidpoint)
                        {
                            hoverCurveEditPoints = hoveredPoints;
                            hoverCurveHandle = curveHandle;
                            hoverCurveMidpoint = hoverMidpoint;
                            Invalidate();
                        }

                        Cursor = curveHandle >= 0
                            ? Cursors.SizeAll
                            : hoveredHandle == 0 || hoveredHandle == 2
                                ? Cursors.SizeNWSE
                                : hoveredHandle == 1 || hoveredHandle == 3
                                    ? Cursors.SizeNESW
                                    : Cursors.Default;
                    }
                    else if (hoverCurveEditPoints != null || hoverCurveHandle >= 0)
                    {
                        hoverCurveEditPoints = null;
                        hoverCurveHandle = -1;
                        hoverCurveMidpoint = false;
                        Invalidate();
                    }

                    switch (command)
                    {
                        case 3:
                            if (subCommand == 1)
                            {
                                Lines[2].P1.X = x_Mouse;
                                Lines[2].P1.Y = y_Mouse;
                                RenderNeeded = true;
                            }
                            break;
                        case 4:
                            if (subCommand == 1)
                            {
                                Lines[3].P1.X = x_Mouse;
                                Lines[3].P1.Y = y_Mouse;
                                RenderNeeded = true;
                            }
                            break;
                        case 5:
                            if (subCommand == 1)
                            {
                                measure_ruler.P1.X = x_Mouse;
                                measure_ruler.P1.Y = y_Mouse;
                                RenderNeeded = true;
                            }
                            break;
                        case 10:
                            if (subCommand == 1)
                            {
                                AreaPos1.X = x_Mouse;
                                AreaPos1.Y = y_Mouse;
                                RenderNeeded = true;
                            }
                            break;
                    }
                    break;
                case MouseButtons.Right:
                    double dx = e.X - x_MouseDown;
                    double dy = e.Y - y_MouseDown;
                    x0 = x0_MouseDown - dx / zoom;
                    y0 = y0_MouseDown - dy / zoom;
                    BeginFastPaint();
                    RenderNeeded = true;
                    break;
                case MouseButtons.Left:
                    if (activeErrorHandle >= 0 && SelectedError != null)
                    {
                        UpdateSelectedErrorFromHandle(activeErrorHandle, x0 + e.X / zoom, y0 + e.Y / zoom);
                        RenderNeeded = true;
                    }
                    else if (activeCurveHandle >= 0 && activeCurveEditPoints != null)
                    {
                        activeCurveEditPoints[activeCurveHandle].X = x0 + e.X / zoom;
                        if (activeCurveHandle == 0)
                            activeCurveEditPoints[activeCurveHandle].Y = Lines[1].P0.Y;
                        else if (activeCurveHandle == activeCurveEditPoints.Count - 1)
                            activeCurveEditPoints[activeCurveHandle].Y = Lines[0].P0.Y;
                        else
                            activeCurveEditPoints[activeCurveHandle].Y = y0 + e.Y / zoom;
                        RenderNeeded = true;
                    }
                    else if (activeCurveInsertSegment >= 0 && activeCurveEditPoints != null)
                    {
                        x_Mouse = x0 + e.X / zoom;
                        y_Mouse = y0 + e.Y / zoom;
                        RenderNeeded = true;
                    }
                    else if (command == 10 && subCommand == 1)
                    {
                        AreaPos1.X = x0 + e.X / zoom;
                        AreaPos1.Y = y0 + e.Y / zoom;
                        RenderNeeded = true;
                    }
                    break;
            }
        }

        void DrawingArea_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta > 0)
            {
                zoom *= 1.333;
            }
            else
            {
                zoom /= 1.333;
            }

            if (zoom > 16.0) zoom = 16.0;
            if (zoom < 0.05) zoom = 0.05;

            x0 = x_Mouse - e.X / zoom;
            y0 = y_Mouse - e.Y / zoom;

            BeginFastPaint();
            RenderNeeded = true;
        }

        /// <summary>
        /// Switches to fast (low-quality) image interpolation during pan/zoom; a short timer
        /// repaints in high quality once the interaction pauses.
        /// </summary>
        private void BeginFastPaint()
        {
            fastPaint = true;
            fastPaintTimer.Stop();
            fastPaintTimer.Start();
        }

        void DrawingArea_MouseDown(object sender, MouseEventArgs e)
        {
            mouseButton = e.Button;
            int prevCommand = command;
            int prevSubCommand = subCommand;

            switch (mouseButton)
            {
                case MouseButtons.Left:
                    x_Mouse = x0 + e.X / zoom;
                    y_Mouse = y0 + e.Y / zoom;

                    if (command == 0 && PavementPhoto != null)
                    {
                        activeCurveHandle = HitTestCurveHandle(e.Location, out activeCurveEditPoints, out bool midpoint);
                        if (activeCurveHandle >= 0)
                        {
                            curvePointsBeforeEdit = SnapshotPoints(activeCurveEditPoints);
                            if (midpoint)
                            {
                                activeCurveInsertSegment = activeCurveHandle;
                                PointF midpointPoint = GetCurveSegmentMidpoint(
                                    activeCurveEditPoints,
                                    activeCurveInsertSegment,
                                    ToControlPoint(activeCurveEditPoints[activeCurveInsertSegment]),
                                    ToControlPoint(activeCurveEditPoints[activeCurveInsertSegment + 1]));
                                activeCurveInsertMidpoint = new Pos(x0 + midpointPoint.X / zoom, y0 + midpointPoint.Y / zoom);
                                activeCurveHandle = -1;
                            }

                            break;
                        }

                        activeErrorHandle = HitTestSelectedHandle(e.Location);
                        if (activeErrorHandle >= 0 && SelectedError != null)
                        {
                            errorBeforeEdit = SelectedError.Clone();
                            break;
                        }

                        SelectErrorAt(x_Mouse, y_Mouse);
                        break;
                    }

                    switch (command)
                    {
                        case 1:
                            if (photo == null) break;
                            Lines[0].P0.X = 0.0;
                            Lines[0].P1.X = photo.Width;
                            Lines[0].P0.Y = y_Mouse;
                            Lines[0].P1.Y = y_Mouse;
                            command = 0;
                            RenderNeeded = true;
                            break;
                        case 2:
                            if (photo == null) break;
                            Lines[1].P0.X = 0.0;
                            Lines[1].P1.X = photo.Width;
                            Lines[1].P0.Y = y_Mouse;
                            Lines[1].P1.Y = y_Mouse;
                            command = 0;
                            RenderNeeded = true;
                            break;
                        case 3:
                            if (subCommand == 0)
                            {
                                Lines[2].P0.X = x_Mouse;
                                Lines[2].P0.Y = y_Mouse;
                                Lines[2].P1.X = x_Mouse;
                                Lines[2].P1.Y = y_Mouse;
                                subCommand = 1;
                            }
                            else
                            {
                                command = 0;
                                subCommand = 0;
                            }
                            RenderNeeded = true;
                            break;
                        case 4:
                            if (subCommand == 0)
                            {
                                Lines[3].P0.X = x_Mouse;
                                Lines[3].P0.Y = y_Mouse;
                                Lines[3].P1.X = x_Mouse;
                                Lines[3].P1.Y = y_Mouse;
                                subCommand = 1;
                            }
                            else
                            {
                                command = 0;
                                subCommand = 0;
                            }
                            RenderNeeded = true;
                            break;
                        case 5:
                            if (subCommand == 0)
                            {
                                measure_ruler = new Line();
                                measure_ruler.P0.X = x_Mouse;
                                measure_ruler.P0.Y = y_Mouse;
                                measure_ruler.P1.X = x_Mouse;
                                measure_ruler.P1.Y = y_Mouse;
                                subCommand = 1;
                            }
                            else
                            {
                                command = 0;
                                subCommand = 0;
                                CalcMeasureLength();
                            }
                            RenderNeeded = true;
                            break;
                        case 10:
                            subCommand = 1;
                            AreaPos0.X = x_Mouse;
                            AreaPos0.Y = y_Mouse;
                            AreaPos1.X = x_Mouse;
                            AreaPos1.Y = y_Mouse;
                            break;
                    }

                    if (OnCommandStateChanged != null)
                    {
                        CommandEventArgs eventArgs = new CommandEventArgs();
                        eventArgs.Command = prevCommand;
                        eventArgs.SubCommand = prevSubCommand;
                        eventArgs.MouseX = x_Mouse;
                        eventArgs.MouseY = y_Mouse;
                        OnCommandStateChanged(this, eventArgs);
                    }
                    break;
                case MouseButtons.Right:
                    if (command == 0 && PavementPhoto != null)
                    {
                        int handle = HitTestCurveHandle(e.Location, out List<Pos> points, out bool midpoint);
                        if (handle > 0 && !midpoint && points != null && handle < points.Count - 1)
                        {
                            List<Pos> before = SnapshotPoints(points);
                            points.RemoveAt(handle);
                            CurveEditCompleted?.Invoke(this, new CurveEditEventArgs(points, before, SnapshotPoints(points)));
                            mouseButton = MouseButtons.None;
                            RenderNeeded = true;
                            break;
                        }
                    }

                    x0_MouseDown = x0;
                    y0_MouseDown = y0;
                    x_MouseDown = e.X;
                    y_MouseDown = e.Y;
                    break;
            }

        }

        private void RaiseCommandStateChanged(int currentCommand, int currentSubCommand, double mouseX, double mouseY)
        {
            OnCommandStateChanged?.Invoke(this, new CommandEventArgs
            {
                Command = currentCommand,
                SubCommand = currentSubCommand,
                MouseX = mouseX,
                MouseY = mouseY
            });
        }

        public void SelectError(SurfaceError error)
        {
            SelectedError = error;
            Invalidate();
        }

        public void ClearErrorSelection()
        {
            SelectedError = null;
            activeErrorHandle = -1;
            errorBeforeEdit = null;
            Invalidate();
        }

        private void DrawingArea_Resize(object sender, EventArgs e)
        {
            if (Width > 0 && Height > 0)
                Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            controlDC = e.Graphics;
            if (fastPaint)
            {
                controlDC.SmoothingMode = SmoothingMode.HighSpeed;
                controlDC.InterpolationMode = InterpolationMode.NearestNeighbor;
            }
            else
            {
                controlDC.SmoothingMode = SmoothingMode.AntiAlias;
                controlDC.InterpolationMode = InterpolationMode.HighQualityBilinear;
            }

            try
            {
                RenderFrame();
            }
            finally
            {
                controlDC = null;
            }
        }

        private void Clear()
        {
            controlDC.Clear(Theme.Canvas);
        }

        private void RenderErrors()
        {
            if (PavementPhoto != null)
            {
                double pavementWidth2 = PavementPhoto.PerspectiveCorrection.PavementWidth / 2.0;
                double startSection = PavementPhoto.Section;

                foreach (SurfaceError myError in PavementPhoto.Errors)
                {
                    double leftReal = myError.Left + pavementWidth2;
                    double rightReal = myError.Right + pavementWidth2;
                    double nearReal = myError.StartSection - startSection;
                    double farReal = myError.EndSection - startSection;

                    Pos[] screenPositions = topViewEnabled
                        ? new Pos[]
                        {
                            RealToViewPosition(new Pos(leftReal, nearReal)),
                            RealToViewPosition(new Pos(rightReal, nearReal)),
                            RealToViewPosition(new Pos(rightReal, farReal)),
                            RealToViewPosition(new Pos(leftReal, farReal))
                        }
                        : PavementPhoto.PerspectiveCorrection.GetScreenAreaPolygon(leftReal, rightReal, nearReal, farReal);

                    Point[] points = new Point[screenPositions.Length];
                    for (int i = 0; i < screenPositions.Length; i++)
                    {
                        points[i].X = (int)((screenPositions[i].X - x0) * zoom);
                        points[i].Y = (int)((screenPositions[i].Y - y0) * zoom);
                    }

                    Color areaColor = Color.FromArgb(100, 128, 128, 128);
                    bool on = false;
                    foreach (ErrorLayerControl myLayer in ErrorLayers)
                    {
                        if (myLayer.ErrorCode == myError.ErrorCode)
                        {
                            areaColor = Color.FromArgb(128, myLayer.LayerColor.R, myLayer.LayerColor.G, myLayer.LayerColor.B);
                            on = myLayer.IsVisible;
                            break;
                        }
                    }

                    if (on)
                    {
                        using SolidBrush brush = new SolidBrush(areaColor);
                        controlDC.FillPolygon(brush, points);

                        if (ReferenceEquals(myError, SelectedError))
                            DrawErrorSelection(points, GetErrorScreenPoints(myError));
                    }
                }
            }
        }

        public void Render()
        {
            Invalidate();
        }

        private void RenderFrame()
        {
            Clear();

            if (photo != null)
            {
                DrawImage(photo, 0, 0);
            }

            for (int i = 0; i < Lines.Count; i++)
            {
                if (topViewEnabled)
                {
                    // Top view: Lines holds the border and grid built by the main form —
                    // draw them as-is, without the perspective-view curve substitutions.
                    DrawLine(Lines[i]);
                    continue;
                }

                if (i == 2 && LeftEdgeCurvePoints.Count >= 2)
                    continue;
                if (i == 3 && RightEdgeCurvePoints.Count >= 2)
                    continue;

                DrawLine(GetDisplayLine(i));
            }

            if (measure_ruler != null)
            {
                DrawLine(measure_ruler);
            }

            if (!topViewEnabled)
            {
                DrawCurvePoints(LeftEdgeCurvePoints, Color.Pink);
                DrawCurvePoints(RightEdgeCurvePoints, Color.Pink);
                DrawCurveRubberLines();
            }

            if (command == 10 && subCommand == 1)
            {
                DrawSelectedArea();
            }

            if (PavementPhoto != null)
            {
                RenderErrors();
            }

        }

        public void DrawSelectedArea()
        {
            if (PavementPhoto == null) return;
            if (!PavementPhoto.PerspectiveCorrection.Normalized) return;

            using SolidBrush brush = new SolidBrush(AreaColor);
            Point[] points = new Point[4];

            Pos nearPos;
            Pos farPos;

            if (AreaPos0.Y > AreaPos1.Y)
            {
                nearPos = AreaPos0;
                farPos = AreaPos1;
            }
            else
            {
                nearPos = AreaPos1;
                farPos = AreaPos0;
            }

            Pos nearPosReal = ViewToRealPosition(nearPos);
            Pos farPosReal = ViewToRealPosition(farPos);

            double xMinReal = Math.Min(nearPosReal.X, farPosReal.X);
            double xMaxReal = Math.Max(nearPosReal.X, farPosReal.X);

            Pos[] corners = topViewEnabled
                ? new Pos[]
                {
                    RealToViewPosition(new Pos(xMinReal, nearPosReal.Y)),
                    RealToViewPosition(new Pos(xMaxReal, nearPosReal.Y)),
                    RealToViewPosition(new Pos(xMaxReal, farPosReal.Y)),
                    RealToViewPosition(new Pos(xMinReal, farPosReal.Y))
                }
                : PavementPhoto.PerspectiveCorrection.GetScreenAreaPolygon(xMinReal, xMaxReal, nearPosReal.Y, farPosReal.Y);

            points = new Point[corners.Length];
            for (int i = 0; i < corners.Length; i++)
            {
                points[i].X = (int)((corners[i].X - x0) * zoom);
                points[i].Y = (int)((corners[i].Y - y0) * zoom);
            }

            controlDC.FillPolygon(brush, points);
        }

        private void SelectErrorAt(double screenX, double screenY)
        {
            SelectedError = null;
            if (PavementPhoto == null || !PavementPhoto.PerspectiveCorrection.Normalized)
                return;

            double pavementWidth2 = PavementPhoto.PerspectiveCorrection.PavementWidth / 2.0;
            Pos real = ViewToRealPosition(new Pos(screenX, screenY));
            real.X -= pavementWidth2;
            real.Y += PavementPhoto.Section;

            for (int i = PavementPhoto.Errors.Count - 1; i >= 0; i--)
            {
                SurfaceError error = PavementPhoto.Errors[i];
                if (IsErrorVisible(error)
                    && real.X >= error.Left && real.X <= error.Right
                    && real.Y >= error.StartSection && real.Y <= error.EndSection)
                {
                    SelectedError = error;
                    break;
                }
            }

            Invalidate();
        }

        private bool IsErrorVisible(SurfaceError error)
        {
            foreach (ErrorLayerControl layer in ErrorLayers)
            {
                if (layer.ErrorCode == error.ErrorCode)
                    return layer.IsVisible;
            }

            return false;
        }

        private void UpdateSelectedErrorFromHandle(int handle, double screenX, double screenY)
        {
            double pavementWidth2 = PavementPhoto.PerspectiveCorrection.PavementWidth / 2.0;
            Pos real = ViewToRealPosition(new Pos(screenX, screenY));
            double x = real.X - pavementWidth2;
            double y = real.Y + PavementPhoto.Section;
            const double minimumSize = 0.001;

            if (handle == 0 || handle == 3)
                SelectedError.Left = Math.Min(x, SelectedError.Right - minimumSize);
            else
                SelectedError.Right = Math.Max(x, SelectedError.Left + minimumSize);

            if (handle == 0 || handle == 1)
                SelectedError.StartSection = Math.Min(y, SelectedError.EndSection - minimumSize);
            else
                SelectedError.EndSection = Math.Max(y, SelectedError.StartSection + minimumSize);
        }

        private int HitTestSelectedHandle(Point mousePoint)
        {
            if (SelectedError == null)
                return -1;

            Point[] points = GetErrorScreenPoints(SelectedError);
            int radius = ErrorHandleSize;
            for (int i = 0; i < points.Length; i++)
            {
                if (Math.Abs(mousePoint.X - points[i].X) <= radius
                    && Math.Abs(mousePoint.Y - points[i].Y) <= radius)
                    return i;
            }

            return -1;
        }

        private Point[] GetErrorScreenPoints(SurfaceError error)
        {
            double pavementWidth2 = PavementPhoto.PerspectiveCorrection.PavementWidth / 2.0;
            double startSection = PavementPhoto.Section;
            Pos[] realCorners =
            {
                new Pos(error.Left + pavementWidth2, error.StartSection - startSection),
                new Pos(error.Right + pavementWidth2, error.StartSection - startSection),
                new Pos(error.Right + pavementWidth2, error.EndSection - startSection),
                new Pos(error.Left + pavementWidth2, error.EndSection - startSection)
            };

            Point[] points = new Point[4];
            for (int i = 0; i < points.Length; i++)
            {
                Pos screen = RealToViewPosition(realCorners[i]);
                points[i] = new Point(
                    (int)((screen.X - x0) * zoom),
                    (int)((screen.Y - y0) * zoom));
            }

            return points;
        }

        private void DrawErrorSelection(Point[] outlinePoints, Point[] cornerHandles)
        {
            using Pen outline = new Pen(Color.White, 2.0f) { DashStyle = DashStyle.Dash };
            controlDC.DrawPolygon(outline, outlinePoints);

            // Handles only at the four draggable corners — the outline polygon may contain
            // many interpolated points along curved edges.
            int size = ErrorHandleSize;
            int half = size / 2;
            foreach (Point point in cornerHandles)
            {
                Rectangle handle = new Rectangle(point.X - half, point.Y - half, size, size);
                controlDC.FillRectangle(Brushes.White, handle);
                controlDC.DrawRectangle(Pens.Black, handle);
            }
        }

        public void DrawLine(Line line)
        {
            using Pen pen = new Pen(line.LineColor, (float)line.LineWidth);

            float x1 = (float)((line.P0.X - x0) * zoom);
            float y1 = (float)((line.P0.Y - y0) * zoom);
            float x2 = (float)((line.P1.X - x0) * zoom);
            float y2 = (float)((line.P1.Y - y0) * zoom);

            controlDC.DrawLine(pen, x1, y1, x2, y2);
        }

        private Line GetDisplayLine(int index)
        {
            if (index == 1 && LeftEdgeCurvePoints.Count >= 2 && RightEdgeCurvePoints.Count >= 2)
                return CreateDisplayLine(LeftEdgeCurvePoints[0], RightEdgeCurvePoints[0], Lines[index]);

            if (index == 0 && LeftEdgeCurvePoints.Count >= 2 && RightEdgeCurvePoints.Count >= 2)
                return CreateDisplayLine(
                    LeftEdgeCurvePoints[LeftEdgeCurvePoints.Count - 1],
                    RightEdgeCurvePoints[RightEdgeCurvePoints.Count - 1],
                    Lines[index]);

            return Lines[index];
        }

        private static Line CreateDisplayLine(Pos p0, Pos p1, Line source)
        {
            return new Line
            {
                P0 = new Pos(p0.X, p0.Y),
                P1 = new Pos(p1.X, p1.Y),
                LineColor = source.LineColor,
                LineWidth = source.LineWidth
            };
        }

        private void DrawCurvePoints(List<Pos> points, Color color)
        {
            if (points.Count == 0)
                return;

            using Pen pen = new Pen(color, 2.0f);
            using SolidBrush brush = new SolidBrush(color);
            using SolidBrush midpointBrush = new SolidBrush(Color.LightGreen);
            if (points.Count > 2)
            {
                PointF[] curve = new PointF[points.Count];
                for (int i = 0; i < points.Count; i++)
                    curve[i] = ToControlPoint(points[i]);

                controlDC.DrawCurve(pen, curve, 0.5f);
            }

            bool hoveredList = ReferenceEquals(points, hoverCurveEditPoints);
            PointF? previous = null;
            for (int i = 0; i < points.Count; i++)
            {
                Pos point = points[i];
                PointF current = ToControlPoint(point);

                if (previous.HasValue)
                {
                    if (points.Count == 2)
                        controlDC.DrawLine(pen, previous.Value, current);

                    PointF midpoint = GetCurveSegmentMidpoint(points, i - 1, previous.Value, current);
                    bool hovered = hoveredList && hoverCurveMidpoint && hoverCurveHandle == i - 1;
                    DrawHandleDot(midpoint, midpointBrush, hovered);
                }

                DrawHandleDot(current, brush, hoveredList && !hoverCurveMidpoint && hoverCurveHandle == i);
                previous = current;
            }
        }

        private void DrawHandleDot(PointF center, Brush brush, bool hovered)
        {
            int radius = hovered ? CurveHandleRadius + 2 : CurveHandleRadius;
            float x = center.X - radius;
            float y = center.Y - radius;
            float size = radius * 2;
            if (hovered)
            {
                using SolidBrush glow = new SolidBrush(Color.FromArgb(96, 255, 255, 255));
                controlDC.FillEllipse(glow, x - 3, y - 3, size + 6, size + 6);
            }

            controlDC.FillEllipse(hovered ? Brushes.Orange : brush, x, y, size, size);
            controlDC.DrawEllipse(Pens.Black, x, y, size, size);
        }

        private void DrawCurveRubberLines()
        {
            if (activeCurveEditPoints != null
                && activeCurveInsertSegment >= 0
                && activeCurveInsertSegment < activeCurveEditPoints.Count - 1)
            {
                Pos mouse = new Pos(x_Mouse, y_Mouse);
                DrawRubberLine(activeCurveEditPoints[activeCurveInsertSegment], mouse, false);
                DrawRubberLine(mouse, activeCurveEditPoints[activeCurveInsertSegment + 1], false);
                if (activeCurveInsertMidpoint != null)
                    DrawRubberLine(activeCurveInsertMidpoint, mouse, true);
                return;
            }

            if (activeCurveEditPoints != null && activeCurveHandle > 0 && activeCurveHandle < activeCurveEditPoints.Count - 1)
            {
                DrawRubberLine(activeCurveEditPoints[activeCurveHandle - 1], activeCurveEditPoints[activeCurveHandle], false);
                DrawRubberLine(activeCurveEditPoints[activeCurveHandle], activeCurveEditPoints[activeCurveHandle + 1], false);
                return;
            }
        }

        private void DrawRubberLine(Pos p0, Pos p1, bool dashed)
        {
            using Pen pen = new Pen(Color.White, 1.0f);
            if (dashed)
                pen.DashStyle = DashStyle.Dash;
            PointF c0 = ToControlPoint(p0);
            PointF c1 = ToControlPoint(p1);
            controlDC.DrawLine(pen, c0, c1);
        }

        private PointF GetCurveSegmentMidpoint(List<Pos> points, int segmentIndex, PointF fallbackPrevious, PointF fallbackCurrent)
        {
            if (points.Count < 3 || segmentIndex < 0 || segmentIndex >= points.Count - 1)
                return new PointF(
                    (fallbackPrevious.X + fallbackCurrent.X) / 2.0f,
                    (fallbackPrevious.Y + fallbackCurrent.Y) / 2.0f);

            Pos p0 = points[Math.Max(0, segmentIndex - 1)];
            Pos p1 = points[segmentIndex];
            Pos p2 = points[segmentIndex + 1];
            Pos p3 = points[Math.Min(points.Count - 1, segmentIndex + 2)];
            Pos midpoint = CatmullRom(p0, p1, p2, p3, 0.5);
            return ToControlPoint(midpoint);
        }

        private static Pos CatmullRom(Pos p0, Pos p1, Pos p2, Pos p3, double t)
        {
            double t2 = t * t;
            double t3 = t2 * t;
            return new Pos(
                0.5 * ((2.0 * p1.X)
                    + (-p0.X + p2.X) * t
                    + (2.0 * p0.X - 5.0 * p1.X + 4.0 * p2.X - p3.X) * t2
                    + (-p0.X + 3.0 * p1.X - 3.0 * p2.X + p3.X) * t3),
                0.5 * ((2.0 * p1.Y)
                    + (-p0.Y + p2.Y) * t
                    + (2.0 * p0.Y - 5.0 * p1.Y + 4.0 * p2.Y - p3.Y) * t2
                    + (-p0.Y + 3.0 * p1.Y - 3.0 * p2.Y + p3.Y) * t3));
        }

        private int HitTestCurveHandle(Point mousePoint, out List<Pos> points, out bool midpoint)
        {
            int handle = HitTestCurveHandle(LeftEdgeCurvePoints, mousePoint, out midpoint);
            if (handle >= 0)
            {
                points = LeftEdgeCurvePoints;
                return handle;
            }

            handle = HitTestCurveHandle(RightEdgeCurvePoints, mousePoint, out midpoint);
            if (handle >= 0)
            {
                points = RightEdgeCurvePoints;
                return handle;
            }

            points = null;
            midpoint = false;
            return -1;
        }

        private int HitTestCurveHandle(List<Pos> points, Point mousePoint, out bool midpoint)
        {
            midpoint = false;
            if (points.Count < 2)
                return -1;

            int radius = CurveHitRadius;
            for (int i = 0; i < points.Count; i++)
            {
                PointF point = ToControlPoint(points[i]);
                if (Math.Abs(mousePoint.X - point.X) <= radius && Math.Abs(mousePoint.Y - point.Y) <= radius)
                    return i;
            }

            for (int i = 0; i < points.Count - 1; i++)
            {
                PointF p0 = ToControlPoint(points[i]);
                PointF p1 = ToControlPoint(points[i + 1]);
                PointF midpointPoint = GetCurveSegmentMidpoint(points, i, p0, p1);
                float mx = midpointPoint.X;
                float my = midpointPoint.Y;
                if (Math.Abs(mousePoint.X - mx) <= radius && Math.Abs(mousePoint.Y - my) <= radius)
                {
                    midpoint = true;
                    return i;
                }
            }

            return -1;
        }

        private PointF ToControlPoint(Pos point)
        {
            return new PointF(
                (float)((point.X - x0) * zoom),
                (float)((point.Y - y0) * zoom));
        }

        public void DrawImage(Image image, double X, double Y)
        {
            Rectangle sRect = new Rectangle(0, 0, image.Width, image.Height);
            int dX = (int)((X - x0) * zoom + 0.5);
            int dY = (int)((Y - y0) * zoom + 0.5);
            int dWidth = (int)(image.Width * zoom + 0.5);
            int dHeight = (int)(image.Height * zoom + 0.5);

            Rectangle dRect = new Rectangle(dX, dY, dWidth, dHeight);
            controlDC.DrawImage(image, dRect, sRect, GraphicsUnit.Pixel);
        }

        public void CalcMeasureLength()
        {
            if (measure_ruler != null)
            {
                Pos start = measure_ruler.P0;
                Pos end = measure_ruler.P1;
                double measure_length = Math.Sqrt(
                    (end.Y - start.Y) * (end.Y - start.Y) +
                    (end.X - start.X) * (end.X - start.X));
                MeasureAction?.Invoke(measure_length);
                measure_ruler = null;
            }
            else MeasureAction?.Invoke(0);
        }

        private void DrawingArea_Disposed(object sender, EventArgs e)
        {
            // The photo bitmap is owned and disposed by MainForm.
            fastPaintTimer.Dispose();
        }
    }

    public class MouseMoveEventArgs : EventArgs
    {
        public double MouseX = 0.0;
        public double MouseY = 0.0;

        public MouseMoveEventArgs(double X, double Y)
        {
            MouseX = X;
            MouseY = Y;
        }
    }

    public class CommandEventArgs : EventArgs
    {
        public int Command = 0;
        public int SubCommand = 0;
        public double MouseX = 0;
        public double MouseY = 0;
    }

    public class CurveEditEventArgs : EventArgs
    {
        /// <summary>The live point list that was edited (LeftEdgeCurvePoints or RightEdgeCurvePoints).</summary>
        public List<Pos> Points { get; }
        public List<Pos> Before { get; }
        public List<Pos> After { get; }

        public CurveEditEventArgs(List<Pos> points, List<Pos> before, List<Pos> after)
        {
            Points = points;
            Before = before;
            After = after;
        }
    }

    public class ErrorEditEventArgs : EventArgs
    {
        public SurfaceError Error { get; }
        public SurfaceError Before { get; }
        public SurfaceError After { get; }

        public ErrorEditEventArgs(SurfaceError error, SurfaceError before, SurfaceError after)
        {
            Error = error;
            Before = before;
            After = after;
        }
    }
}
