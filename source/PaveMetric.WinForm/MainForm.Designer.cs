namespace PaveMetric
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.projectToolStrip = new System.Windows.Forms.ToolStrip();
            this.button_ImportPhotos = new System.Windows.Forms.ToolStripButton();
            this.button_ImportFrodo = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.button_OpenProject = new System.Windows.Forms.ToolStripButton();
            this.button_SaveProject = new System.Windows.Forms.ToolStripButton();
            this.button_SaveAs = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.button_ErrorExport = new System.Windows.Forms.ToolStripButton();
            this.button_DrawErrors = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.button_PhotoRename = new System.Windows.Forms.ToolStripButton();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.label_Coords = new System.Windows.Forms.ToolStripStatusLabel();
            this.tabControl_Main = new System.Windows.Forms.TabControl();
            this.tabPage_Project = new System.Windows.Forms.TabPage();
            this.tabPage_Rating = new System.Windows.Forms.TabPage();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.button_FirstNonNormalized = new System.Windows.Forms.ToolStripButton();
            this.button_Previous = new System.Windows.Forms.ToolStripButton();
            this.combo_Section = new System.Windows.Forms.ToolStripComboBox();
            this.button_Next = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.button_FarDistance = new System.Windows.Forms.ToolStripButton();
            this.button_NearDistance = new System.Windows.Forms.ToolStripButton();
            this.button_LeftEdge = new System.Windows.Forms.ToolStripButton();
            this.button_RightEdge = new System.Windows.Forms.ToolStripButton();
            this.button_Normalize = new System.Windows.Forms.ToolStripButton();
            this.button_ToggleView = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.button_Measure = new System.Windows.Forms.ToolStripButton();
            this.label_Measure = new System.Windows.Forms.ToolStripLabel();
            this.textBox_MeasureBase = new System.Windows.Forms.ToolStripTextBox();
            this.labelUnit = new System.Windows.Forms.ToolStripLabel();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.button_Double = new System.Windows.Forms.Button();
            this.button_Half = new System.Windows.Forms.Button();
            this.button_ApplyToAll = new System.Windows.Forms.Button();
            this.groupBox_SectionLength = new System.Windows.Forms.GroupBox();
            this.textBox_SectionLength = new System.Windows.Forms.TextBox();
            this.groupBox_PavementWidth = new System.Windows.Forms.GroupBox();
            this.textBox_PavementWidth = new System.Windows.Forms.TextBox();
            this.groupBox_Net = new System.Windows.Forms.GroupBox();
            this.numericUpDown_Row = new System.Windows.Forms.NumericUpDown();
            this.numericUpDown_Col = new System.Windows.Forms.NumericUpDown();
            this.errorLayerGroup = new PaveMetric.ErrorLayerGroup();
            this.drawingArea = new PaveMetric.DrawingArea();
            this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.projectToolStrip.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.tabControl_Main.SuspendLayout();
            this.tabPage_Project.SuspendLayout();
            this.tabPage_Rating.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.groupBox_SectionLength.SuspendLayout();
            this.groupBox_PavementWidth.SuspendLayout();
            this.groupBox_Net.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_Row)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_Col)).BeginInit();
            this.SuspendLayout();
            // 
            // projectToolStrip
            // 
            this.projectToolStrip.AutoSize = false;
            this.projectToolStrip.Dock = System.Windows.Forms.DockStyle.Fill;
            this.projectToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.button_ImportPhotos,
            this.button_ImportFrodo,
            this.toolStripSeparator1,
            this.button_OpenProject,
            this.button_SaveProject,
            this.button_SaveAs,
            this.toolStripSeparator3,
            this.button_ErrorExport,
            this.button_DrawErrors,
            this.toolStripSeparator5,
            this.button_PhotoRename,
            this.toolStripButton1});
            this.projectToolStrip.Location = new System.Drawing.Point(0, 0);
            this.projectToolStrip.Name = "projectToolStrip";
            this.projectToolStrip.Size = new System.Drawing.Size(1012, 33);
            this.projectToolStrip.TabIndex = 1;
            this.projectToolStrip.Text = "toolStrip1";
            // 
            // button_ImportPhotos
            // 
            this.button_ImportPhotos.Image = ((System.Drawing.Image)(resources.GetObject("button_ImportPhotos.Image")));
            this.button_ImportPhotos.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.button_ImportPhotos.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.button_ImportPhotos.Name = "button_ImportPhotos";
            this.button_ImportPhotos.Size = new System.Drawing.Size(130, 30);
            this.button_ImportPhotos.Text = "Fotók importálása";
            this.button_ImportPhotos.Click += new System.EventHandler(this.button_ImportPhotos_Click);
            // 
            // button_ImportFrodo
            // 
            this.button_ImportFrodo.Image = ((System.Drawing.Image)(resources.GetObject("button_ImportFrodo.Image")));
            this.button_ImportFrodo.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.button_ImportFrodo.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.button_ImportFrodo.Name = "button_ImportFrodo";
            this.button_ImportFrodo.Size = new System.Drawing.Size(139, 30);
            this.button_ImportFrodo.Text = "FRODO importálása";
            this.button_ImportFrodo.Click += new System.EventHandler(this.button_ImportFrodo_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 33);
            // 
            // button_OpenProject
            // 
            this.button_OpenProject.Image = ((System.Drawing.Image)(resources.GetObject("button_OpenProject.Image")));
            this.button_OpenProject.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.button_OpenProject.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.button_OpenProject.Name = "button_OpenProject";
            this.button_OpenProject.Size = new System.Drawing.Size(79, 30);
            this.button_OpenProject.Text = "Megnyit";
            this.button_OpenProject.Click += new System.EventHandler(this.button_OpenProject_Click);
            // 
            // button_SaveProject
            // 
            this.button_SaveProject.Image = ((System.Drawing.Image)(resources.GetObject("button_SaveProject.Image")));
            this.button_SaveProject.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.button_SaveProject.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.button_SaveProject.Name = "button_SaveProject";
            this.button_SaveProject.Size = new System.Drawing.Size(63, 30);
            this.button_SaveProject.Text = "Ment";
            this.button_SaveProject.Click += new System.EventHandler(this.button_SaveProject_Click);
            // 
            // button_SaveAs
            // 
            this.button_SaveAs.Image = ((System.Drawing.Image)(resources.GetObject("button_SaveAs.Image")));
            this.button_SaveAs.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.button_SaveAs.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.button_SaveAs.Name = "button_SaveAs";
            this.button_SaveAs.Size = new System.Drawing.Size(122, 30);
            this.button_SaveAs.Text = "Mentés másként";
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 33);
            // 
            // button_ErrorExport
            // 
            this.button_ErrorExport.Image = ((System.Drawing.Image)(resources.GetObject("button_ErrorExport.Image")));
            this.button_ErrorExport.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.button_ErrorExport.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.button_ErrorExport.Name = "button_ErrorExport";
            this.button_ErrorExport.Size = new System.Drawing.Size(129, 30);
            this.button_ErrorExport.Text = "Hibák exportálása";
            this.button_ErrorExport.Click += new System.EventHandler(this.button_ErrorExport_Click);
            // 
            // button_DrawErrors
            // 
            this.button_DrawErrors.Image = ((System.Drawing.Image)(resources.GetObject("button_DrawErrors.Image")));
            this.button_DrawErrors.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.button_DrawErrors.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.button_DrawErrors.Name = "button_DrawErrors";
            this.button_DrawErrors.Size = new System.Drawing.Size(154, 30);
            this.button_DrawErrors.Text = "Hibaképek exportálása";
            this.button_DrawErrors.Click += new System.EventHandler(this.button_DrawErrors_Click);
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            this.toolStripSeparator5.Size = new System.Drawing.Size(6, 33);
            // 
            // button_PhotoRename
            // 
            this.button_PhotoRename.Image = ((System.Drawing.Image)(resources.GetObject("button_PhotoRename.Image")));
            this.button_PhotoRename.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.button_PhotoRename.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.button_PhotoRename.Name = "button_PhotoRename";
            this.button_PhotoRename.Size = new System.Drawing.Size(125, 30);
            this.button_PhotoRename.Text = "Fotók átnevezése";
            this.button_PhotoRename.Click += new System.EventHandler(this.button_PhotoRename_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.label_Coords});
            this.statusStrip1.Location = new System.Drawing.Point(0, 639);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1020, 22);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // label_Coords
            // 
            this.label_Coords.AutoSize = false;
            this.label_Coords.Name = "label_Coords";
            this.label_Coords.Size = new System.Drawing.Size(200, 17);
            this.label_Coords.Text = "0,00 ; 0,00";
            // 
            // tabControl_Main
            // 
            this.tabControl_Main.Controls.Add(this.tabPage_Project);
            this.tabControl_Main.Controls.Add(this.tabPage_Rating);
            this.tabControl_Main.Dock = System.Windows.Forms.DockStyle.Top;
            this.tabControl_Main.Location = new System.Drawing.Point(0, 0);
            this.tabControl_Main.Name = "tabControl_Main";
            this.tabControl_Main.SelectedIndex = 0;
            this.tabControl_Main.Size = new System.Drawing.Size(1020, 59);
            this.tabControl_Main.TabIndex = 4;
            // 
            // tabPage_Project
            // 
            this.tabPage_Project.Controls.Add(this.projectToolStrip);
            this.tabPage_Project.Location = new System.Drawing.Point(4, 22);
            this.tabPage_Project.Name = "tabPage_Project";
            this.tabPage_Project.Size = new System.Drawing.Size(1012, 33);
            this.tabPage_Project.TabIndex = 0;
            this.tabPage_Project.Text = "Projekt";
            this.tabPage_Project.UseVisualStyleBackColor = true;
            // 
            // tabPage_Rating
            // 
            this.tabPage_Rating.Controls.Add(this.toolStrip1);
            this.tabPage_Rating.Location = new System.Drawing.Point(4, 22);
            this.tabPage_Rating.Name = "tabPage_Rating";
            this.tabPage_Rating.Size = new System.Drawing.Size(976, 33);
            this.tabPage_Rating.TabIndex = 1;
            this.tabPage_Rating.Text = "Értékelés";
            this.tabPage_Rating.UseVisualStyleBackColor = true;
            // 
            // toolStrip1
            // 
            this.toolStrip1.AutoSize = false;
            this.toolStrip1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.button_FirstNonNormalized,
            this.button_Previous,
            this.combo_Section,
            this.button_Next,
            this.toolStripSeparator2,
            this.button_FarDistance,
            this.button_NearDistance,
            this.button_LeftEdge,
            this.button_RightEdge,
            this.button_Normalize,
            this.button_ToggleView,
            this.toolStripSeparator4,
            this.button_Measure,
            this.label_Measure,
            this.textBox_MeasureBase,
            this.labelUnit});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(976, 33);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // button_FirstNonNormalized
            // 
            this.button_FirstNonNormalized.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.button_FirstNonNormalized.Image = ((System.Drawing.Image)(resources.GetObject("button_FirstNonNormalized.Image")));
            this.button_FirstNonNormalized.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.button_FirstNonNormalized.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.button_FirstNonNormalized.Name = "button_FirstNonNormalized";
            this.button_FirstNonNormalized.Size = new System.Drawing.Size(28, 30);
            this.button_FirstNonNormalized.Text = "Ugrás az utolsóra";
            this.button_FirstNonNormalized.Click += new System.EventHandler(this.button_FirstNonNormalized_Click);
            // 
            // button_Previous
            // 
            this.button_Previous.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.button_Previous.Image = ((System.Drawing.Image)(resources.GetObject("button_Previous.Image")));
            this.button_Previous.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.button_Previous.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.button_Previous.Name = "button_Previous";
            this.button_Previous.Size = new System.Drawing.Size(28, 30);
            this.button_Previous.Text = "Előző fotó";
            this.button_Previous.Click += new System.EventHandler(this.button_Previous_Click);
            // 
            // combo_Section
            // 
            this.combo_Section.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.combo_Section.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.combo_Section.ForeColor = System.Drawing.Color.Red;
            this.combo_Section.Name = "combo_Section";
            this.combo_Section.Size = new System.Drawing.Size(121, 33);
            this.combo_Section.SelectedIndexChanged += new System.EventHandler(this.combo_Section_SelectedIndexChanged);
            // 
            // button_Next
            // 
            this.button_Next.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.button_Next.Image = ((System.Drawing.Image)(resources.GetObject("button_Next.Image")));
            this.button_Next.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.button_Next.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.button_Next.Name = "button_Next";
            this.button_Next.Size = new System.Drawing.Size(28, 30);
            this.button_Next.Text = "toolStripButton1";
            this.button_Next.ToolTipText = "Következő fotó";
            this.button_Next.Click += new System.EventHandler(this.button_Next_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 33);
            // 
            // button_FarDistance
            // 
            this.button_FarDistance.AutoToolTip = false;
            this.button_FarDistance.Image = ((System.Drawing.Image)(resources.GetObject("button_FarDistance.Image")));
            this.button_FarDistance.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.button_FarDistance.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.button_FarDistance.Name = "button_FarDistance";
            this.button_FarDistance.Size = new System.Drawing.Size(95, 30);
            this.button_FarDistance.Text = "Távoli határ";
            this.button_FarDistance.Click += new System.EventHandler(this.button_FarDistance_Click);
            // 
            // button_NearDistance
            // 
            this.button_NearDistance.AutoToolTip = false;
            this.button_NearDistance.Image = ((System.Drawing.Image)(resources.GetObject("button_NearDistance.Image")));
            this.button_NearDistance.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.button_NearDistance.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.button_NearDistance.Name = "button_NearDistance";
            this.button_NearDistance.Size = new System.Drawing.Size(96, 30);
            this.button_NearDistance.Text = "Közeli határ";
            this.button_NearDistance.Click += new System.EventHandler(this.button_NearDistance_Click);
            // 
            // button_LeftEdge
            // 
            this.button_LeftEdge.AutoToolTip = false;
            this.button_LeftEdge.Image = ((System.Drawing.Image)(resources.GetObject("button_LeftEdge.Image")));
            this.button_LeftEdge.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.button_LeftEdge.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.button_LeftEdge.Name = "button_LeftEdge";
            this.button_LeftEdge.Size = new System.Drawing.Size(73, 30);
            this.button_LeftEdge.Text = "Bal szél";
            this.button_LeftEdge.Click += new System.EventHandler(this.button_LeftEdge_Click);
            // 
            // button_RightEdge
            // 
            this.button_RightEdge.AutoToolTip = false;
            this.button_RightEdge.Image = ((System.Drawing.Image)(resources.GetObject("button_RightEdge.Image")));
            this.button_RightEdge.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.button_RightEdge.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.button_RightEdge.Name = "button_RightEdge";
            this.button_RightEdge.Size = new System.Drawing.Size(82, 30);
            this.button_RightEdge.Text = "Jobb szél";
            this.button_RightEdge.Click += new System.EventHandler(this.button_RightEdge_Click);
            //
            // button_Normalize
            // 
            this.button_Normalize.AutoToolTip = false;
            this.button_Normalize.Image = ((System.Drawing.Image)(resources.GetObject("button_Normalize.Image")));
            this.button_Normalize.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.button_Normalize.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.button_Normalize.Name = "button_Normalize";
            this.button_Normalize.Size = new System.Drawing.Size(92, 30);
            this.button_Normalize.Text = "Normalizál";
            this.button_Normalize.Click += new System.EventHandler(this.button_Normalize_Click);
            //
            // button_ToggleView
            //
            this.button_ToggleView.CheckOnClick = true;
            this.button_ToggleView.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.button_ToggleView.Enabled = false;
            this.button_ToggleView.Name = "button_ToggleView";
            this.button_ToggleView.Size = new System.Drawing.Size(75, 30);
            this.button_ToggleView.Text = "Felülnézet";
            this.button_ToggleView.ToolTipText = "Váltás perspektivikus és felülnézeti kép között";
            this.button_ToggleView.Click += new System.EventHandler(this.button_ToggleView_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(6, 33);
            // 
            // button_Measure
            // 
            this.button_Measure.AutoToolTip = false;
            this.button_Measure.Image = ((System.Drawing.Image)(resources.GetObject("button_Measure.Image")));
            this.button_Measure.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.button_Measure.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.button_Measure.Name = "button_Measure";
            this.button_Measure.Size = new System.Drawing.Size(67, 30);
            this.button_Measure.Text = "Mérés";
            this.button_Measure.Click += new System.EventHandler(this.button_Measure_Click);
            // 
            // label_Measure
            // 
            this.label_Measure.Name = "label_Measure";
            this.label_Measure.Size = new System.Drawing.Size(36, 30);
            this.label_Measure.Text = "Bázis:";
            // 
            // textBox_MeasureBase
            // 
            this.textBox_MeasureBase.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_MeasureBase.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.textBox_MeasureBase.Margin = new System.Windows.Forms.Padding(1);
            this.textBox_MeasureBase.Name = "textBox_MeasureBase";
            this.textBox_MeasureBase.Size = new System.Drawing.Size(40, 31);
            this.textBox_MeasureBase.Text = "1000";
            // 
            // labelUnit
            // 
            this.labelUnit.Name = "labelUnit";
            this.labelUnit.Size = new System.Drawing.Size(29, 30);
            this.labelUnit.Text = "mm";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer1.IsSplitterFixed = true;
            this.splitContainer1.Location = new System.Drawing.Point(0, 59);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.button_Double);
            this.splitContainer1.Panel1.Controls.Add(this.button_Half);
            this.splitContainer1.Panel1.Controls.Add(this.button_ApplyToAll);
            this.splitContainer1.Panel1.Controls.Add(this.groupBox_SectionLength);
            this.splitContainer1.Panel1.Controls.Add(this.groupBox_PavementWidth);
            this.splitContainer1.Panel1.Controls.Add(this.groupBox_Net);
            this.splitContainer1.Panel1.Controls.Add(this.errorLayerGroup);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.drawingArea);
            this.splitContainer1.Size = new System.Drawing.Size(1020, 580);
            this.splitContainer1.SplitterDistance = 180;
            this.splitContainer1.TabIndex = 5;
            // 
            // button_Double
            // 
            this.button_Double.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.button_Double.Location = new System.Drawing.Point(93, 204);
            this.button_Double.Name = "button_Double";
            this.button_Double.Size = new System.Drawing.Size(83, 23);
            this.button_Double.TabIndex = 8;
            this.button_Double.Tag = "double";
            this.button_Double.Text = "Dupláz";
            this.button_Double.UseVisualStyleBackColor = true;
            this.button_Double.Click += new System.EventHandler(this.button_Double_Click);
            // 
            // button_Half
            // 
            this.button_Half.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.button_Half.Location = new System.Drawing.Point(7, 204);
            this.button_Half.Name = "button_Half";
            this.button_Half.Size = new System.Drawing.Size(80, 23);
            this.button_Half.TabIndex = 7;
            this.button_Half.Tag = "half";
            this.button_Half.Text = "Felez";
            this.button_Half.UseVisualStyleBackColor = true;
            this.button_Half.Click += new System.EventHandler(this.button_Half_Click);
            // 
            // button_ApplyToAll
            // 
            this.button_ApplyToAll.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.button_ApplyToAll.Location = new System.Drawing.Point(7, 175);
            this.button_ApplyToAll.Name = "button_ApplyToAll";
            this.button_ApplyToAll.Size = new System.Drawing.Size(169, 23);
            this.button_ApplyToAll.TabIndex = 6;
            this.button_ApplyToAll.Text = "Alkalmaz az összes szelvényre";
            this.button_ApplyToAll.UseVisualStyleBackColor = true;
            this.button_ApplyToAll.Click += new System.EventHandler(this.button_ApplyToAll_Click);
            // 
            // groupBox_SectionLength
            // 
            this.groupBox_SectionLength.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox_SectionLength.Controls.Add(this.textBox_SectionLength);
            this.groupBox_SectionLength.Location = new System.Drawing.Point(7, 3);
            this.groupBox_SectionLength.Name = "groupBox_SectionLength";
            this.groupBox_SectionLength.Size = new System.Drawing.Size(169, 47);
            this.groupBox_SectionLength.TabIndex = 2;
            this.groupBox_SectionLength.TabStop = false;
            this.groupBox_SectionLength.Text = "Szakasz hossza [m]";
            // 
            // textBox_SectionLength
            // 
            this.textBox_SectionLength.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox_SectionLength.Location = new System.Drawing.Point(9, 16);
            this.textBox_SectionLength.Name = "textBox_SectionLength";
            this.textBox_SectionLength.Size = new System.Drawing.Size(154, 20);
            this.textBox_SectionLength.TabIndex = 0;
            this.textBox_SectionLength.TextChanged += new System.EventHandler(this.textBox_SectionLength_TextChanged);
            // 
            // groupBox_PavementWidth
            // 
            this.groupBox_PavementWidth.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox_PavementWidth.Controls.Add(this.textBox_PavementWidth);
            this.groupBox_PavementWidth.Location = new System.Drawing.Point(7, 49);
            this.groupBox_PavementWidth.Name = "groupBox_PavementWidth";
            this.groupBox_PavementWidth.Size = new System.Drawing.Size(169, 50);
            this.groupBox_PavementWidth.TabIndex = 1;
            this.groupBox_PavementWidth.TabStop = false;
            this.groupBox_PavementWidth.Text = "Burkolatszélesség [m]";
            // 
            // textBox_PavementWidth
            // 
            this.textBox_PavementWidth.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox_PavementWidth.Location = new System.Drawing.Point(6, 19);
            this.textBox_PavementWidth.Name = "textBox_PavementWidth";
            this.textBox_PavementWidth.Size = new System.Drawing.Size(157, 20);
            this.textBox_PavementWidth.TabIndex = 0;
            this.textBox_PavementWidth.TextChanged += new System.EventHandler(this.textBox_PavementWidth_TextChanged);
            // 
            // groupBox_Net
            // 
            this.groupBox_Net.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox_Net.Controls.Add(this.numericUpDown_Row);
            this.groupBox_Net.Controls.Add(this.numericUpDown_Col);
            this.groupBox_Net.Location = new System.Drawing.Point(7, 102);
            this.groupBox_Net.Name = "groupBox_Net";
            this.groupBox_Net.Size = new System.Drawing.Size(169, 68);
            this.groupBox_Net.TabIndex = 3;
            this.groupBox_Net.TabStop = false;
            this.groupBox_Net.Text = "Háló kiosztás";
            // 
            // numericUpDown_Row
            // 
            this.numericUpDown_Row.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.numericUpDown_Row.Location = new System.Drawing.Point(5, 42);
            this.numericUpDown_Row.Name = "numericUpDown_Row";
            this.numericUpDown_Row.Size = new System.Drawing.Size(158, 20);
            this.numericUpDown_Row.TabIndex = 1;
            this.numericUpDown_Row.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.numericUpDown_Row.ValueChanged += new System.EventHandler(this.numericUpDown_Row_ValueChanged);
            // 
            // numericUpDown_Col
            // 
            this.numericUpDown_Col.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.numericUpDown_Col.Location = new System.Drawing.Point(5, 19);
            this.numericUpDown_Col.Name = "numericUpDown_Col";
            this.numericUpDown_Col.Size = new System.Drawing.Size(158, 20);
            this.numericUpDown_Col.TabIndex = 0;
            this.numericUpDown_Col.Value = new decimal(new int[] {
            24,
            0,
            0,
            0});
            this.numericUpDown_Col.ValueChanged += new System.EventHandler(this.numericUpDown_Col_ValueChanged);
            // 
            // errorLayerGroup
            // 
            this.errorLayerGroup.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.errorLayerGroup.AutoScroll = true;
            this.errorLayerGroup.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.errorLayerGroup.Location = new System.Drawing.Point(4, 233);
            this.errorLayerGroup.Name = "errorLayerGroup";
            this.errorLayerGroup.Padding = new System.Windows.Forms.Padding(3);
            this.errorLayerGroup.Size = new System.Drawing.Size(172, 344);
            this.errorLayerGroup.TabIndex = 0;
            // 
            // drawingArea
            // 
            this.drawingArea.Command = 0;
            this.drawingArea.Dock = System.Windows.Forms.DockStyle.Fill;
            this.drawingArea.Location = new System.Drawing.Point(0, 0);
            this.drawingArea.Margin = new System.Windows.Forms.Padding(5);
            this.drawingArea.MeasureAction = null;
            this.drawingArea.Name = "drawingArea";
            this.drawingArea.PavementPhoto = null;
            this.drawingArea.Photo = null;
            this.drawingArea.Size = new System.Drawing.Size(836, 580);
            this.drawingArea.TabIndex = 3;
            // 
            // toolStripButton1
            // 
            this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
            this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton1.Name = "toolStripButton1";
            this.toolStripButton1.Size = new System.Drawing.Size(23, 30);
            this.toolStripButton1.Text = "toolStripButton1";
            this.toolStripButton1.Click += new System.EventHandler(this.toolStripButton1_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1020, 661);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.tabControl_Main);
            this.KeyPreview = true;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Útburkolat állapot értékelés v1.2";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyDown);
            this.projectToolStrip.ResumeLayout(false);
            this.projectToolStrip.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.tabControl_Main.ResumeLayout(false);
            this.tabPage_Project.ResumeLayout(false);
            this.tabPage_Rating.ResumeLayout(false);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.groupBox_SectionLength.ResumeLayout(false);
            this.groupBox_SectionLength.PerformLayout();
            this.groupBox_PavementWidth.ResumeLayout(false);
            this.groupBox_PavementWidth.PerformLayout();
            this.groupBox_Net.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_Row)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_Col)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip projectToolStrip;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel label_Coords;
        private System.Windows.Forms.ToolStripButton button_ImportPhotos;
        private System.Windows.Forms.ToolStripButton button_OpenProject;
        private System.Windows.Forms.ToolStripButton button_SaveProject;
        private System.Windows.Forms.ToolStripButton button_SaveAs;
        private DrawingArea drawingArea;
        private System.Windows.Forms.TabControl tabControl_Main;
        private System.Windows.Forms.TabPage tabPage_Project;
        private System.Windows.Forms.TabPage tabPage_Rating;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton button_Previous;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripComboBox combo_Section;
        private System.Windows.Forms.ToolStripButton button_Next;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton button_FarDistance;
        private System.Windows.Forms.ToolStripButton button_NearDistance;
        private System.Windows.Forms.ToolStripButton button_LeftEdge;
        private System.Windows.Forms.ToolStripButton button_RightEdge;
        private System.Windows.Forms.ToolStripButton button_Normalize;
        private System.Windows.Forms.ToolStripButton button_ToggleView;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private ErrorLayerGroup errorLayerGroup;
        private System.Windows.Forms.ToolStripButton button_FirstNonNormalized;
        private System.Windows.Forms.GroupBox groupBox_Net;
        private System.Windows.Forms.NumericUpDown numericUpDown_Row;
        private System.Windows.Forms.NumericUpDown numericUpDown_Col;
        private System.Windows.Forms.GroupBox groupBox_SectionLength;
        private System.Windows.Forms.TextBox textBox_SectionLength;
        private System.Windows.Forms.GroupBox groupBox_PavementWidth;
        private System.Windows.Forms.TextBox textBox_PavementWidth;
        private System.Windows.Forms.Button button_ApplyToAll;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripButton button_ErrorExport;
        private System.Windows.Forms.ToolStripButton button_ImportFrodo;
        private System.Windows.Forms.Button button_Double;
        private System.Windows.Forms.Button button_Half;
        private System.Windows.Forms.ToolStripButton button_DrawErrors;
        private System.Windows.Forms.ToolStripButton button_Measure;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripTextBox textBox_MeasureBase;
        private System.Windows.Forms.ToolStripLabel label_Measure;
        private System.Windows.Forms.ToolStripLabel labelUnit;
        private System.Windows.Forms.ToolStripButton button_PhotoRename;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.ToolStripButton toolStripButton1;
    }
}


