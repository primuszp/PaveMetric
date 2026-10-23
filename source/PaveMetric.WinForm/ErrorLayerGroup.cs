using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PaveMetric
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
            // docked controls stack in reverse z-order: keep list order top-to-bottom
            newLayer.BringToFront();
        }

        Label emptyHint;

        /// <summary>Shows <paramref name="text"/> in place of the list, or hides it when null.</summary>
        public void SetEmptyHint(string text)
        {
            if (text == null)
            {
                if (emptyHint != null) emptyHint.Visible = false;
                return;
            }
            emptyHint ??= new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Theme.TextMuted,
                Padding = new Padding(8),
                Parent = this,
            };
            emptyHint.Text = text;
            emptyHint.Visible = true;
            // docked controls are laid out back to front: the fill must come last
            emptyHint.BringToFront();
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
