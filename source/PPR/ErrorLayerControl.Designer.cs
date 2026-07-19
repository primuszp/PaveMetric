namespace PPR
{
    partial class ErrorLayerControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.button_Color = new System.Windows.Forms.Button();
            this.label_LayerName = new System.Windows.Forms.Label();
            this.checkBox_Visible = new System.Windows.Forms.CheckBox();
            this.button_Select = new System.Windows.Forms.Button();
            this.button_Delete = new System.Windows.Forms.Button();
            this.SuspendLayout();

            // Row 1 — color swatch (20×20)
            this.button_Color.Name = "button_Color";
            this.button_Color.Location = new System.Drawing.Point(2, 3);
            this.button_Color.Size = new System.Drawing.Size(20, 20);
            this.button_Color.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_Color.TabStop = false;
            this.button_Color.Click += new System.EventHandler(this.button_Color_Click);

            // Row 1 — type name label (fills between color swatch and visible checkbox)
            this.label_LayerName.Name = "label_LayerName";
            this.label_LayerName.Location = new System.Drawing.Point(24, 4);
            this.label_LayerName.Size = new System.Drawing.Size(122, 18);
            this.label_LayerName.Anchor = System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Right;
            this.label_LayerName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // Row 1 — visibility checkbox (right-anchored)
            this.checkBox_Visible.Name = "checkBox_Visible";
            this.checkBox_Visible.Location = new System.Drawing.Point(148, 4);
            this.checkBox_Visible.Size = new System.Drawing.Size(20, 20);
            this.checkBox_Visible.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Right;
            this.checkBox_Visible.TabStop = false;
            this.checkBox_Visible.CheckedChanged += new System.EventHandler(this.checkBox_Visible_CheckedChanged);

            // Row 2 — "Kijelöl" button (left half)
            this.button_Select.Name = "button_Select";
            this.button_Select.Location = new System.Drawing.Point(2, 27);
            this.button_Select.Size = new System.Drawing.Size(82, 22);
            this.button_Select.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_Select.Text = "Kijelöl";
            this.button_Select.TabStop = false;
            this.button_Select.Click += new System.EventHandler(this.button_Select_Click);

            // Row 2 — "Töröl" button (right half, right-anchored)
            this.button_Delete.Name = "button_Delete";
            this.button_Delete.Location = new System.Drawing.Point(86, 27);
            this.button_Delete.Size = new System.Drawing.Size(84, 22);
            this.button_Delete.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Right;
            this.button_Delete.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_Delete.Text = "Töröl";
            this.button_Delete.TabStop = false;
            this.button_Delete.Click += new System.EventHandler(this.button_Delete_Click);

            this.Controls.Add(this.button_Color);
            this.Controls.Add(this.label_LayerName);
            this.Controls.Add(this.checkBox_Visible);
            this.Controls.Add(this.button_Select);
            this.Controls.Add(this.button_Delete);

            this.Name = "ErrorLayerControl";
            this.Size = new System.Drawing.Size(172, 52);

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Button button_Color;
        private System.Windows.Forms.Label label_LayerName;
        private System.Windows.Forms.CheckBox checkBox_Visible;
        private System.Windows.Forms.Button button_Select;
        private System.Windows.Forms.Button button_Delete;
    }
}
