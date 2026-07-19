using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PPR
{
    public partial class ErrorLayerGroup : UserControl
    {
        List<ErrorLayerControl> errorLayers = new List<ErrorLayerControl>();

        public List<ErrorLayerControl> ErrorLayers => errorLayers;

        public ErrorLayerControl ActiveLayer
        {
            get
            {
                foreach (ErrorLayerControl layer in errorLayers)
                    if (layer.IsActive) return layer;
                return null;
            }
        }

        public void SetActiveLayer(ErrorLayerControl activeLayer)
        {
            foreach (ErrorLayerControl layer in errorLayers)
                layer.IsActive = false;
            activeLayer.IsActive = true;
        }

        public ErrorLayerGroup()
        {
            InitializeComponent();
            AutoScroll = true;
        }

        public void AddLayer(ErrorLayerControl newLayer)
        {
            if (newLayer == null) return;
            newLayer.Parent = this;
            newLayer.Dock = DockStyle.Top;
            errorLayers.Add(newLayer);
            newLayer.SendToBack();
        }

        public void RemoveLayer(ErrorLayerControl layer)
        {
            if (layer == null) return;
            errorLayers.Remove(layer);
            Controls.Remove(layer);
            layer.Dispose();
        }
    }
}
