namespace PPR
{
    partial class ErrorLayerControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.checkBox_Visible = new System.Windows.Forms.CheckBox();
            this.button_Color = new System.Windows.Forms.Button();
            this.button_Action = new System.Windows.Forms.Button();
            this.button_Delete = new System.Windows.Forms.Button();
            this.textBox_LayerName = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // checkBox_Visible
            // 
            this.checkBox_Visible.AutoSize = true;
            this.checkBox_Visible.Dock = System.Windows.Forms.DockStyle.Top;
            this.checkBox_Visible.Location = new System.Drawing.Point(3, 37);
            this.checkBox_Visible.Name = "checkBox_Visible";
            this.checkBox_Visible.Size = new System.Drawing.Size(114, 17);
            this.checkBox_Visible.TabIndex = 0;
            this.checkBox_Visible.Text = "Látható";
            this.checkBox_Visible.UseVisualStyleBackColor = true;
            this.checkBox_Visible.CheckedChanged += new System.EventHandler(this.checkBox_Visible_CheckedChanged);
            // 
            // button_Color
            // 
            this.button_Color.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.button_Color.AutoSize = true;
            this.button_Color.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_Color.Location = new System.Drawing.Point(6, 60);
            this.button_Color.Name = "button_Color";
            this.button_Color.Size = new System.Drawing.Size(108, 25);
            this.button_Color.TabIndex = 1;
            this.button_Color.Text = "Szín";
            this.button_Color.UseVisualStyleBackColor = true;
            // 
            // button_Action
            // 
            this.button_Action.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.button_Action.AutoSize = true;
            this.button_Action.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_Action.Location = new System.Drawing.Point(6, 91);
            this.button_Action.Name = "button_Action";
            this.button_Action.Size = new System.Drawing.Size(108, 25);
            this.button_Action.TabIndex = 2;
            this.button_Action.Text = "Kijelöl";
            this.button_Action.UseVisualStyleBackColor = true;
            this.button_Action.Click += new System.EventHandler(this.button_Action_Click);
            // 
            // button_Delete
            // 
            this.button_Delete.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.button_Delete.AutoSize = true;
            this.button_Delete.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_Delete.Location = new System.Drawing.Point(6, 122);
            this.button_Delete.Name = "button_Delete";
            this.button_Delete.Size = new System.Drawing.Size(108, 25);
            this.button_Delete.TabIndex = 3;
            this.button_Delete.Text = "Töröl";
            this.button_Delete.UseVisualStyleBackColor = true;
            this.button_Delete.Click += new System.EventHandler(this.button_Delete_Click);
            // 
            // textBox_LayerName
            // 
            this.textBox_LayerName.Dock = System.Windows.Forms.DockStyle.Top;
            this.textBox_LayerName.Enabled = false;
            this.textBox_LayerName.Location = new System.Drawing.Point(3, 3);
            this.textBox_LayerName.Multiline = true;
            this.textBox_LayerName.Name = "textBox_LayerName";
            this.textBox_LayerName.Size = new System.Drawing.Size(114, 34);
            this.textBox_LayerName.TabIndex = 4;
            // 
            // ErrorLayerControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.button_Delete);
            this.Controls.Add(this.button_Action);
            this.Controls.Add(this.button_Color);
            this.Controls.Add(this.checkBox_Visible);
            this.Controls.Add(this.textBox_LayerName);
            this.Name = "ErrorLayerControl";
            this.Padding = new System.Windows.Forms.Padding(3);
            this.Size = new System.Drawing.Size(120, 159);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox checkBox_Visible;
        private System.Windows.Forms.Button button_Color;
        private System.Windows.Forms.Button button_Action;
        private System.Windows.Forms.Button button_Delete;
        private System.Windows.Forms.TextBox textBox_LayerName;
    }
}
