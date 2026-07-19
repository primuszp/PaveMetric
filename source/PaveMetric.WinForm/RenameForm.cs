using System;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace PaveMetric
{
    public partial class RenameForm : Form
    {
        private string _sourcePath;
        private string _targetPath;
        private string _formatCode;
        private double _startValue;
        private double _deltaValue;

        public RenameForm()
        {
            InitializeComponent();
            Theme.Apply(this);
        }

        private void buttonSourcePath_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                var result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    _sourcePath = fbd.SelectedPath;
                    labelSourcePath.Text = _sourcePath;
                }
            }
        }

        private void buttonTargetPath_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                var result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    _targetPath = fbd.SelectedPath;
                    labelTargetPath.Text = _targetPath;
                }
            }
        }

        private void RenamePhotos()
        {
            if (string.IsNullOrEmpty(_sourcePath) || string.IsNullOrEmpty(_targetPath)) return;

            _startValue = double.Parse(textBoxStart.Text);
            _deltaValue = double.Parse(textBoxDelta.Text);
            _formatCode = textBoxFormat.Text;

            var files = Directory.GetFiles(_sourcePath);

            for (var i = 0; i < files.Length; i++)
            {
                var section = _startValue + i * _deltaValue;
                var extension = Path.GetExtension(files[i]);
                var newFile = section.ToString(_formatCode, CultureInfo.InvariantCulture);
                var newPath = Path.Combine(_targetPath, newFile + extension);

                File.Copy(files[i], newPath, true);
            }
        }

        private void textBoxStart_TextChanged(object sender, EventArgs e)
        {
            if (!double.TryParse(textBoxStart.Text, out _startValue))
            {
                MessageBox.Show("Bemeneti karakterlánc formátum nem helyes!", "Figyelmeztetés",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void textBoxDelta_TextChanged(object sender, EventArgs e)
        {
            if (!double.TryParse(textBoxDelta.Text, out _deltaValue))
            {
                MessageBox.Show("Bemeneti karakterlánc formátum nem helyes!", "Figyelmeztetés",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void buttonRename_Click(object sender, EventArgs e)
        {
            RenamePhotos();
            MessageBox.Show("A képek átnevezése befejeződött!", "Folyamatállapot",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}