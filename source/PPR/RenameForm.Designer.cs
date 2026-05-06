
namespace PPR
{
    partial class RenameForm
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
            this.labelSourcePath = new System.Windows.Forms.Label();
            this.buttonSourcePath = new System.Windows.Forms.Button();
            this.buttonTargetPath = new System.Windows.Forms.Button();
            this.labelTargetPath = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.textBoxDelta = new System.Windows.Forms.TextBox();
            this.labelDelta = new System.Windows.Forms.Label();
            this.textBoxStart = new System.Windows.Forms.TextBox();
            this.labelStart = new System.Windows.Forms.Label();
            this.labelFormat = new System.Windows.Forms.Label();
            this.textBoxFormat = new System.Windows.Forms.TextBox();
            this.buttonRename = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // labelSourcePath
            // 
            this.labelSourcePath.AutoSize = true;
            this.labelSourcePath.Location = new System.Drawing.Point(107, 24);
            this.labelSourcePath.Name = "labelSourcePath";
            this.labelSourcePath.Size = new System.Drawing.Size(74, 13);
            this.labelSourcePath.TabIndex = 0;
            this.labelSourcePath.Text = "Forrás útvonal";
            // 
            // buttonSourcePath
            // 
            this.buttonSourcePath.Location = new System.Drawing.Point(6, 19);
            this.buttonSourcePath.Name = "buttonSourcePath";
            this.buttonSourcePath.Size = new System.Drawing.Size(95, 23);
            this.buttonSourcePath.TabIndex = 1;
            this.buttonSourcePath.Text = "Forrás mappa";
            this.buttonSourcePath.UseVisualStyleBackColor = true;
            this.buttonSourcePath.Click += new System.EventHandler(this.buttonSourcePath_Click);
            // 
            // buttonTargetPath
            // 
            this.buttonTargetPath.Location = new System.Drawing.Point(6, 48);
            this.buttonTargetPath.Name = "buttonTargetPath";
            this.buttonTargetPath.Size = new System.Drawing.Size(95, 23);
            this.buttonTargetPath.TabIndex = 2;
            this.buttonTargetPath.Text = "Cél mappa";
            this.buttonTargetPath.UseVisualStyleBackColor = true;
            this.buttonTargetPath.Click += new System.EventHandler(this.buttonTargetPath_Click);
            // 
            // labelTargetPath
            // 
            this.labelTargetPath.AutoSize = true;
            this.labelTargetPath.Location = new System.Drawing.Point(107, 53);
            this.labelTargetPath.Name = "labelTargetPath";
            this.labelTargetPath.Size = new System.Drawing.Size(60, 13);
            this.labelTargetPath.TabIndex = 3;
            this.labelTargetPath.Text = "Cél útvonal";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.buttonSourcePath);
            this.groupBox1.Controls.Add(this.labelTargetPath);
            this.groupBox1.Controls.Add(this.buttonTargetPath);
            this.groupBox1.Controls.Add(this.labelSourcePath);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(427, 78);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Elérési út";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.textBoxFormat);
            this.groupBox2.Controls.Add(this.labelFormat);
            this.groupBox2.Controls.Add(this.textBoxDelta);
            this.groupBox2.Controls.Add(this.labelDelta);
            this.groupBox2.Controls.Add(this.textBoxStart);
            this.groupBox2.Controls.Add(this.labelStart);
            this.groupBox2.Location = new System.Drawing.Point(12, 97);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(427, 76);
            this.groupBox2.TabIndex = 5;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Szelvényezési adatok";
            // 
            // textBoxDelta
            // 
            this.textBoxDelta.Location = new System.Drawing.Point(110, 46);
            this.textBoxDelta.Name = "textBoxDelta";
            this.textBoxDelta.Size = new System.Drawing.Size(80, 20);
            this.textBoxDelta.TabIndex = 3;
            this.textBoxDelta.Text = "10";
            this.textBoxDelta.TextChanged += new System.EventHandler(this.textBoxDelta_TextChanged);
            // 
            // labelDelta
            // 
            this.labelDelta.AutoSize = true;
            this.labelDelta.Location = new System.Drawing.Point(33, 49);
            this.labelDelta.Name = "labelDelta";
            this.labelDelta.Size = new System.Drawing.Size(71, 13);
            this.labelDelta.TabIndex = 2;
            this.labelDelta.Text = "Távolság [m]:";
            // 
            // textBoxStart
            // 
            this.textBoxStart.Location = new System.Drawing.Point(110, 20);
            this.textBoxStart.Name = "textBoxStart";
            this.textBoxStart.Size = new System.Drawing.Size(80, 20);
            this.textBoxStart.TabIndex = 1;
            this.textBoxStart.Text = "0";
            this.textBoxStart.TextChanged += new System.EventHandler(this.textBoxStart_TextChanged);
            // 
            // labelStart
            // 
            this.labelStart.AutoSize = true;
            this.labelStart.Location = new System.Drawing.Point(6, 23);
            this.labelStart.Name = "labelStart";
            this.labelStart.Size = new System.Drawing.Size(98, 13);
            this.labelStart.TabIndex = 0;
            this.labelStart.Text = "Kezdőszelvény [m]:";
            // 
            // labelFormat
            // 
            this.labelFormat.AutoSize = true;
            this.labelFormat.Location = new System.Drawing.Point(196, 23);
            this.labelFormat.Name = "labelFormat";
            this.labelFormat.Size = new System.Drawing.Size(56, 13);
            this.labelFormat.TabIndex = 4;
            this.labelFormat.Text = "Formátum:";
            // 
            // textBoxFormat
            // 
            this.textBoxFormat.Location = new System.Drawing.Point(258, 20);
            this.textBoxFormat.Name = "textBoxFormat";
            this.textBoxFormat.Size = new System.Drawing.Size(80, 20);
            this.textBoxFormat.TabIndex = 5;
            this.textBoxFormat.Text = "0+00";
            // 
            // buttonRename
            // 
            this.buttonRename.Location = new System.Drawing.Point(12, 179);
            this.buttonRename.Name = "buttonRename";
            this.buttonRename.Size = new System.Drawing.Size(427, 22);
            this.buttonRename.TabIndex = 6;
            this.buttonRename.Text = "Képek átnevezése";
            this.buttonRename.UseVisualStyleBackColor = true;
            this.buttonRename.Click += new System.EventHandler(this.buttonRename_Click);
            // 
            // RenameForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(451, 213);
            this.Controls.Add(this.buttonRename);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "RenameForm";
            this.Text = "Fotók átnevezése";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label labelSourcePath;
        private System.Windows.Forms.Button buttonSourcePath;
        private System.Windows.Forms.Button buttonTargetPath;
        private System.Windows.Forms.Label labelTargetPath;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox textBoxDelta;
        private System.Windows.Forms.Label labelDelta;
        private System.Windows.Forms.TextBox textBoxStart;
        private System.Windows.Forms.Label labelStart;
        private System.Windows.Forms.TextBox textBoxFormat;
        private System.Windows.Forms.Label labelFormat;
        private System.Windows.Forms.Button buttonRename;
    }
}