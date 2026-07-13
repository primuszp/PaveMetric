using System;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace PPR
{
    public partial class MainForm : Form
    {
        Project _project = new Project();
        readonly Stack<EditOperation> _undoStack = new Stack<EditOperation>();
        readonly Stack<EditOperation> _redoStack = new Stack<EditOperation>();
        Bitmap _perspectiveBitmap;
        Bitmap _topViewBitmap;
        readonly object _prefetchSync = new object();
        string _prefetchFileName;
        Bitmap _prefetchBitmap;

        public MainForm()
        {
            InitializeComponent();

            Load += new EventHandler(MainForm_Load);
            FormClosed += MainForm_FormClosed;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            int width = Screen.PrimaryScreen.WorkingArea.Width;
            int height = Screen.PrimaryScreen.WorkingArea.Height;
            int dx = 5;
            int dy = 5;

            this.Left = dx;
            this.Top = dy;
            this.Width = width - 2 * dx - 150;
            this.Height = height - 2 * dy;

            drawingArea.OnDrawingAreaMouseMove += new DrawingArea.MouseMoveEventHandler(drawingArea_OnMouseMove);
            drawingArea.OnCommandStateChanged += new DrawingArea.CommandEventHAndler(drawingArea_OnCommandStateChanged);
            drawingArea.ErrorEditCompleted += drawingArea_ErrorEditCompleted;
            drawingArea.CurveEditCompleted += drawingArea_CurveEditCompleted;

            ErrorLayerControl newLayer = new ErrorLayerControl(errorLayerGroup);
            errorLayerGroup.AddLayer(newLayer);
            _project.ErrorLayers.Add(newLayer);
            newLayer.LayerName = "Kitöltött kátyú";
            newLayer.ErrorCode = ErrorCodes.FilledPothole;
            newLayer.LayerColor = Color.LightBlue;
            newLayer.IsVisible = true;
            newLayer.OnActionButtonClick += new EventHandler(ErrorLayer_OnActionButtonClick);
            newLayer.OnVisibleStateChanged += new EventHandler(ErrorLayer_OnVisibleStateChanged);
            newLayer.OnDeleteButtonClick += new EventHandler(ErrorLayer_OnDeleteButtonClick);

            newLayer = new ErrorLayerControl(errorLayerGroup);
            errorLayerGroup.AddLayer(newLayer);
            _project.ErrorLayers.Add(newLayer);
            newLayer.LayerName = "Kátyú";
            newLayer.ErrorCode = ErrorCodes.Pothole;
            newLayer.LayerColor = Color.Blue;
            newLayer.IsVisible = true;
            newLayer.OnActionButtonClick += new EventHandler(ErrorLayer_OnActionButtonClick);
            newLayer.OnVisibleStateChanged += new EventHandler(ErrorLayer_OnVisibleStateChanged);
            newLayer.OnDeleteButtonClick += new EventHandler(ErrorLayer_OnDeleteButtonClick);

            newLayer = new ErrorLayerControl(errorLayerGroup);
            errorLayerGroup.AddLayer(newLayer);
            _project.ErrorLayers.Add(newLayer);
            newLayer.LayerName = "Hálós repedés deformációval";
            newLayer.ErrorCode = ErrorCodes.AlligatorCrack;
            newLayer.LayerColor = Color.Red;
            newLayer.IsVisible = true;
            newLayer.OnActionButtonClick += new EventHandler(ErrorLayer_OnActionButtonClick);
            newLayer.OnVisibleStateChanged += new EventHandler(ErrorLayer_OnVisibleStateChanged);
            newLayer.OnDeleteButtonClick += new EventHandler(ErrorLayer_OnDeleteButtonClick);

            newLayer = new ErrorLayerControl(errorLayerGroup);
            errorLayerGroup.AddLayer(newLayer);
            _project.ErrorLayers.Add(newLayer);
            newLayer.LayerName = "Hálós repedés";
            newLayer.ErrorCode = ErrorCodes.MapCrack;
            newLayer.LayerColor = Color.Pink;
            newLayer.IsVisible = true;
            newLayer.OnActionButtonClick += new EventHandler(ErrorLayer_OnActionButtonClick);
            newLayer.OnVisibleStateChanged += new EventHandler(ErrorLayer_OnVisibleStateChanged);
            newLayer.OnDeleteButtonClick += new EventHandler(ErrorLayer_OnDeleteButtonClick);
            errorLayerGroup.SetActiveLayer(newLayer);

            newLayer = new ErrorLayerControl(errorLayerGroup);
            errorLayerGroup.AddLayer(newLayer);
            _project.ErrorLayers.Add(newLayer);
            newLayer.LayerName = "Felületi hámlás";
            newLayer.ErrorCode = ErrorCodes.SurfacePeelOff;
            newLayer.LayerColor = Color.Yellow;
            newLayer.IsVisible = true;
            newLayer.OnActionButtonClick += new EventHandler(ErrorLayer_OnActionButtonClick);
            newLayer.OnVisibleStateChanged += new EventHandler(ErrorLayer_OnVisibleStateChanged);
            newLayer.OnDeleteButtonClick += new EventHandler(ErrorLayer_OnDeleteButtonClick);
            errorLayerGroup.SetActiveLayer(newLayer);

            newLayer = new ErrorLayerControl(errorLayerGroup);
            errorLayerGroup.AddLayer(newLayer);
            _project.ErrorLayers.Add(newLayer);
            newLayer.LayerName = "Izzadás";
            newLayer.ErrorCode = ErrorCodes.SurfacePerspiration;
            newLayer.LayerColor = Color.Green;
            newLayer.IsVisible = true;
            newLayer.OnActionButtonClick += new EventHandler(ErrorLayer_OnActionButtonClick);
            newLayer.OnVisibleStateChanged += new EventHandler(ErrorLayer_OnVisibleStateChanged);
            newLayer.OnDeleteButtonClick += new EventHandler(ErrorLayer_OnDeleteButtonClick);
            errorLayerGroup.SetActiveLayer(newLayer);
        }

        void ErrorLayer_OnDeleteButtonClick(object sender, EventArgs e)
        {
            ErrorLayerControl layer = sender as ErrorLayerControl;
            errorLayerGroup.SetActiveLayer(layer);
            drawingArea.ActualError = new SurfaceError();
            drawingArea.ActualError.ErrorCode = layer.ErrorCode;
            drawingArea.Command = 11;
        }

        void ErrorLayer_OnVisibleStateChanged(object sender, EventArgs e)
        {
            ErrorLayerControl layer = sender as ErrorLayerControl;
            errorLayerGroup.SetActiveLayer(layer);
        }

        void ErrorLayer_OnActionButtonClick(object sender, EventArgs e)
        {
            ErrorLayerControl layer = sender as ErrorLayerControl;
            errorLayerGroup.SetActiveLayer(layer);
            drawingArea.Command = 10;
            drawingArea.AreaColor = Color.FromArgb(128, layer.LayerColor.R, layer.LayerColor.G, layer.LayerColor.B);
            drawingArea.ActualError = new SurfaceError();
            drawingArea.ActualError.ErrorCode = layer.ErrorCode;
        }

        void drawingArea_OnCommandStateChanged(object sender, CommandEventArgs e)
        {
            if (e.Command <= 4 && e.Command > 0)
            {
                if (e.Command == 3 && e.SubCommand == 1)
                    SetCurveFromLine(drawingArea.LeftEdgeCurvePoints, drawingArea.Lines[2], drawingArea.Lines[1].P0.Y, drawingArea.Lines[0].P0.Y);
                else if (e.Command == 4 && e.SubCommand == 1)
                    SetCurveFromLine(drawingArea.RightEdgeCurvePoints, drawingArea.Lines[3], drawingArea.Lines[1].P0.Y, drawingArea.Lines[0].P0.Y);

                drawingArea.RenderNeeded = true;
            }

            if (e.Command == 10)
            {
                if (e.SubCommand == 1)
                {
                    double pavementWidth_2 = _project.ActualPhoto.PerspectiveCorrection.PavementWidth / 2.0;
                    double startSection = _project.ActualPhoto.Section;
                    Pos realPos0 = drawingArea.ViewToRealPosition(drawingArea.AreaPos0);
                    Pos realPos1 = drawingArea.ViewToRealPosition(drawingArea.AreaPos1);
                    drawingArea.ActualError.Left = Math.Min(realPos0.X, realPos1.X) - pavementWidth_2;
                    drawingArea.ActualError.Right = Math.Max(realPos0.X, realPos1.X) - pavementWidth_2;
                    drawingArea.ActualError.StartSection = Math.Min(realPos0.Y, realPos1.Y) + startSection;
                    drawingArea.ActualError.EndSection = Math.Max(realPos0.Y, realPos1.Y) + startSection;
                    if (drawingArea.ActualError.Right - drawingArea.ActualError.Left < 0.001
                        || drawingArea.ActualError.EndSection - drawingArea.ActualError.StartSection < 0.001)
                    {
                        drawingArea.RenderNeeded = true;
                        return;
                    }

                    SurfaceError addedError = drawingArea.ActualError;
                    PavementPhoto photo = _project.ActualPhoto;
                    int index = photo.Errors.Count;
                    photo.Errors.Add(addedError);
                    RecordOperation(
                        () => photo.Errors.Remove(addedError),
                        () => photo.Errors.Insert(Math.Min(index, photo.Errors.Count), addedError));

                    drawingArea.Command = 0;
                    drawingArea.SelectError(addedError);
                    drawingArea.ActualError = null;
                }
            }

            if (e.Command == 11)
            {
                if (_project.ActualPhoto != null)
                {
                    double pavementWidth_2 = _project.ActualPhoto.PerspectiveCorrection.PavementWidth / 2.0;
                    double startSection = _project.ActualPhoto.Section;
                    Pos realPos = drawingArea.ViewToRealPosition(new Pos(e.MouseX, e.MouseY));
                    realPos.X -= pavementWidth_2;
                    realPos.Y += startSection;
                    SurfaceError deletedError = null;
                    foreach (SurfaceError myError in _project.ActualPhoto.Errors)
                    {
                        if (myError.ErrorCode == drawingArea.ActualError.ErrorCode)
                        {
                            if (realPos.X > myError.Left && realPos.X < myError.Right)
                            {
                                if (realPos.Y > myError.StartSection && realPos.Y < myError.EndSection)
                                {
                                    deletedError = myError;
                                    break;
                                }
                            }
                        }
                    }
                    if (deletedError != null)
                    {
                        PavementPhoto photo = _project.ActualPhoto;
                        int index = photo.Errors.IndexOf(deletedError);
                        photo.Errors.Remove(deletedError);
                        RecordOperation(
                            () => photo.Errors.Insert(Math.Min(index, photo.Errors.Count), deletedError),
                            () => photo.Errors.Remove(deletedError));
                        drawingArea.RenderNeeded = true;
                    }
                }
            }

        }

        void drawingArea_CurveEditCompleted(object sender, CurveEditEventArgs e)
        {
            List<Pos> points = e.Points;
            List<Pos> before = e.Before;
            List<Pos> after = e.After;
            RecordOperation(
                () => { RestorePoints(points, before); MarkPerspectiveDirty(); },
                () => { RestorePoints(points, after); MarkPerspectiveDirty(); });
            MarkPerspectiveDirty();
        }

        private static void RestorePoints(List<Pos> target, List<Pos> snapshot)
        {
            target.Clear();
            foreach (Pos point in snapshot)
                target.Add(new Pos(point.X, point.Y));
        }

        /// <summary>
        /// Highlights the Normalize button after a curve edit: the perspective correction and
        /// the top view are stale until the user re-normalizes.
        /// </summary>
        private void MarkPerspectiveDirty()
        {
            button_Normalize.BackColor = Color.Gold;
            button_Normalize.ToolTipText = "A szegélyek módosultak — normalizálás szükséges.";
        }

        private void ClearPerspectiveDirty()
        {
            button_Normalize.BackColor = SystemColors.Control;
            button_Normalize.ToolTipText = string.Empty;
        }

        void drawingArea_ErrorEditCompleted(object sender, ErrorEditEventArgs e)
        {
            if (e.Before == null || BoundsEqual(e.Before, e.After))
                return;

            SurfaceError error = e.Error;
            SurfaceError before = e.Before.Clone();
            SurfaceError after = e.After.Clone();
            RecordOperation(
                () => error.CopyBoundsFrom(before),
                () => error.CopyBoundsFrom(after));
        }

        void drawingArea_OnMouseMove(object sender, MouseMoveEventArgs e)
        {
            if (_project.ActualPhoto == null) return;
            Pos screenPosition = new Pos(e.MouseX, e.MouseY);
            if (_project.ActualPhoto.PerspectiveCorrection.Normalized)
            {
                Pos realPosition = drawingArea.ViewToRealPosition(screenPosition);
                label_Coords.Text = realPosition.X.ToString("0.00") + " ; " + realPosition.Y.ToString("0.00");
            }
        }

        private void button_FarLine_Click(object sender, EventArgs e)
        {
            drawingArea.Command = 1;
        }

        private void button_NearLine_Click(object sender, EventArgs e)
        {
            drawingArea.Command = 2;
        }

        private void button_LeftLine_Click(object sender, EventArgs e)
        {
            drawingArea.Command = 3;
        }

        private void button_RightLine_Click(object sender, EventArgs e)
        {
            drawingArea.Command = 4;
        }

        void DrawTopViewNet()
        {
            drawingArea.Lines.Clear();
            if (_topViewBitmap == null) return;

            var pc = _project.ActualPhoto.PerspectiveCorrection;
            double nearY = pc.TopViewNearRealY;
            double farY = pc.TopViewFarRealY;
            double w = pc.PavementWidth;

            Pos V(double x, double y) => drawingArea.RealToViewPosition(new Pos(x, y));

            Line MakeLine(Pos p0, Pos p1, Color color, double lw = 1.5)
            {
                var line = new Line();
                line.P0.X = p0.X; line.P0.Y = p0.Y;
                line.P1.X = p1.X; line.P1.Y = p1.Y;
                line.LineColor = color;
                line.LineWidth = lw;
                return line;
            }

            // Border
            drawingArea.Lines.Add(MakeLine(V(0, farY), V(w, farY), Color.LightGreen, 2.0));
            drawingArea.Lines.Add(MakeLine(V(0, nearY), V(w, nearY), Color.LightGreen, 2.0));
            drawingArea.Lines.Add(MakeLine(V(0, nearY), V(0, farY), Color.Pink, 2.0));
            drawingArea.Lines.Add(MakeLine(V(w, nearY), V(w, farY), Color.Pink, 2.0));

            // Vertical grid lines — same real X positions as in perspective view
            double dx = w / pc.ColCount;
            for (int i = 1; i < pc.ColCount; i++)
                drawingArea.Lines.Add(MakeLine(V(i * dx, nearY), V(i * dx, farY), Color.FromArgb(128, 255, 255, 255)));

            // Horizontal grid lines — same real Y positions as in perspective view
            double dy = pc.Length / pc.RowCount;
            for (int i = 1; i < pc.RowCount; i++)
            {
                double y = i * dy;
                if (y >= nearY && y <= farY)
                    drawingArea.Lines.Add(MakeLine(V(0, y), V(w, y), Color.FromArgb(128, 255, 255, 255)));
            }
        }

        void DrawNet()
        {
            drawingArea.Lines.Clear();

            var pc = _project.ActualPhoto.PerspectiveCorrection;
            Line[] lines = pc.GetNet();

            drawingArea.Lines.Add(CreateLine(pc.LeftEdge.P1, pc.RightEdge.P1, Color.LightGreen, 2.0));
            drawingArea.Lines.Add(CreateLine(pc.LeftEdge.P0, pc.RightEdge.P0, Color.LightGreen, 2.0));
            drawingArea.Lines.Add(CreateLine(pc.LeftEdge.P0, pc.LeftEdge.P1, Color.Pink, 2.0));
            drawingArea.Lines.Add(CreateLine(pc.RightEdge.P0, pc.RightEdge.P1, Color.Pink, 2.0));

            // Net lines
            foreach (Line line in lines)
            {
                drawingArea.Lines.Add(line);
            }
        }

        private static Line CreateLine(Pos p0, Pos p1, Color color, double lineWidth)
        {
            return new Line
            {
                P0 = new Pos(p0.X, p0.Y),
                P1 = new Pos(p1.X, p1.Y),
                LineColor = color,
                LineWidth = lineWidth
            };
        }

        private void ImportPhotos()
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                ClearEditHistory();
                _project.LoadPhotos(folderDialog.SelectedPath);
                UpdateControls();
            }
        }

        private void SaveProject(string FileName)
        {
            _project.ProjectPath = Path.GetDirectoryName(FileName);
            _project.ProjectFileName = FileName;

            XmlSerializer serializer = new XmlSerializer(typeof(Project));
            using XmlTextWriter writer = new XmlTextWriter(FileName, Encoding.UTF8);
            writer.Formatting = Formatting.Indented;
            serializer.Serialize(writer, _project);
        }

        private void LoadProject()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "Projekt megnyitása";
            dialog.Multiselect = false;
            dialog.Filter = "PPR Projektek | *.ppr";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Project));
                using (XmlTextReader reader = new XmlTextReader(dialog.FileName))
                    _project = (Project)serializer.Deserialize(reader);
                ClearEditHistory();
                _project.MoveToFirstPhoto();
                _project.ProjectFileName = dialog.FileName;
                _project.ProjectPath = Path.GetDirectoryName(dialog.FileName);
                UpdateTitle();
                UpdateControls();
            }
        }

        private void SaveProjectAs()
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = "Projekt mentése";
            dialog.Filter = "PPR Projektek | *.ppr";

            if (_project.ProjectPath != "")
            {
                if (Directory.Exists(_project.ProjectPath))
                {
                    dialog.InitialDirectory = _project.ProjectPath;
                }
            }

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                SaveProject(dialog.FileName);
                UpdateTitle();
            }
        }

        private void UpdateTitle()
        {
            if (_project.ProjectFileName == "")
            {
                this.Text = "Útburkolat állapot értékelés";
            }
            else
            {
                this.Text = "Útburkolat állapot értékelés - " + _project.ProjectFileName;
                tabControl_Main.SelectTab("tabPage_Rating");
            }
        }

        private void UpdateControls()
        {
            combo_Section.Items.Clear();
            foreach (PavementPhoto myPhoto in _project.Photos)
            {
                combo_Section.Items.Add(myPhoto.Section.ToString("0+000"));
            }
            if (combo_Section.Items.Count > 0)
            {
                combo_Section.SelectedIndex = 0;
            }

            UpdateDrawing();
        }

        private void UpdateDrawing()
        {
            string fileName = "";
            drawingArea.ClearErrorSelection();
            SetTopView(false);

            if (_project.ActualPhoto != null)
            {
                fileName = GetPhotoFilePath(_project.ActualPhoto);
                _perspectiveBitmap?.Dispose();
                _topViewBitmap?.Dispose();
                _topViewBitmap = null;
                _perspectiveBitmap = LoadPhotoBitmap(fileName);
                drawingArea.Photo = _perspectiveBitmap;
                PrefetchNeighborPhotos();

                drawingArea.PavementPhoto = _project.ActualPhoto;
                drawingArea.LeftEdgeCurvePoints = ClonePoints(_project.ActualPhoto.PerspectiveCorrection.LeftEdgePoints);
                drawingArea.RightEdgeCurvePoints = ClonePoints(_project.ActualPhoto.PerspectiveCorrection.RightEdgePoints);
                if (drawingArea.LeftEdgeCurvePoints.Count < 2)
                    SetCurveFromLine(drawingArea.LeftEdgeCurvePoints, _project.ActualPhoto.PerspectiveCorrection.LeftEdge, _project.ActualPhoto.PerspectiveCorrection.NearDistance, _project.ActualPhoto.PerspectiveCorrection.FarDistance);
                if (drawingArea.RightEdgeCurvePoints.Count < 2)
                    SetCurveFromLine(drawingArea.RightEdgeCurvePoints, _project.ActualPhoto.PerspectiveCorrection.RightEdge, _project.ActualPhoto.PerspectiveCorrection.NearDistance, _project.ActualPhoto.PerspectiveCorrection.FarDistance);

                //if (project.ActualPhoto.PerspectiveCorrection.PavementWidth == 0)
                //{
                //    project.ActualPhoto.PerspectiveCorrection.PavementWidth = 6.0;
                //}
                textBox_PavementWidth.Text = _project.ActualPhoto.PerspectiveCorrection.PavementWidth.ToString("0.00");


                //if (project.ActualPhoto.PerspectiveCorrection.Length == 0)
                //{
                //    project.ActualPhoto.PerspectiveCorrection.Length = 10.0;
                //}
                textBox_SectionLength.Text = _project.ActualPhoto.PerspectiveCorrection.Length.ToString("0.00");


                if (_project.ActualPhoto.PerspectiveCorrection.ColCount == 0)
                {
                    _project.ActualPhoto.PerspectiveCorrection.ColCount = 24;
                }
                numericUpDown_Col.Value = _project.ActualPhoto.PerspectiveCorrection.ColCount;

                if (_project.ActualPhoto.PerspectiveCorrection.RowCount == 0)
                {
                    _project.ActualPhoto.PerspectiveCorrection.RowCount = 20;
                }
                numericUpDown_Row.Value = _project.ActualPhoto.PerspectiveCorrection.RowCount;

                if (drawingArea.ErrorLayers.Count == 0)
                {
                    foreach (ErrorLayerControl myLayer in errorLayerGroup.ErrorLayers)
                    {
                        drawingArea.ErrorLayers.Add(myLayer);
                        if (!_project.ErrorLayers.Contains(myLayer))
                            _project.ErrorLayers.Add(myLayer);
                    }
                }

                if (_project.ActualPhoto.PerspectiveCorrection.Normalized)
                {
                    DrawNet();
                    button_ToggleView.Enabled = true;
                }
                else
                {
                    drawingArea.ClearLines();
                    button_ToggleView.Enabled = false;
                }
            }
            else
            {
                drawingArea.PavementPhoto = null;
                drawingArea.LeftEdgeCurvePoints.Clear();
                drawingArea.RightEdgeCurvePoints.Clear();
                button_ToggleView.Enabled = false;
            }

            drawingArea.RenderNeeded = true;
        }

        private string GetPhotoFilePath(PavementPhoto photo)
        {
            return _project.ProjectPath + "\\" + photo.PhotoFileName + ".jpg";
        }

        /// <summary>
        /// Uses the prefetched bitmap when the requested file was already loaded in the
        /// background, otherwise loads synchronously.
        /// </summary>
        private Bitmap LoadPhotoBitmap(string fileName)
        {
            lock (_prefetchSync)
            {
                if (_prefetchBitmap != null && _prefetchFileName == fileName)
                {
                    Bitmap bitmap = _prefetchBitmap;
                    _prefetchBitmap = null;
                    _prefetchFileName = null;
                    return bitmap;
                }
            }

            return new Bitmap(fileName);
        }

        /// <summary>
        /// Loads the next section's photo in the background so stepping through sections
        /// (Space/PageDown) shows the image instantly.
        /// </summary>
        private void PrefetchNeighborPhotos()
        {
            int nextIndex = combo_Section.SelectedIndex + 1;
            if (nextIndex <= 0 || nextIndex >= _project.Photos.Count)
                return;

            string fileName = GetPhotoFilePath(_project.Photos[nextIndex]);
            lock (_prefetchSync)
            {
                if (_prefetchFileName == fileName)
                    return;
            }

            Task.Run(() =>
            {
                try
                {
                    Bitmap bitmap = new Bitmap(fileName);
                    lock (_prefetchSync)
                    {
                        _prefetchBitmap?.Dispose();
                        _prefetchBitmap = bitmap;
                        _prefetchFileName = fileName;
                    }
                }
                catch
                {
                    // Prefetch is best-effort; a failed load falls back to the synchronous path.
                }
            });
        }

        private void button_ImportPhotos_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                ClearEditHistory();
                _project.LoadPhotos(folderDialog.SelectedPath);
                UpdateControls();
            }
        }

        private void button_OpenProject_Click(object sender, EventArgs e)
        {
            LoadProject();
        }

        private void button_SaveProject_Click(object sender, EventArgs e)
        {
            if (_project.ProjectFileName != "" && File.Exists(_project.ProjectFileName))
            {
                SaveProject(_project.ProjectFileName);
            }
            else
            {
                SaveProjectAs();
            }
        }

        private void button_FarDistance_Click(object sender, EventArgs e)
        {
            drawingArea.Command = 1;
        }

        private void button_NearDistance_Click(object sender, EventArgs e)
        {
            drawingArea.Command = 2;
        }

        private void button_LeftEdge_Click(object sender, EventArgs e)
        {
            drawingArea.Command = 3;
        }

        private void button_RightEdge_Click(object sender, EventArgs e)
        {
            drawingArea.Command = 4;
        }

        private void button_Measure_Click(object sender, EventArgs e)
        {
            drawingArea.Command = 5;
            drawingArea.MeasureAction = CalcMeasureLength;
        }

        /// <summary>
        /// Parses user-entered numbers accepting both comma and dot decimal separators,
        /// independent of the Windows locale.
        /// </summary>
        internal static bool TryParseDouble(string text, out double value)
        {
            return double.TryParse(
                (text ?? string.Empty).Trim().Replace(',', '.'),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value);
        }

        void CalcMeasureLength(double measureLength)
        {
            TryParseDouble(textBox_MeasureBase.Text, out var baseLength);
            double scale = baseLength / measureLength;

            if (scale > 0)
            {
                if (_project.ActualPhoto.PerspectiveCorrection.Normalized)
                {
                    double x0 = _project.ActualPhoto.PerspectiveCorrection.LeftEdge.P1.X;
                    double x1 = _project.ActualPhoto.PerspectiveCorrection.RightEdge.P1.X;
                    double pw = Math.Round(Math.Abs(x1 - x0) * scale / 1000d, 2);

                    textBox_PavementWidth.Text = string.Format("{0:N2}", pw);         
                    Normalize();
                }
            }
        }

        void Normalize()
        {
            Line[] lines = new Line[4];
            for (int i = 0; i < 4; i++)
            {
                lines[i] = drawingArea.Lines[i];
            }

            TryParseDouble(textBox_SectionLength.Text, out var length);
            TryParseDouble(textBox_PavementWidth.Text, out var width);
            SnapCurveEndpointsToBoundaries(drawingArea.LeftEdgeCurvePoints, drawingArea.Lines[1].P0.Y, drawingArea.Lines[0].P0.Y);
            SnapCurveEndpointsToBoundaries(drawingArea.RightEdgeCurvePoints, drawingArea.Lines[1].P0.Y, drawingArea.Lines[0].P0.Y);

            _project.ActualPhoto.PerspectiveCorrection.Length = length;
            _project.ActualPhoto.PerspectiveCorrection.PavementWidth = width;

            if (HasCurvedEdges())
            {
                _project.ActualPhoto.PerspectiveCorrection.NormalizeCurved(
                    drawingArea.LeftEdgeCurvePoints,
                    drawingArea.RightEdgeCurvePoints,
                    length,
                    width);
            }
            else
            {
                if (drawingArea.LeftEdgeCurvePoints.Count == 2)
                {
                    lines[2].P0 = new Pos(drawingArea.LeftEdgeCurvePoints[0].X, drawingArea.LeftEdgeCurvePoints[0].Y);
                    lines[2].P1 = new Pos(drawingArea.LeftEdgeCurvePoints[1].X, drawingArea.LeftEdgeCurvePoints[1].Y);
                }

                if (drawingArea.RightEdgeCurvePoints.Count == 2)
                {
                    lines[3].P0 = new Pos(drawingArea.RightEdgeCurvePoints[0].X, drawingArea.RightEdgeCurvePoints[0].Y);
                    lines[3].P1 = new Pos(drawingArea.RightEdgeCurvePoints[1].X, drawingArea.RightEdgeCurvePoints[1].Y);
                }

                _project.ActualPhoto.PerspectiveCorrection.Normalize(lines, length, width);
            }
            _topViewBitmap?.Dispose();
            _topViewBitmap = null;

            ClearPerspectiveDirty();
            DrawNet();

            drawingArea.RenderNeeded = true;
        }

        private static List<Pos> ClonePoints(List<Pos> points)
        {
            List<Pos> result = new List<Pos>();
            if (points == null)
                return result;

            foreach (Pos point in points)
                result.Add(new Pos(point.X, point.Y));

            return result;
        }

        private static void SetCurveFromLine(List<Pos> points, Line line, double nearY, double farY)
        {
            if (!IsUsableLine(line))
                return;

            points.Clear();
            points.Add(new Pos(GetLineXAtY(line, nearY), nearY));
            points.Add(new Pos(GetLineXAtY(line, farY), farY));
        }

        private static double GetLineXAtY(Line line, double y)
        {
            double dy = line.P1.Y - line.P0.Y;
            if (Math.Abs(dy) < 1e-9)
                return line.P0.X;

            double amount = (y - line.P0.Y) / dy;
            return line.P0.X + (line.P1.X - line.P0.X) * amount;
        }

        private static void SnapCurveEndpointsToBoundaries(List<Pos> points, double nearY, double farY)
        {
            if (points == null || points.Count < 2)
                return;

            points[0].Y = nearY;
            points[points.Count - 1].Y = farY;
        }

        private bool HasCurvedEdges()
        {
            return drawingArea.LeftEdgeCurvePoints.Count > 2 || drawingArea.RightEdgeCurvePoints.Count > 2;
        }

        private static bool IsUsableLine(Line line)
        {
            if (line == null)
                return false;

            double dx = line.P1.X - line.P0.X;
            double dy = line.P1.Y - line.P0.Y;
            return dx * dx + dy * dy > 1e-9;
        }

        private void button_Normalize_Click(object sender, EventArgs e)
        {
            Normalize();
            button_ToggleView.Enabled = _project.ActualPhoto?.PerspectiveCorrection.Normalized == true;
        }

        private void button_ToggleView_Click(object sender, EventArgs e)
        {
            SetTopView(button_ToggleView.Checked);
        }

        private void SetTopView(bool enabled)
        {
            if (enabled && (_project.ActualPhoto == null || !_project.ActualPhoto.PerspectiveCorrection.Normalized))
                enabled = false;

            if (enabled && _topViewBitmap == null)
                _topViewBitmap = _project.ActualPhoto.PerspectiveCorrection.CreateTopView(_perspectiveBitmap);

            if (enabled && _topViewBitmap == null)
            {
                button_ToggleView.Checked = false;
                MessageBox.Show(
                    "A felülnézet nem készíthető el. Ellenőrizd, hogy a közeli/távoli határ és mindkét útszél helyesen van kijelölve az aktuális képen.",
                    "Érvénytelen perspektívageometria",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                enabled = false;
            }

            drawingArea.TopViewEnabled = enabled;
            drawingArea.Photo = enabled ? _topViewBitmap : _perspectiveBitmap;
            if (enabled)
                drawingArea.FitImageToView();
            else
                drawingArea.ResetView();
            button_ToggleView.Checked = enabled;
            button_ToggleView.Text = enabled ? "Perspektíva" : "Felülnézet";

            button_FarDistance.Enabled = !enabled;
            button_NearDistance.Enabled = !enabled;
            button_LeftEdge.Enabled = !enabled;
            button_RightEdge.Enabled = !enabled;
            button_Normalize.Enabled = !enabled;
            button_Measure.Enabled = !enabled;

            if (enabled)
                DrawTopViewNet();
            else if (_project.ActualPhoto?.PerspectiveCorrection.Normalized == true)
                DrawNet();

            drawingArea.RenderNeeded = true;
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _topViewBitmap?.Dispose();
            _perspectiveBitmap?.Dispose();
            lock (_prefetchSync)
            {
                _prefetchBitmap?.Dispose();
                _prefetchBitmap = null;
            }
        }

        private void button_Previous_Click(object sender, EventArgs e)
        {
            PreviousSection();
        }

        private void button_Next_Click(object sender, EventArgs e)
        {
            NextSection();
        }

        private void combo_Section_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Undo history is per photo: curve-edit operations reference the point lists of
            // the section they were recorded on, which are replaced on section change.
            ClearEditHistory();
            _project.MoveToPhotoAt(combo_Section.SelectedIndex);
            UpdateDrawing();
        }

        private void NextSection()
        {
            if (combo_Section.Items.Count > 0)
            {
                if (combo_Section.SelectedIndex < combo_Section.Items.Count - 1)
                {
                    combo_Section.SelectedIndex++;
                }
            }
        }

        private void PreviousSection()
        {
            if (combo_Section.Items.Count > 0)
            {
                if (combo_Section.SelectedIndex > 0)
                {
                    combo_Section.SelectedIndex--;
                }
            }
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Z)
            {
                if (e.Shift)
                    Redo();
                else
                    Undo();

                e.SuppressKeyPress = true;
                return;
            }

            if (e.Control && e.KeyCode == Keys.Y)
            {
                Redo();
                e.SuppressKeyPress = true;
                return;
            }

            switch (e.KeyCode)
            {
                case Keys.Escape:
                    drawingArea.CancelActiveEdit();
                    drawingArea.Command = 0;
                    break;
                case Keys.Delete:
                    DeleteSelectedError();
                    e.SuppressKeyPress = true;
                    break;
                case Keys.PageDown:
                case Keys.Space:
                    NextSection();
                    break;
                case Keys.PageUp:
                    PreviousSection();
                    break;
                case Keys.B:
                    drawingArea.Command = 3;
                    break;
                case Keys.J:
                    drawingArea.Command = 4;
                    break;
                case Keys.T:
                    drawingArea.Command = 1;
                    break;
                case Keys.K:
                    drawingArea.Command = 2;
                    break;
                case Keys.M:
                    drawingArea.Command = 5;
                    break;
            }
        }

        private void button_FirstNonNormalized_Click(object sender, EventArgs e)
        {
            if (_project.ActualPhoto != null)
            {
                int photoIndex = 0;

                for (int i = 0; i < _project.Photos.Count; i++)
                {
                    if (!_project.Photos[i].PerspectiveCorrection.Normalized)
                    {
                        photoIndex = i;
                        break;
                    }
                }

                if (photoIndex != 0)
                {
                    combo_Section.SelectedIndex = photoIndex;
                }
            }
        }

        private void UpdateNet()
        {
            if (_project.ActualPhoto.PerspectiveCorrection.Normalized)
            {
                DrawNet();
            }
            else
            {
                drawingArea.ClearLines();
            }
            drawingArea.RenderNeeded = true;
        }

        private void textBox_SectionLength_TextChanged(object sender, EventArgs e)
        {
            if (_project.ActualPhoto != null)
            {
                double tempValue = _project.ActualPhoto.PerspectiveCorrection.Length;
                if (MainForm.TryParseDouble(textBox_SectionLength.Text, out tempValue))
                {
                    _project.ActualPhoto.PerspectiveCorrection.Length = tempValue;
                    UpdateNet();
                }
            }
        }

        private void textBox_PavementWidth_TextChanged(object sender, EventArgs e)
        {
            if (_project.ActualPhoto != null)
            {
                double tempValue = _project.ActualPhoto.PerspectiveCorrection.PavementWidth;
                if (MainForm.TryParseDouble(textBox_PavementWidth.Text, out tempValue))
                {
                    _project.ActualPhoto.PerspectiveCorrection.PavementWidth = tempValue;
                    UpdateNet();
                }
            }
        }

        private void numericUpDown_Col_ValueChanged(object sender, EventArgs e)
        {
            if (_project.ActualPhoto != null)
            {
                int tempValue = _project.ActualPhoto.PerspectiveCorrection.ColCount;

                if ((int)numericUpDown_Col.Value != tempValue)
                {
                    _project.ActualPhoto.PerspectiveCorrection.ColCount = (int)numericUpDown_Col.Value;
                    UpdateNet();
                }
            }
        }

        private void numericUpDown_Row_ValueChanged(object sender, EventArgs e)
        {
            if (_project.ActualPhoto != null)
            {
                int tempValue = _project.ActualPhoto.PerspectiveCorrection.RowCount;

                if ((int)numericUpDown_Row.Value != tempValue)
                {
                    _project.ActualPhoto.PerspectiveCorrection.RowCount = (int)numericUpDown_Row.Value;
                    UpdateNet();
                }
            }
        }

        private void button_ApplyToAll_Click(object sender, EventArgs e)
        {
            foreach (PavementPhoto myPhoto in _project.Photos)
            {
                if (MainForm.TryParseDouble(textBox_SectionLength.Text, out var tempValue))
                {
                    myPhoto.PerspectiveCorrection.Length = tempValue;
                }

                if (MainForm.TryParseDouble(textBox_PavementWidth.Text, out tempValue))
                {
                    myPhoto.PerspectiveCorrection.PavementWidth = tempValue;
                }

                myPhoto.PerspectiveCorrection.ColCount = (int)numericUpDown_Col.Value;
                myPhoto.PerspectiveCorrection.RowCount = (int)numericUpDown_Row.Value;

                UpdateNet();
            }
        }

        private void button_ErrorExport_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = "Hibák exportálása szövegfájlba";
            dialog.Filter = "Szövegfájlok | *.txt";

            if (_project.ProjectPath != "")
            {
                if (Directory.Exists(_project.ProjectPath))
                {
                    dialog.InitialDirectory = _project.ProjectPath;
                }
            }

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _project.ExportErrors(dialog.FileName);
            }
        }

        private void button_ImportFrodo_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "FRODO log fájl importálása";
            dialog.Multiselect = false;
            dialog.Filter = "FRODO Log File | *.log";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _project.ImportFRODO(dialog.FileName);
            }
        }

        private void button_Half_Click(object sender, EventArgs e)
        {
            ScaleSection(0.5);
        }

        private void button_Double_Click(object sender, EventArgs e)
        {
            ScaleSection(2.0);
        }

        private void ScaleSection(double scale)
        {
            if (_project.ActualPhoto == null)
                return;

            _project.ActualPhoto.PerspectiveCorrection.PavementWidth *= scale;
            _project.ActualPhoto.PerspectiveCorrection.Length *= scale;
            ScaleLength(scale);
            ScaleWidth(scale);
            UpdateDrawing();
        }

        void ScaleWidth(double scale)
        {
            foreach (var error in _project.ActualPhoto.Errors)
            {
                error.Left *= scale;
                error.Right *= scale;
            }
            drawingArea.RenderNeeded = true;
        }

        void ScaleLength(double scale)
        {
            foreach (var error in _project.ActualPhoto.Errors)
            {
                error.StartSection = _project.ActualPhoto.Section + scale * (error.StartSection - _project.ActualPhoto.Section);
                error.EndSection = _project.ActualPhoto.Section + scale * (error.EndSection - _project.ActualPhoto.Section);
            }
            drawingArea.RenderNeeded = true;
        }

        private void button_PhotoRename_Click(object sender, EventArgs e)
        {
            var form = new RenameForm();
            form.Show();
        }

        private void button_DrawErrors_Click(object sender, EventArgs e)
        {
            _project.RenderErrorsToBitmap();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            _project.ExportTechnology("techno.txt");
        }

        private void DeleteSelectedError()
        {
            SurfaceError error = drawingArea.SelectedError;
            PavementPhoto photo = _project.ActualPhoto;
            if (error == null || photo == null)
                return;

            int index = photo.Errors.IndexOf(error);
            if (index < 0)
                return;

            photo.Errors.RemoveAt(index);
            drawingArea.ClearErrorSelection();
            RecordOperation(
                () => photo.Errors.Insert(Math.Min(index, photo.Errors.Count), error),
                () => photo.Errors.Remove(error));
        }

        private void RecordOperation(Action undo, Action redo)
        {
            _undoStack.Push(new EditOperation(undo, redo));
            _redoStack.Clear();
            drawingArea.RenderNeeded = true;
        }

        private void ClearEditHistory()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            drawingArea.ClearErrorSelection();
        }

        private void Undo()
        {
            if (_undoStack.Count == 0)
                return;

            EditOperation operation = _undoStack.Pop();
            operation.Undo();
            _redoStack.Push(operation);
            drawingArea.ClearErrorSelection();
            drawingArea.RenderNeeded = true;
        }

        private void Redo()
        {
            if (_redoStack.Count == 0)
                return;

            EditOperation operation = _redoStack.Pop();
            operation.Redo();
            _undoStack.Push(operation);
            drawingArea.ClearErrorSelection();
            drawingArea.RenderNeeded = true;
        }

        private static bool BoundsEqual(SurfaceError first, SurfaceError second)
        {
            const double tolerance = 1e-9;
            return Math.Abs(first.Left - second.Left) < tolerance
                && Math.Abs(first.Right - second.Right) < tolerance
                && Math.Abs(first.StartSection - second.StartSection) < tolerance
                && Math.Abs(first.EndSection - second.EndSection) < tolerance;
        }

        private sealed class EditOperation
        {
            public Action Undo { get; }
            public Action Redo { get; }

            public EditOperation(Action undo, Action redo)
            {
                Undo = undo;
                Redo = redo;
            }
        }
    }
}
