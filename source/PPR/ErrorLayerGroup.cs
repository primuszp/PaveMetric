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
    public partial class ErrorLayerGroup : UserControl
    {
        List<ErrorLayerControl> errorLayers = new List<ErrorLayerControl>();

        public List<ErrorLayerControl> ErrorLayers
        {
            get { return errorLayers; }
        }

        public ErrorLayerControl ActiveLayer
        {
            get
            {
                foreach (ErrorLayerControl myLayer in errorLayers)
                {
                    if (myLayer.IsActive)
                    {
                        return myLayer;
                    }
                }

                return null;
            }
        }

        public void SetActiveLayer(ErrorLayerControl ActiveLayerControl)
        {
            foreach (ErrorLayerControl myLayer in errorLayers)
            {
                myLayer.IsActive = false;
            }

            ActiveLayerControl.IsActive = true;
        }

        public ErrorLayerGroup()
        {
            InitializeComponent();
            VerticalScroll.Visible = true;
        }

        public void AddLayer(ErrorLayerControl NewLayer)
        {
            if (NewLayer != null)
            {
                NewLayer.Parent = this;
                NewLayer.Dock = DockStyle.Top;
                errorLayers.Add(NewLayer);
                //this.AutoScroll = true;
                //this.HorizontalScroll.Enabled = false;
            }
        }
    }
}
