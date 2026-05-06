using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace PPR
{
    public partial class MainForm : Form
    {
        Project _project = new Project();

        public MainForm()
        {
            InitializeComponent();

            Load += new EventHandler(MainForm_Load);
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
            if (e.Command < 4 && e.Command > 0)
            {
                bool doNormalize = true;
                for (int i = 0; i < 4; i++)
                {
                    if (drawingArea.Lines[i].P1.Y == 0)
                    {
                        doNormalize = false;
                        break;
                    }
                }

                if (e.Command == 3 || e.Command == 4)
                {
                    if (e.SubCommand == 0) doNormalize = false;
                }

                if (doNormalize)
                {
                    Normalize();
                }
            }

            if (e.Command == 10)
            {
                if (e.SubCommand == 1)
                {
                    double pavementWidth_2 = _project.ActualPhoto.PerspectiveCorrection.PavementWidth / 2.0;
                    double startSection = _project.ActualPhoto.Section;
                    Pos realPos0 = _project.ActualPhoto.PerspectiveCorrection.GetRealPosition(drawingArea.AreaPos0);
                    Pos realPos1 = _project.ActualPhoto.PerspectiveCorrection.GetRealPosition(drawingArea.AreaPos1);
                    drawingArea.ActualError.Left = Math.Min(realPos0.X, realPos1.X) - pavementWidth_2;
                    drawingArea.ActualError.Right = Math.Max(realPos0.X, realPos1.X) - pavementWidth_2;
                    drawingArea.ActualError.StartSection = Math.Min(realPos0.Y, realPos1.Y) + startSection;
                    drawingArea.ActualError.EndSection = Math.Max(realPos0.Y, realPos1.Y) + startSection;
                    _project.ActualPhoto.Errors.Add(drawingArea.ActualError);

                    drawingArea.Command = 10;
                    ErrorCodes tempCode = drawingArea.ActualError.ErrorCode;
                    drawingArea.ActualError = new SurfaceError();
                    drawingArea.ActualError.ErrorCode = tempCode;
                }
            }

            if (e.Command == 11)
            {
                if (_project.ActualPhoto != null)
                {
                    double pavementWidth_2 = _project.ActualPhoto.PerspectiveCorrection.PavementWidth / 2.0;
                    double startSection = _project.ActualPhoto.Section;
                    Pos realPos = _project.ActualPhoto.PerspectiveCorrection.GetRealPosition(new Pos(e.MouseX, e.MouseY));
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
                        _project.ActualPhoto.Errors.Remove(deletedError);
                        drawingArea.RenderNeeded = true;
                    }
                }
            }

        }

        void drawingArea_OnMouseMove(object sender, MouseMoveEventArgs e)
        {
            if (_project.ActualPhoto == null) return;
            Pos screenPosition = new Pos(e.MouseX, e.MouseY);
            if (_project.ActualPhoto.PerspectiveCorrection.Normalized)
            {
                Pos realPosition = _project.ActualPhoto.PerspectiveCorrection.GetRealPosition(screenPosition);
                label_Coords.Text = realPosition.X.ToString("0.00") + " ; " + realPosition.Y.ToString("0.00");

                Pos screenPosition2 = _project.ActualPhoto.PerspectiveCorrection.GetScreenPosition(realPosition);
                label_Coords.Text += "   " + (screenPosition2.X).ToString("0.00") + " ; " + (screenPosition2.Y).ToString("0.00");
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

        private void button_Normalize_Click(object sender, EventArgs e)
        {
            Line farLine = drawingArea.Lines[0];
            Line nearLine = drawingArea.Lines[1];
            Line leftLine = drawingArea.Lines[2];
            Line rightLine = drawingArea.Lines[3];

            double nearDistance = nearLine.P0.Y;
            double farDistance = farLine.P0.Y;

            if (leftLine.P0.X > leftLine.P1.X)
            {
                Pos temp = new Pos(leftLine.P1.X, leftLine.P1.Y);
                leftLine.P1 = leftLine.P0;
                leftLine.P0 = temp;
            }
            if (rightLine.P0.X < rightLine.P1.X)
            {
                Pos temp = new Pos(rightLine.P1.X, rightLine.P1.Y);
                rightLine.P1 = rightLine.P0;
                rightLine.P0 = temp;
            }

            double dx = leftLine.P1.X - leftLine.P0.X;
            double dy = leftLine.P0.Y - leftLine.P1.Y;

            double dyFar = leftLine.P1.Y - farDistance;
            double dxFar = dyFar * dx / dy;

            double xFarLeft = leftLine.P1.X + dxFar;
            leftLine.P1.X = xFarLeft;
            leftLine.P1.Y = farDistance;
            farLine.P0.X = xFarLeft;

            double dyNear = leftLine.P1.Y - nearDistance;
            double dxNear = dyNear * dx / dy;

            double xNearLeft = leftLine.P1.X + dxNear;
            leftLine.P0.X = xNearLeft;
            leftLine.P0.Y = nearDistance;
            nearLine.P0.X = xNearLeft;


            dx = rightLine.P1.X - rightLine.P0.X;
            dy = rightLine.P0.Y - rightLine.P1.Y;

            dyFar = rightLine.P1.Y - farDistance;
            dxFar = dyFar * dx / dy;

            double xFarRight = rightLine.P1.X + dxFar;
            rightLine.P1.X = xFarRight;
            rightLine.P1.Y = farDistance;
            farLine.P1.X = xFarRight;

            dyNear = rightLine.P1.Y - nearDistance;
            dxNear = dyNear * dx / dy;

            double xNearRight = rightLine.P1.X + dxNear;
            rightLine.P0.X = xNearRight;
            rightLine.P0.Y = nearDistance;
            nearLine.P1.X = xNearRight;

            DrawNet();

            drawingArea.RenderNeeded = true;
        }

        void DrawNet()
        {
            drawingArea.Lines.Clear();

            Line[] lines = _project.ActualPhoto.PerspectiveCorrection.GetNet();

            // Far border
            Line newLine = new Line();
            newLine.LineWidth = 2.0;
            newLine.LineColor = Color.LightGreen;
            newLine.P0.X = _project.ActualPhoto.PerspectiveCorrection.LeftEdge.P1.X;
            newLine.P0.Y = _project.ActualPhoto.PerspectiveCorrection.LeftEdge.P1.Y;
            newLine.P1.X = _project.ActualPhoto.PerspectiveCorrection.RightEdge.P1.X;
            newLine.P1.Y = _project.ActualPhoto.PerspectiveCorrection.RightEdge.P1.Y;
            drawingArea.Lines.Add(newLine);

            // Near border
            newLine = new Line();
            newLine.LineWidth = 2.0;
            newLine.LineColor = Color.LightGreen;
            newLine.P0.X = _project.ActualPhoto.PerspectiveCorrection.LeftEdge.P0.X;
            newLine.P0.Y = _project.ActualPhoto.PerspectiveCorrection.LeftEdge.P0.Y;
            newLine.P1.X = _project.ActualPhoto.PerspectiveCorrection.RightEdge.P0.X;
            newLine.P1.Y = _project.ActualPhoto.PerspectiveCorrection.RightEdge.P0.Y;
            drawingArea.Lines.Add(newLine);

            // Left pavement edge
            newLine = new Line();
            newLine.LineWidth = 2.0;
            newLine.LineColor = Color.Pink;
            newLine.P0.X = _project.ActualPhoto.PerspectiveCorrection.LeftEdge.P0.X;
            newLine.P0.Y = _project.ActualPhoto.PerspectiveCorrection.LeftEdge.P0.Y;
            newLine.P1.X = _project.ActualPhoto.PerspectiveCorrection.LeftEdge.P1.X;
            newLine.P1.Y = _project.ActualPhoto.PerspectiveCorrection.LeftEdge.P1.Y;
            drawingArea.Lines.Add(newLine);

            // Right pavement edge
            newLine = new Line();
            newLine.LineWidth = 2.0;
            newLine.LineColor = Color.Pink;
            newLine.P0.X = _project.ActualPhoto.PerspectiveCorrection.RightEdge.P0.X;
            newLine.P0.Y = _project.ActualPhoto.PerspectiveCorrection.RightEdge.P0.Y;
            newLine.P1.X = _project.ActualPhoto.PerspectiveCorrection.RightEdge.P1.X;
            newLine.P1.Y = _project.ActualPhoto.PerspectiveCorrection.RightEdge.P1.Y;
            drawingArea.Lines.Add(newLine);

            // Net lines
            foreach (Line line in lines)
            {
                drawingArea.Lines.Add(line);
            }
        }

        private void ImportPhotos()
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                _project.LoadPhotos(folderDialog.SelectedPath);
                UpdateControls();
            }
        }

        private void SaveProject(string FileName)
        {
            _project.ProjectPath = Path.GetDirectoryName(FileName);
            _project.ProjectFileName = FileName;

            XmlSerializer serializer = new XmlSerializer(typeof(Project));
            XmlTextWriter writer = new XmlTextWriter(FileName, Encoding.UTF8);

            writer.Formatting = Formatting.Indented;
            serializer.Serialize(writer, _project);


            writer.Close();
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
                XmlTextReader reader = new XmlTextReader(dialog.FileName);

                _project = (Project)serializer.Deserialize(reader);
                reader.Close();
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

            if (_project.ActualPhoto != null)
            {
                fileName = _project.ProjectPath + "\\" + _project.ActualPhoto.PhotoFileName + ".jpg";
                Bitmap bmp = new Bitmap(fileName);
                if (drawingArea.Photo != null) drawingArea.Photo.Dispose();
                drawingArea.Photo = bmp;

                drawingArea.PavementPhoto = _project.ActualPhoto;

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
                        _project.ErrorLayers.Add(myLayer);
                    }
                }

                if (_project.ActualPhoto.PerspectiveCorrection.Normalized)
                {
                    DrawNet();
                }
                else
                {
                    drawingArea.ClearLines();
                }
            }
            else
            {
                drawingArea.PavementPhoto = null;
            }

            drawingArea.RenderNeeded = true;
        }

        private void button_ImportPhotos_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
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

        void CalcMeasureLength(double measureLength)
        {
            double.TryParse(textBox_MeasureBase.Text, out var baseLength);
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

            double.TryParse(textBox_SectionLength.Text, out var length);
            double.TryParse(textBox_PavementWidth.Text, out var width);

            _project.ActualPhoto.PerspectiveCorrection.Length = length;
            _project.ActualPhoto.PerspectiveCorrection.PavementWidth = width;

            _project.ActualPhoto.PerspectiveCorrection.Normalize(lines, length, width);

            DrawNet();

            drawingArea.RenderNeeded = true;
        }

        private void button_Normalize_Click_1(object sender, EventArgs e)
        {
            Normalize();
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
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    drawingArea.Command = 0;
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
                if (double.TryParse(textBox_SectionLength.Text, out tempValue))
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
                if (double.TryParse(textBox_PavementWidth.Text, out tempValue))
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
                if (double.TryParse(textBox_SectionLength.Text, out var tempValue))
                {
                    myPhoto.PerspectiveCorrection.Length = tempValue;
                }

                if (double.TryParse(textBox_PavementWidth.Text, out tempValue))
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
            _project.ActualPhoto.PerspectiveCorrection.PavementWidth *= 0.5;
            _project.ActualPhoto.PerspectiveCorrection.Length *= 0.5;
            ScaleLength(0.5);
            ScaleWidth(0.5);
            UpdateDrawing();
        }

        private void button_Double_Click(object sender, EventArgs e)
        {
            ScaleLength(2.0);
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
    }
}
