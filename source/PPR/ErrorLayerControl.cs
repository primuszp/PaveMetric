using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PPR
{
    public partial class ErrorLayerControl : UserControl
    {
        bool isActive = false;

        ErrorLayerGroup parentGroup = null;
        string layerName = "Anonymous";

        public string LayerName
        {
            get
            {
                return layerName;
            }
            set
            {
                layerName = value;
                textBox_LayerName.Text = layerName;
            }
        }

        public bool IsVisible
        {
            get
            {
                return checkBox_Visible.Checked;
            }
            set
            {
                checkBox_Visible.Checked = value;
            }
        }

        Color layerColor = Color.Gray;
        public Color LayerColor
        {
            get
            {
                return layerColor;
            }
            set
            {
                layerColor = value;
                button_Color.BackColor = value;
            }
        }

        public ErrorCodes ErrorCode { get; set; }

        public bool IsActive
        {
            get { return isActive; }
            set
            {
                isActive = value;
                if (isActive)
                {
                    BorderStyle = BorderStyle.Fixed3D;
                }
                else
                {
                    BorderStyle = BorderStyle.None;
                }
            }
        }

        public event EventHandler OnActionButtonClick;
        public event EventHandler OnDeleteButtonClick;
        public event EventHandler OnVisibleStateChanged;

        public ErrorLayerControl(ErrorLayerGroup ParentGroup)
        {
            parentGroup = ParentGroup;
            InitializeComponent();
        }

        private void button_Action_Click(object sender, EventArgs e)
        {
            if (OnActionButtonClick != null)
            {
                OnActionButtonClick(this, e);
            }
        }

        private void checkBox_Visible_CheckedChanged(object sender, EventArgs e)
        {
            if (OnVisibleStateChanged != null)
            {
                OnVisibleStateChanged(this, e);
            }
        }

        private void button_Delete_Click(object sender, EventArgs e)
        {
            if (OnDeleteButtonClick != null)
            {
                OnDeleteButtonClick(this, e);
            }
        }
    }
}
