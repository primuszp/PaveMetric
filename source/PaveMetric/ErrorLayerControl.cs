using System;
using System.Drawing;
using System.Windows.Forms;

namespace PaveMetric
{
    public partial class ErrorLayerControl : UserControl
    {
        bool isActive = false;
        ErrorLayerGroup parentGroup = null;
        string layerName = "Anonymous";
        Color layerColor = Color.Gray;
        ErrorCodes errorCode;

        public string LayerName
        {
            get => layerName;
            set
            {
                layerName = value;
                if (label_LayerName != null)
                    label_LayerName.Text = value;
            }
        }

        public bool IsVisible
        {
            get => checkBox_Visible.Checked;
            set => checkBox_Visible.Checked = value;
        }

        public Color LayerColor
        {
            get => layerColor;
            set
            {
                layerColor = value;
                if (button_Color != null)
                    button_Color.BackColor = value;
            }
        }

        public ErrorCodes ErrorCode
        {
            get => errorCode;
            set => errorCode = value;
        }

        public bool IsActive
        {
            get => isActive;
            set
            {
                isActive = value;
                Color bg = isActive ? Theme.AccentSoft : Theme.Surface;
                BackColor = bg;
                if (label_LayerName != null)
                {
                    label_LayerName.BackColor = bg;
                    label_LayerName.ForeColor = isActive ? Theme.Accent : Theme.Text;
                }
                if (button_Select != null)
                    button_Select.FlatAppearance.BorderColor = isActive ? Theme.Accent : Theme.Border;
            }
        }

        public event EventHandler OnActionButtonClick;
        public event EventHandler OnDeleteButtonClick;
        public event EventHandler OnVisibleStateChanged;

        public ErrorLayerControl(ErrorLayerGroup ParentGroup)
        {
            parentGroup = ParentGroup;
            InitializeComponent();
            Theme.Apply(this);
            button_Color.FlatAppearance.BorderColor = Theme.Border;
        }

        private void button_Color_Click(object sender, EventArgs e)
        {
            using var dlg = new ColorDialog { Color = layerColor, FullOpen = true };
            if (dlg.ShowDialog() == DialogResult.OK)
                LayerColor = dlg.Color;
        }

        private void button_Select_Click(object sender, EventArgs e)
        {
            OnActionButtonClick?.Invoke(this, EventArgs.Empty);
        }

        private void button_Delete_Click(object sender, EventArgs e)
        {
            OnDeleteButtonClick?.Invoke(this, EventArgs.Empty);
        }

        private void checkBox_Visible_CheckedChanged(object sender, EventArgs e)
        {
            OnVisibleStateChanged?.Invoke(this, e);
        }
    }
}
