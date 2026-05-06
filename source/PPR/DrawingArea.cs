using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Collections.Generic;

namespace PPR
{
    public partial class DrawingArea : UserControl
    {
        public Graphics controlDC;
        public Image controlBitmap;

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

        public List<Line> Lines = new List<Line>();
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
            }
        }

        Timer renderTimer;
        public bool RenderNeeded = false;

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

        public DrawingArea()
        {
            InitializeComponent();
            Resize += new EventHandler(DrawingArea_Resize);
            MouseDown += new MouseEventHandler(DrawingArea_MouseDown);
            MouseUp += new MouseEventHandler(DrawingArea_MouseUp);
            MouseWheel += new MouseEventHandler(DrawingArea_MouseWheel);
            MouseMove += new MouseEventHandler(DrawingArea_MouseMove);
            Load += new EventHandler(DrawingArea_Load);
        }

        void DrawingArea_Load(object sender, EventArgs e)
        {
            renderTimer = new Timer();
            renderTimer.Tick += new EventHandler(renderTimer_Tick);
            renderTimer.Interval = 40;
            renderTimer.Start();

            ClearLines();
        }

        public void ClearLines()
        {
            Lines.Clear();
            for (int i = 0; i < 4; i++) Lines.Add(new Line());
            Lines[0].LineColor = Color.LightGreen;
            Lines[0].LineWidth = 2;
            Lines[1].LineColor = Color.LightGreen;
            Lines[1].LineWidth = 2;
            Lines[2].LineColor = Color.Pink;
            Lines[2].LineWidth = 2;
            Lines[3].LineColor = Color.Pink;
            Lines[3].LineWidth = 2;
        }

        void renderTimer_Tick(object sender, EventArgs e)
        {
            if (RenderNeeded)
            {
                RenderNeeded = false;

                Render();
            }
        }

        void DrawingArea_MouseUp(object sender, MouseEventArgs e)
        {
            mouseButton = MouseButtons.None;
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
                    RenderNeeded = true;
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

            if (zoom > 2.0) zoom = 2.0;

            x0 = x_Mouse - e.X / zoom;
            y0 = y_Mouse - e.Y / zoom;

            RenderNeeded = true;
        }

        void DrawingArea_MouseDown(object sender, MouseEventArgs e)
        {
            mouseButton = e.Button;
            int prevCommand = command;
            int prevSubCommand = subCommand;

            switch (mouseButton)
            {
                case MouseButtons.Left:
                    switch (command)
                    {
                        case 1:
                            Lines[0].P0.X = 0.0;
                            Lines[0].P1.X = photo.Width;
                            Lines[0].P0.Y = y_Mouse;
                            Lines[0].P1.Y = y_Mouse;
                            RenderNeeded = true;
                            command = 0;
                            break;
                        case 2:
                            Lines[1].P0.X = 0.0;
                            Lines[1].P1.X = photo.Width;
                            Lines[1].P0.Y = y_Mouse;
                            Lines[1].P1.Y = y_Mouse;
                            RenderNeeded = true;
                            command = 0;
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
                            if (subCommand == 0)
                            {
                                subCommand = 1;
                                AreaPos0.X = x_Mouse;
                                AreaPos0.Y = y_Mouse;
                                AreaPos1.X = x_Mouse;
                                AreaPos1.Y = y_Mouse;
                            }
                            else
                            {
                                subCommand = 0;
                            }
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
                    x0_MouseDown = x0;
                    y0_MouseDown = y0;
                    x_MouseDown = e.X;
                    y_MouseDown = e.Y;
                    break;
            }

        }

        private void DrawingArea_Resize(object sender, EventArgs e)
        {
            if (Width > 0 && Height > 0)
            {
                SetDrawingArea();
                Render();
            }
        }

        public void SetDrawingArea()
        {
            if (controlBitmap != null) controlBitmap.Dispose();

            controlBitmap = new Bitmap(this.Width, this.Height, this.CreateGraphics());
            controlDC = Graphics.FromImage(controlBitmap);
            controlDC.SmoothingMode = SmoothingMode.AntiAlias;
        }

        private void RefreshControl()
        {
            Graphics windowDC = this.CreateGraphics();
            windowDC.DrawImage(controlBitmap, 0, 0);
            windowDC.Dispose();
        }

        private void Clear()
        {
            if (controlDC == null) SetDrawingArea();

            controlDC.Clear(Color.Black);
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

                    Pos[] screenPositions = new Pos[4];
                    screenPositions[0] = PavementPhoto.PerspectiveCorrection.GetScreenPosition(new Pos(leftReal, nearReal));
                    screenPositions[1] = PavementPhoto.PerspectiveCorrection.GetScreenPosition(new Pos(rightReal, nearReal));
                    screenPositions[2] = PavementPhoto.PerspectiveCorrection.GetScreenPosition(new Pos(rightReal, farReal));
                    screenPositions[3] = PavementPhoto.PerspectiveCorrection.GetScreenPosition(new Pos(leftReal, farReal));

                    Point[] points = new Point[4];
                    for (int i = 0; i < 4; i++)
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
                        SolidBrush brush = new SolidBrush(areaColor);
                        controlDC.FillPolygon(brush, points);

                        brush.Dispose();
                    }
                }
            }
        }

        public void Render()
        {
            Clear();

            if (photo != null)
            {
                DrawImage(photo, 0, 0);
            }

            foreach (Line line in Lines)
            {
                DrawLine(line);
            }

            if (measure_ruler != null)
            {
                DrawLine(measure_ruler);
            }

            if (command == 10 && subCommand == 1)
            {
                DrawSelectedArea();
            }

            if (PavementPhoto != null)
            {
                RenderErrors();
            }

            RefreshControl();
        }

        public void DrawSelectedArea()
        {
            if (PavementPhoto == null) return;
            if (!PavementPhoto.PerspectiveCorrection.Normalized) return;

            SolidBrush brush = new SolidBrush(AreaColor);
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

            Pos nearPosReal = PavementPhoto.PerspectiveCorrection.GetRealPosition(nearPos);
            Pos farPosReal = PavementPhoto.PerspectiveCorrection.GetRealPosition(farPos);

            double xMinReal = Math.Min(nearPosReal.X, farPosReal.X);
            double xMaxReal = Math.Max(nearPosReal.X, farPosReal.X);

            Pos[] cornersReal = new Pos[4];

            cornersReal[0] = new Pos(xMinReal, nearPosReal.Y);
            cornersReal[1] = new Pos(xMaxReal, nearPosReal.Y);
            cornersReal[2] = new Pos(xMaxReal, farPosReal.Y);
            cornersReal[3] = new Pos(xMinReal, farPosReal.Y);

            if ((farPosReal.Y - nearPosReal.Y) > 2.0)
            {
                ;
            }

            Pos[] corners = new Pos[4];

            for (int i = 0; i < 4; i++)
            {
                corners[i] = PavementPhoto.PerspectiveCorrection.GetScreenPosition(cornersReal[i]);
                points[i].X = (int)((corners[i].X - x0) * zoom);
                points[i].Y = (int)((corners[i].Y - y0) * zoom);
            }

            controlDC.FillPolygon(brush, points);
        }

        public void DrawLine(Line line)
        {
            Pen pen = new Pen(line.LineColor, (float)line.LineWidth);

            float x1 = (float)((line.P0.X - x0) * zoom);
            float y1 = (float)((line.P0.Y - y0) * zoom);
            float x2 = (float)((line.P1.X - x0) * zoom);
            float y2 = (float)((line.P1.Y - y0) * zoom);

            controlDC.DrawLine(pen, x1, y1, x2, y2);

            pen.Dispose();
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
                double measure_length = Math.Sqrt(Math.Pow(end.Y - start.Y, 2) + Math.Pow(end.X - start.X, 2));
                //double measure_length = Math.Abs(end.X - start.X);
                MeasureAction?.Invoke(measure_length);
                measure_ruler = null;
            }
            else MeasureAction?.Invoke(0);
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
}