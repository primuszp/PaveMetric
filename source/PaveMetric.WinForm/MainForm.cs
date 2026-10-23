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
using System.Linq;

namespace PaveMetric
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
        ErrorTypeToolbox _errorTypeToolbox;
        ToolStripButton _button_ErrorTypes;
        readonly ToolTip toolTip_Dimensions = new ToolTip();

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

            Theme.Apply(this);
            combo_Section.ForeColor = Theme.Accent;
            combo_Section.BackColor = Theme.Surface;

            drawingArea.OnDrawingAreaMouseMove += new DrawingArea.MouseMoveEventHandler(drawingArea_OnMouseMove);
            drawingArea.OnCommandStateChanged += new DrawingArea.CommandEventHAndler(drawingArea_OnCommandStateChanged);
            drawingArea.ErrorEditCompleted += drawingArea_ErrorEditCompleted;
            drawingArea.CurveEditCompleted += drawingArea_CurveEditCompleted;

            foreach (ErrorType type in ErrorCatalog.Load())
                AddErrorLayer(type);
            ApplyProjectErrorTypes();

            // errorLayerGroup stays in the left panel (palette); the toolbox is a
            // separate floating editor for the error catalog (code, name, color).
            _errorTypeToolbox = new ErrorTypeToolbox(errorLayerGroup);
            _errorTypeToolbox.CountUsage = CountErrorsWithCode;
            _errorTypeToolbox.IsEnabledInProject = code => _project.IsErrorTypeEnabled(code);
            _errorTypeToolbox.EnabledInProjectChanged += (layer, enabled) =>
            {
                _project.SetErrorTypeEnabled(layer.ErrorCode, enabled);
                ApplyProjectErrorTypes();
            };
            _errorTypeToolbox.UserClosed += (s, e) => _button_ErrorTypes.Checked = false;
            _errorTypeToolbox.TypeAdded += newLayer =>
            {
                WireErrorLayer(newLayer);
                // a type created while working on a project is meant for that project
                _project.SetErrorTypeEnabled(newLayer.ErrorCode, true);
                ApplyProjectErrorTypes();
            };
            _errorTypeToolbox.TypeRemoved += layer =>
            {
                _project.ErrorLayers.Remove(layer);
                drawingArea.ErrorLayers.Remove(layer);
                if (drawingArea.ActualError?.ErrorCode == layer.ErrorCode)
                    drawingArea.Command = 0;
                drawingArea.RenderNeeded = true;
            };
            _errorTypeToolbox.CodeChanged += RemapErrorCode;
            _errorTypeToolbox.CatalogChanged += (s, e) =>
            {
                SaveErrorCatalog();
                drawingArea.RenderNeeded = true;
            };

            _button_ErrorTypes = new ToolStripButton("Hibatípusok")
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                AutoToolTip = false,
                ToolTipText = "Projekt hibatípusainak kiválasztása és a hibakatalógus szerkesztése",
                CheckOnClick = true,
            };
            _button_ErrorTypes.CheckedChanged += (s, e) =>
            {
                if (_button_ErrorTypes.Checked)
                {
                    _errorTypeToolbox.Populate();
                    if (!_errorTypeToolbox.Visible)
                    {
                        // Keep the entire toolbox on the owner's monitor, even when
                        // the main window is maximized or close to a screen edge.
                        Rectangle workingArea = Screen.FromControl(this).WorkingArea;
                        _errorTypeToolbox.Size = new Size(
                            Math.Min(_errorTypeToolbox.Width, workingArea.Width),
                            Math.Min(_errorTypeToolbox.Height, workingArea.Height));
                        int x = Right + 4;
                        if (x + _errorTypeToolbox.Width > workingArea.Right)
                            x = Right - _errorTypeToolbox.Width;
                        _errorTypeToolbox.Location = new Point(
                            Math.Clamp(x, workingArea.Left,
                                Math.Max(workingArea.Left, workingArea.Right - _errorTypeToolbox.Width)),
                            Math.Clamp(Top + 80, workingArea.Top,
                                Math.Max(workingArea.Top, workingArea.Bottom - _errorTypeToolbox.Height)));
                    }
                    _errorTypeToolbox.Show(this);
                    _errorTypeToolbox.Activate();
                }
                else
                {
                    _errorTypeToolbox.Hide();
                }
            };

            toolStrip1.Items.Add(new ToolStripSeparator());
            toolStrip1.Items.Add(_button_ErrorTypes);
        }

        ErrorLayerControl AddErrorLayer(ErrorType type)
        {
            var layer = new ErrorLayerControl(errorLayerGroup)
            {
                ErrorCode = type.Code,
                LayerName = type.Name,
                LayerColor = type.Color,
                IsVisible = true,
            };
            errorLayerGroup.AddLayer(layer);
            WireErrorLayer(layer);
            return layer;
        }

        void WireErrorLayer(ErrorLayerControl layer)
        {
            layer.OnActionButtonClick += new EventHandler(ErrorLayer_OnActionButtonClick);
            layer.OnVisibleStateChanged += new EventHandler(ErrorLayer_OnVisibleStateChanged);
            layer.OnDeleteButtonClick += new EventHandler(ErrorLayer_OnDeleteButtonClick);
            if (!_project.ErrorLayers.Contains(layer))
                _project.ErrorLayers.Add(layer);
            if (!drawingArea.ErrorLayers.Contains(layer))
                drawingArea.ErrorLayers.Add(layer);
        }

        void SaveErrorCatalog()
        {
            ErrorCatalog.Save(errorLayerGroup.ErrorLayers.Select(l => l.ToErrorType()));
        }

        int CountErrorsWithCode(int code)
        {
            return _project.Photos.Sum(p => p.Errors.Count(e => e.ErrorCode == code));
        }

        /// <summary>Moves every recorded error of <paramref name="oldCode"/> to <paramref name="newCode"/>.</summary>
        void RemapErrorCode(int oldCode, int newCode)
        {
            foreach (PavementPhoto photo in _project.Photos)
                foreach (SurfaceError error in photo.Errors)
                    if (error.ErrorCode == oldCode)
                        error.ErrorCode = newCode;

            if (drawingArea.ActualError != null && drawingArea.ActualError.ErrorCode == oldCode)
                drawingArea.ActualError.ErrorCode = newCode;

            if (_project.ErrorTypeCodes.Remove(oldCode))
                _project.ErrorTypeCodes.Add(newCode);

            // undo/redo entries may hold errors that are not on any photo right now and so
            // still carry the old code; replaying them would resurrect orphaned errors
            ClearEditHistory();
            drawingArea.RenderNeeded = true;
        }

        /// <summary>
        /// Shows in the palette only the types selected for the current project (plus any type
        /// that already has recorded errors), and moves the active type off a hidden one.
        /// </summary>
        void ApplyProjectErrorTypes()
        {
            foreach (ErrorLayerControl layer in errorLayerGroup.ErrorLayers)
                layer.Visible = _project.IsErrorTypeShown(layer.ErrorCode);
            errorLayerGroup.SetEmptyHint(errorLayerGroup.ErrorLayers.Any(l => _project.IsErrorTypeShown(l.ErrorCode))
                ? null
                : "Ehhez a projekthez nincs hibatípus kiválasztva.\n\nA Hibatípusok gombbal pipálja be a használni kívánt típusokat.");

            ErrorLayerControl active = errorLayerGroup.ActiveLayer;
            if (active == null || !_project.IsErrorTypeShown(active.ErrorCode))
            {
                var first = errorLayerGroup.ErrorLayers.FirstOrDefault(l => _project.IsErrorTypeShown(l.ErrorCode));
                if (first != null)
                    errorLayerGroup.SetActiveLayer(first);
                else if (active != null)
                    active.IsActive = false;
            }

            if (drawingArea.ActualError != null && !_project.IsErrorTypeShown(drawingArea.ActualError.ErrorCode))
                drawingArea.Command = 0;
            drawingArea.RenderNeeded = true;
        }

        /// <summary>
        /// Adds a catalog entry for every error code used by the loaded project that the
        /// catalog does not know, so those errors stay visible and editable.
        /// </summary>
        void EnsureLayersForProjectCodes()
        {
            var known = new HashSet<int>(errorLayerGroup.ErrorLayers.Select(l => l.ErrorCode));
            var missing = _project.Photos.SelectMany(p => p.Errors).Select(e => e.ErrorCode)
                .Where(c => !known.Contains(c)).Distinct().OrderBy(c => c).ToList();
            if (missing.Count == 0)
                return;

            foreach (int code in missing)
            {
                string name = Enum.IsDefined(typeof(ErrorCodes), code)
                    ? ErrorCatalog.Defaults().FirstOrDefault(t => t.Code == code)?.Name ?? ((ErrorCodes)code).ToString()
                    : $"Ismeretlen hibatípus ({code})";
                AddErrorLayer(new ErrorType(code, name, Color.Gray));
            }
            SaveErrorCatalog();
            _errorTypeToolbox.Populate();

            MessageBox.Show(this,
                "A projekt olyan hibakódokat is tartalmaz, amelyek nem szerepeltek a hibakatalógusban (" +
                string.Join(", ", missing) + "). Ezeket felvettem a katalógusba; a nevük és színük a " +
                "Hibatípusok ablakban módosítható.",
                "Hibakatalógus", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            drawingArea.RenderNeeded = true;
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
            if (e.Command == 6)
            {
                MarkPerspectiveDirty();
                return;
            }

            if (e.Command <= 4 && e.Command > 0)
            {
                if (e.Command == 3 && e.SubCommand == 1)
                    SetCurveFromLine(drawingArea.LeftEdgeCurvePoints, drawingArea.Lines[2], drawingArea.Lines[1].P0.Y, drawingArea.Lines[0].P0.Y);
                else if (e.Command == 4 && e.SubCommand == 1)
                    SetCurveFromLine(drawingArea.RightEdgeCurvePoints, drawingArea.Lines[3], drawingArea.Lines[1].P0.Y, drawingArea.Lines[0].P0.Y);
                else if (e.Command == 1 || e.Command == 2)
                    // keep the edges ending on the moved boundary so the change is visible right away
                    SnapCurveEndpointsToBoundaries(
                        drawingArea.LeftEdgeCurvePoints, drawingArea.RightEdgeCurvePoints,
                        drawingArea.Lines[1], drawingArea.Lines[0]);

                // the photo now needs re-normalizing; the old correction still locates the
                // recorded errors, so Normalize can carry them over to the new geometry
                if (e.SubCommand == 1)
                    MarkPerspectiveDirty();
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
                    drawingArea.ActualError.Left = drawingArea.SnapX(Math.Min(realPos0.X, realPos1.X) - pavementWidth_2);
                    drawingArea.ActualError.Right = drawingArea.SnapX(Math.Max(realPos0.X, realPos1.X) - pavementWidth_2);
                    drawingArea.ActualError.StartSection = drawingArea.SnapY(Math.Min(realPos0.Y, realPos1.Y) + startSection);
                    drawingArea.ActualError.EndSection = drawingArea.SnapY(Math.Max(realPos0.Y, realPos1.Y) + startSection);
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
                    double startSection = _project.ActualPhoto.Section;
                    SurfaceError deletedError = null;
                    foreach (SurfaceError myError in _project.ActualPhoto.Errors)
                    {
                        if (myError.ErrorCode == drawingArea.ActualError.ErrorCode)
                        {
                            Pos imagePoint = drawingArea.TopViewEnabled
                                ? _project.ActualPhoto.PerspectiveCorrection.GetScreenPosition(
                                    drawingArea.ViewToRealPosition(new Pos(e.MouseX, e.MouseY)))
                                : new Pos(e.MouseX, e.MouseY);
                            if (myError.Contains(_project.ActualPhoto.PerspectiveCorrection, startSection, imagePoint))
                            {
                                deletedError = myError;
                                break;
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
            List<Pos> companion = e.CompanionPoints;
            List<Pos> companionBefore = e.CompanionBefore;
            List<Pos> companionAfter = e.CompanionAfter;
            RecordOperation(
                () =>
                {
                    RestorePoints(points, before);
                    if (companion != null && companionBefore != null)
                        RestorePoints(companion, companionBefore);
                    MarkPerspectiveDirty();
                },
                () =>
                {
                    RestorePoints(points, after);
                    if (companion != null && companionAfter != null)
                        RestorePoints(companion, companionAfter);
                    MarkPerspectiveDirty();
                });
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
            // Restore photo-coordinate boundary lines before invalidating the model.
            if (drawingArea.TopViewEnabled)
                SetTopView(false);

            if (_project.ActualPhoto != null)
                _project.ActualPhoto.PerspectiveCorrection.Normalized = false;

            if (drawingArea.Lines.Count > 4)
                drawingArea.Lines.RemoveRange(4, drawingArea.Lines.Count - 4);

            DisposeTopViewBitmap();
            button_ToggleView.Enabled = false;
            button_ToggleView.Checked = false;
            button_Normalize.Checked = false;
            button_Normalize.BackColor = Theme.Warning;
            button_Normalize.ToolTipText = "A szegélyek módosultak — normalizálás szükséges.";
            drawingArea.RenderNeeded = true;
        }

        private void ClearPerspectiveDirty()
        {
            button_Normalize.BackColor = Theme.Surface;
            button_Normalize.ToolTipText = string.Empty;
            button_Normalize.Checked = true;
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
                EnsureLayersForProjectCodes();
                _project.EnableUsedErrorTypes();
                ApplyProjectErrorTypes();
                _errorTypeToolbox.Populate();
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
                // Load and publish the replacement before disposing the current images.
                // DrawingArea may repaint after an Invalidate call, so it must never retain
                // a reference to an already disposed bitmap.
                Bitmap previousPerspectiveBitmap = _perspectiveBitmap;
                Bitmap previousTopViewBitmap = _topViewBitmap;
                Bitmap replacementPerspectiveBitmap = LoadPhotoBitmap(fileName);

                _perspectiveBitmap = replacementPerspectiveBitmap;
                _topViewBitmap = null;
                drawingArea.Photo = _perspectiveBitmap;
                drawingArea.FitImageToView();

                previousTopViewBitmap?.Dispose();
                previousPerspectiveBitmap?.Dispose();
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
                UpdateDimensionWarnings();


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

                // a freshly loaded project starts without layer references
                foreach (ErrorLayerControl myLayer in errorLayerGroup.ErrorLayers)
                {
                    if (!drawingArea.ErrorLayers.Contains(myLayer))
                        drawingArea.ErrorLayers.Add(myLayer);
                    if (!_project.ErrorLayers.Contains(myLayer))
                        _project.ErrorLayers.Add(myLayer);
                }

                if (_project.ActualPhoto.PerspectiveCorrection.Normalized)
                {
                    DrawNet();
                    button_ToggleView.Enabled = true;
                    button_Normalize.Checked = true;
                    button_Normalize.BackColor = Theme.Surface;
                    UpdateRoadModelWarning();
                }
                else
                {
                    drawingArea.ClearLines();
                    button_ToggleView.Enabled = false;
                    button_Normalize.Checked = false;
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
            if (_project.ActualPhoto?.PerspectiveCorrection.Normalized != true)
            {
                MessageBox.Show(this, "A szélesség beállítása előtt normalizálja a képet.",
                    "Mérés", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!TryParseDouble(textBox_MeasureBase.Text, out double baseLength)
                || !double.IsFinite(baseLength) || baseLength <= 0)
            {
                MessageBox.Show(this, "Adja meg a keresztirányú bázis pozitív hosszát milliméterben.",
                    "Mérés", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
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

        void CalcMeasureLength(Pos start, Pos end)
        {
            var correction = _project.ActualPhoto?.PerspectiveCorrection;
            if (correction == null)
                return;
            Pos realStart = drawingArea.ViewToRealPosition(start);
            Pos realEnd = drawingArea.ViewToRealPosition(end);
            bool IsInsideMappedSection(Pos view, Pos real)
            {
                // Curved inverse mapping clamps longitudinal station at the boundaries.
                // Reject points outside them rather than calibrating from clamped values.
                Pos projected = drawingArea.RealToViewPosition(real);
                return Math.Abs(projected.X - view.X) <= 1.0
                    && Math.Abs(projected.Y - view.Y) <= 1.0;
            }
            if (!TryParseDouble(textBox_MeasureBase.Text, out double baseLength)
                || !IsInsideMappedSection(start, realStart) || !IsInsideMappedSection(end, realEnd)
                || !correction.TryCalibrateWidthFromImageReference(
                    drawingArea.TopViewEnabled ? correction.GetScreenPosition(realStart) : start,
                    drawingArea.TopViewEnabled ? correction.GetScreenPosition(realEnd) : end,
                    baseLength / 1000.0, out double width)
                || Math.Round(width, 2) <= 0.0)
            {
                MessageBox.Show(this,
                    "A burkolaton belül, ugyanabban a keresztmetszetben jelölje ki a bázis két különböző pontját. " +
                    "A bázishossz pozitív szám legyen (mm).",
                    "Mérés", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            textBox_PavementWidth.Text = Math.Round(width, 2).ToString("0.00", CultureInfo.CurrentCulture);
            Normalize();
        }

        /// <summary>
        /// Highlights the section length / pavement width fields while they are missing or invalid.
        /// </summary>
        void UpdateDimensionWarnings()
        {
            const string tip = "Adja meg az értéket (m) — enélkül nem lehet normalizálni.";
            foreach (TextBox box in new[] { textBox_SectionLength, textBox_PavementWidth })
            {
                bool ok = TryParseDouble(box.Text, out var value) && value > 0;
                box.BackColor = ok ? Theme.Surface : Theme.Warning;
                toolTip_Dimensions.SetToolTip(box, ok ? string.Empty : tip);
            }
        }

        /// <summary>
        /// Checks that the section length and pavement width are set to positive values;
        /// shows a warning and focuses the offending field otherwise.
        /// </summary>
        bool ValidateNormalizeInputs(out double length, out double width)
        {
            bool lengthOk = TryParseDouble(textBox_SectionLength.Text, out length) && length > 0;
            bool widthOk = TryParseDouble(textBox_PavementWidth.Text, out width) && width > 0;
            if (lengthOk && widthOk)
                return true;

            var missing = new List<string>();
            if (!lengthOk) missing.Add("szakaszhossz");
            if (!widthOk) missing.Add("burkolatszélesség");

            MessageBox.Show(this,
                "A normalizáláshoz meg kell adni a(z) " + string.Join(" és a(z) ", missing) +
                " értékét (pozitív szám, méterben).",
                "Hiányzó adat", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            TextBox target = lengthOk ? textBox_PavementWidth : textBox_SectionLength;
            target.Focus();
            target.SelectAll();
            return false;
        }

        bool Normalize()
        {
            if (_project.ActualPhoto == null)
                return false;
            if (!ValidateNormalizeInputs(out var length, out var width))
                return false;
            Action restoreBefore = CaptureNormalizationState();
            var previous = _project.ActualPhoto.PerspectiveCorrection;
            var replacement = new PerspectiveCorrection { RowCount = previous.RowCount, ColCount = previous.ColCount };

            Line[] lines = new Line[4];
            for (int i = 0; i < 4; i++)
            {
                lines[i] = drawingArea.Lines[i];
            }

            SnapCurveEndpointsToBoundaries(
                drawingArea.LeftEdgeCurvePoints, drawingArea.RightEdgeCurvePoints,
                drawingArea.Lines[1], drawingArea.Lines[0]);

            if (HasCurvedEdges())
            {
                replacement.NormalizeCurved(
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

                replacement.Normalize(lines, length, width);
            }
            if (!_project.ActualPhoto.TryReplaceCorrection(replacement, out string reason))
            {
                restoreBefore();
                MessageBox.Show(this, reason, "Normalizálás", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            DisposeTopViewBitmap();

            ClearPerspectiveDirty();
            UpdateRoadModelWarning();
            DrawNet();

            drawingArea.RenderNeeded = true;
            RecordOperation(restoreBefore, CaptureNormalizationState());
            return true;
        }

        private Action CaptureNormalizationState()
        {
            PavementPhoto photo = _project.ActualPhoto;
            PerspectiveCorrection correction = photo.PerspectiveCorrection;
            bool normalized = correction.Normalized;
            var errors = photo.Errors.Select(error => (Error: error, Bounds: error.Clone())).ToArray();
            var left = ClonePoints(drawingArea.LeftEdgeCurvePoints);
            var right = ClonePoints(drawingArea.RightEdgeCurvePoints);
            Line CopyLine(Line line) => CreateLine(new Pos(line.P0.X, line.P0.Y),
                new Pos(line.P1.X, line.P1.Y), line.LineColor, line.LineWidth);
            var lines = drawingArea.Lines.Select(CopyLine).ToArray();
            string lengthText = textBox_SectionLength.Text, widthText = textBox_PavementWidth.Text;
            Color buttonColor = button_Normalize.BackColor;
            string tooltip = button_Normalize.ToolTipText;
            return () =>
            {
                if (drawingArea.TopViewEnabled) SetTopView(false);
                DisposeTopViewBitmap();
                photo.PerspectiveCorrection = correction;
                correction.Normalized = normalized;
                foreach (var item in errors) item.Error.CopyBoundsFrom(item.Bounds);
                RestorePoints(drawingArea.LeftEdgeCurvePoints, left);
                RestorePoints(drawingArea.RightEdgeCurvePoints, right);
                drawingArea.Lines.Clear();
                drawingArea.Lines.AddRange(lines.Select(CopyLine));
                textBox_SectionLength.Text = lengthText;
                textBox_PavementWidth.Text = widthText;
                button_Normalize.Checked = correction.Normalized;
                button_Normalize.BackColor = buttonColor;
                button_Normalize.ToolTipText = tooltip;
                button_ToggleView.Enabled = correction.Normalized;
                drawingArea.ClearErrorSelection();
                drawingArea.RenderNeeded = true;
            };
        }

        private void UpdateRoadModelWarning()
        {
            button_Normalize.ToolTipText = string.Empty;
            var correction = _project.ActualPhoto?.PerspectiveCorrection;
            if (correction?.Normalized != true || !HasCurvedEdges()) return;
            if (correction.RoadModelVersion == 0)
            {
                button_Normalize.BackColor = Theme.Warning;
                button_Normalize.ToolTipText = "A fénykép a korábbi görbemodellt használja. " +
                    "Az újranormalizálás a rögzített hibákat is automatikusan átvetíti.";
            }
            else if (!correction.UsesMetricRoadPlane)
            {
                button_Normalize.BackColor = Theme.Warning;
                button_Normalize.ToolTipText = "A szélekre nem illeszthető megbízható, állandó szélességű síkbeli modell. " +
                    "A hossz és a terület csak közelítő; ellenőrizze a széleket és a megadott méreteket.";
            }
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

        private static void SnapCurveEndpointsToBoundaries(
            List<Pos> leftPoints, List<Pos> rightPoints,
            Line nearBoundary, Line farBoundary)
        {
            foreach (List<Pos> points in new[] { leftPoints, rightPoints })
            {
                if (points == null || points.Count < 2)
                    continue;
                MoveEndpointOntoLine(points[0], points[1], nearBoundary);
                MoveEndpointOntoLine(points[points.Count - 1], points[points.Count - 2], farBoundary);
            }
        }

        /// <summary>
        /// Slides an edge endpoint along its end segment (through <paramref name="neighbor"/>)
        /// until it lies on <paramref name="boundary"/>, so a moved boundary extends or shortens
        /// the edge instead of bending it. Falls back to moving only Y when they are parallel.
        /// </summary>
        private static void MoveEndpointOntoLine(Pos endpoint, Pos neighbor, Line boundary)
        {
            double bx = boundary.P1.X - boundary.P0.X, by = boundary.P1.Y - boundary.P0.Y;
            double ex = endpoint.X - neighbor.X, ey = endpoint.Y - neighbor.Y;
            double determinant = ex * by - ey * bx;
            if (Math.Abs(determinant) > 1e-9)
            {
                double t = ((boundary.P0.X - neighbor.X) * by - (boundary.P0.Y - neighbor.Y) * bx) / determinant;
                double x = neighbor.X + ex * t, y = neighbor.Y + ey * t;
                if (double.IsFinite(x) && double.IsFinite(y) && t > 0)
                {
                    endpoint.X = x;
                    endpoint.Y = y;
                    return;
                }
            }

            endpoint.Y = Math.Abs(bx) > 1e-9
                ? boundary.P0.Y + by * (endpoint.X - boundary.P0.X) / bx
                : boundary.P0.Y;
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
            if (_project.ActualPhoto?.PerspectiveCorrection.Normalized == true)
            {
                if (drawingArea.TopViewEnabled) SetTopView(false);
                Action restoreBefore = CaptureNormalizationState();
                drawingArea.Command = 0;
                drawingArea.ClearErrorSelection();
                MarkPerspectiveDirty();
                button_Normalize.ToolTipText = "Szerkessze a közeli és távoli határt, valamint a burkolatszéleket, majd normalizáljon újra.";
                RecordOperation(restoreBefore, CaptureNormalizationState());
                return;
            }
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
            drawingArea.FitImageToView();
            button_ToggleView.Checked = enabled;
            button_ToggleView.Text = enabled ? "Perspektíva" : "Felülnézet";

            button_FarDistance.Enabled = !enabled;
            button_NearDistance.Enabled = !enabled;
            button_LeftEdge.Enabled = !enabled;
            button_RightEdge.Enabled = !enabled;
            // Normalization can also be switched off directly from the top view.
            button_Normalize.Enabled = true;
            button_Measure.Enabled = !enabled;

            if (enabled)
                DrawTopViewNet();
            else if (_project.ActualPhoto?.PerspectiveCorrection.Normalized == true)
                DrawNet();

            drawingArea.RenderNeeded = true;
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            // palette color edits do not go through the toolbox, so persist on exit as well
            SaveErrorCatalog();
            drawingArea.Photo = null;
            _topViewBitmap?.Dispose();
            _perspectiveBitmap?.Dispose();
            lock (_prefetchSync)
            {
                _prefetchBitmap?.Dispose();
                _prefetchBitmap = null;
            }
        }

        private void DisposeTopViewBitmap()
        {
            if (ReferenceEquals(drawingArea.Photo, _topViewBitmap))
            {
                drawingArea.TopViewEnabled = false;
                drawingArea.Photo = _perspectiveBitmap;
            }

            _topViewBitmap?.Dispose();
            _topViewBitmap = null;
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
            ApplyDimensionText(textBox_SectionLength, c => c.Length, (c, v) => c.Length = v);
        }

        private void textBox_PavementWidth_TextChanged(object sender, EventArgs e)
        {
            ApplyDimensionText(textBox_PavementWidth, c => c.PavementWidth, (c, v) => c.PavementWidth = v);
        }

        /// <summary>
        /// Takes a typed section length / pavement width. While the photo has recorded errors,
        /// the current geometry is what locates them on the photo, so it must not change under
        /// them: the photo is only marked for re-normalization, and <see cref="Normalize"/>
        /// applies the new value and reprojects the errors.
        /// </summary>
        private void ApplyDimensionText(TextBox box, Func<PerspectiveCorrection, double> get,
            Action<PerspectiveCorrection, double> set)
        {
            UpdateDimensionWarnings();
            PavementPhoto photo = _project.ActualPhoto;
            if (photo == null || !TryParseDouble(box.Text, out double value))
                return;

            PerspectiveCorrection correction = photo.PerspectiveCorrection;
            // the box shows two decimals; that rounding is not a change
            if (Math.Abs(get(correction) - value) < 0.005)
                return;

            if (photo.Errors.Count > 0 && correction.HasUsableCoordinates)
            {
                if (correction.Normalized)
                {
                    MarkPerspectiveDirty();
                    button_Normalize.ToolTipText = "A méretek módosultak — normalizáljon újra; " +
                        "a rögzített hibák az új rendszerbe kerülnek.";
                }
                return;
            }

            set(correction, value);
            UpdateNet();
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
            if (!ValidateNormalizeInputs(out double length, out double width))
                return;

            var skipped = new List<string>();
            PerspectiveCorrection activeBefore = _project.ActualPhoto?.PerspectiveCorrection;
            bool anyReplaced = false;
            foreach (PavementPhoto myPhoto in _project.Photos)
            {
                PerspectiveCorrection correction = myPhoto.PerspectiveCorrection;
                bool changed = Math.Abs(correction.Length - length) >= 0.005
                    || Math.Abs(correction.PavementWidth - width) >= 0.005;

                if (changed && myPhoto.Errors.Count > 0 && correction.HasUsableCoordinates)
                {
                    // rebuild from the same edges and reproject the errors, as Normalize does
                    if (!correction.Normalized
                        || !myPhoto.TryReplaceCorrection(correction.WithDimensions(length, width), out _))
                    {
                        skipped.Add(myPhoto.Section.ToString("0.00"));
                        continue;
                    }
                    anyReplaced = true;
                }
                else if (changed)
                {
                    correction.Length = length;
                    correction.PavementWidth = width;
                }

                myPhoto.PerspectiveCorrection.ColCount = (int)numericUpDown_Col.Value;
                myPhoto.PerspectiveCorrection.RowCount = (int)numericUpDown_Row.Value;
            }

            // undo snapshots refer to the replaced corrections
            if (anyReplaced)
                ClearEditHistory();
            if (_project.ActualPhoto != null && _project.ActualPhoto.PerspectiveCorrection != activeBefore)
            {
                DisposeTopViewBitmap();
                UpdateDrawing();
            }
            else if (_project.ActualPhoto != null)
            {
                UpdateNet();
            }

            if (skipped.Count > 0)
                MessageBox.Show(this,
                    "Ezeknél a szelvényeknél a méretek nem módosultak, mert a rögzített hibák nem " +
                    "vetíthetők át (nincs normalizálva, vagy a hiba az új tartományon kívülre esne): " +
                    string.Join(", ", skipped) + ". Ezeket egyenként normalizálja.",
                    "Alkalmazás mindegyikre", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>
        /// Warns when some photos have no section length / pavement width; their rows are
        /// exported as zero. Returns false if the user cancels the export.
        /// </summary>
        bool ConfirmExportWithMissingDimensions()
        {
            var photos = _project.PhotosWithoutDimensions();
            if (photos.Count == 0)
                return true;

            var list = string.Join(", ", photos.Take(10).Select(p => p.Section.ToString("0.00")));
            if (photos.Count > 10)
                list += $" … (összesen {photos.Count})";

            return MessageBox.Show(this,
                "Az alábbi szelvényeknél nincs megadva a szakaszhossz vagy a burkolatszélesség, " +
                "ezért a hibáik nem számíthatók (0 értékkel kerülnek az exportba):\n\n" + list +
                "\n\nFolytatja az exportot?",
                "Hiányzó adat", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
        }

        private void button_ErrorExport_Click(object sender, EventArgs e)
        {
            if (!ConfirmExportWithMissingDimensions())
                return;

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
            if (!ConfirmExportWithMissingDimensions())
                return;
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
                && Math.Abs(first.EndSection - second.EndSection) < tolerance
                && ((first.ImageOutline == null && second.ImageOutline == null)
                    || (first.ImageOutline != null && second.ImageOutline != null
                        && first.ImageOutline.Count == second.ImageOutline.Count
                        && first.ImageOutline.Zip(second.ImageOutline, (a, b) =>
                            Math.Abs(a.X - b.X) < tolerance && Math.Abs(a.Y - b.Y) < tolerance).All(equal => equal)));
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
