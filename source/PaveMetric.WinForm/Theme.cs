using System;
using System.Drawing;
using System.Windows.Forms;

namespace PaveMetric
{
    /// <summary>
    /// Central visual theme: a restrained light palette with a steel-blue accent.
    /// Apply(form) walks the control tree once, so Designer files stay untouched
    /// and every form gets the same look.
    /// </summary>
    public static class Theme
    {
        // Palette
        public static readonly Color Background = Color.FromArgb(244, 245, 247);   // window base
        public static readonly Color Surface = Color.White;                        // panels, inputs
        public static readonly Color SurfaceHover = Color.FromArgb(232, 238, 248);
        public static readonly Color SurfacePressed = Color.FromArgb(215, 227, 244);
        public static readonly Color Border = Color.FromArgb(213, 217, 224);
        public static readonly Color Text = Color.FromArgb(31, 41, 55);
        public static readonly Color TextMuted = Color.FromArgb(107, 114, 128);
        public static readonly Color Accent = Color.FromArgb(43, 87, 154);         // steel blue
        public static readonly Color AccentSoft = Color.FromArgb(226, 235, 247);
        public static readonly Color Warning = Color.FromArgb(255, 244, 206);      // "normalize needed"
        public static readonly Color WarningBorder = Color.FromArgb(217, 164, 6);
        public static readonly Color Canvas = Color.FromArgb(24, 26, 28);          // drawing background

        public static readonly Font BaseFont = new Font("Segoe UI", 9F);

        public static void Apply(Form form)
        {
            form.Font = BaseFont;
            form.BackColor = Background;
            form.ForeColor = Text;
            ApplyToChildren(form);
        }

        /// <summary>Themes a user control created after the form-level Apply ran.</summary>
        public static void Apply(UserControl control)
        {
            control.Font = BaseFont;
            control.BackColor = Surface;
            control.ForeColor = Text;
            ApplyToChildren(control);
        }

        private static void ApplyToChildren(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                switch (control)
                {
                    case ToolStrip strip: // includes StatusStrip
                        strip.Renderer = new ThemeToolStripRenderer();
                        strip.BackColor = Surface;
                        strip.ForeColor = Text;
                        foreach (ToolStripItem item in strip.Items)
                            item.ForeColor = Text;
                        break;
                    case Button button:
                        StyleButton(button);
                        break;
                    case TextBox textBox:
                        textBox.BorderStyle = BorderStyle.FixedSingle;
                        textBox.BackColor = Surface;
                        textBox.ForeColor = Text;
                        break;
                    case NumericUpDown numeric:
                        numeric.BorderStyle = BorderStyle.FixedSingle;
                        numeric.BackColor = Surface;
                        numeric.ForeColor = Text;
                        break;
                    case GroupBox groupBox:
                        groupBox.ForeColor = TextMuted;
                        break;
                    case SplitContainer split:
                        split.BackColor = Background;
                        split.Panel1.BackColor = Background;
                        split.Panel2.BackColor = Background;
                        break;
                    case TabControl _:
                    case TabPage _:
                        control.BackColor = Background;
                        break;
                }

                if (control.HasChildren)
                    ApplyToChildren(control);
            }
        }

        private static void StyleButton(Button button)
        {
            // Layer color swatches keep their own background; everything else gets the
            // flat themed look.
            bool isColorSwatch = button.Name == "button_Color";

            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Border;
            button.ForeColor = Text;
            if (!isColorSwatch)
            {
                button.BackColor = Surface;
                button.FlatAppearance.MouseOverBackColor = SurfaceHover;
                button.FlatAppearance.MouseDownBackColor = SurfacePressed;
            }
        }

        /// <summary>
        /// Flat toolbar rendering: quiet surface, soft hover, accent underline for checked
        /// toggle buttons, and support for per-item warning backgrounds (Normalize dirty state).
        /// </summary>
        private sealed class ThemeToolStripRenderer : ToolStripProfessionalRenderer
        {
            public ThemeToolStripRenderer() : base(new ThemeColorTable())
            {
                RoundedEdges = false;
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                // Single hairline at the bottom instead of the 3D border.
                using Pen pen = new Pen(Border);
                e.Graphics.DrawLine(pen, 0, e.ToolStrip.Height - 1, e.ToolStrip.Width, e.ToolStrip.Height - 1);
            }

            protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
            {
                Rectangle bounds = new Rectangle(Point.Empty, e.Item.Size);
                bounds.Inflate(-1, -2);

                bool warning = e.Item.BackColor.ToArgb() == Warning.ToArgb()
                    || e.Item.BackColor.ToArgb() == Color.Gold.ToArgb();
                bool isChecked = e.Item is ToolStripButton toolButton && toolButton.Checked;

                if (warning)
                    FillRounded(e.Graphics, bounds, Warning, WarningBorder);
                else if (e.Item.Pressed)
                    FillRounded(e.Graphics, bounds, SurfacePressed, Accent);
                else if (isChecked)
                    FillRounded(e.Graphics, bounds, AccentSoft, Accent);
                else if (e.Item.Selected)
                    FillRounded(e.Graphics, bounds, SurfaceHover, Border);
            }

            private static void FillRounded(Graphics graphics, Rectangle bounds, Color fill, Color border)
            {
                if (bounds.Width < 4 || bounds.Height < 4)
                    return;

                int radius = Math.Min(4, Math.Min(bounds.Width, bounds.Height) / 2 - 1);
                if (radius < 1)
                    return;

                using var path = RoundedRect(bounds, radius);
                var oldMode = graphics.SmoothingMode;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(fill))
                    graphics.FillPath(brush, path);
                using (var pen = new Pen(border))
                    graphics.DrawPath(pen, path);
                graphics.SmoothingMode = oldMode;
            }

            private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle bounds, int radius)
            {
                radius = Math.Max(1, radius);
                var path = new System.Drawing.Drawing2D.GraphicsPath();
                int d = radius * 2;
                path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
                path.AddArc(bounds.Right - d - 1, bounds.Y, d, d, 270, 90);
                path.AddArc(bounds.Right - d - 1, bounds.Bottom - d - 1, d, d, 0, 90);
                path.AddArc(bounds.X, bounds.Bottom - d - 1, d, d, 90, 90);
                path.CloseFigure();
                return path;
            }
        }

        private sealed class ThemeColorTable : ProfessionalColorTable
        {
            public override Color ToolStripGradientBegin => Surface;
            public override Color ToolStripGradientMiddle => Surface;
            public override Color ToolStripGradientEnd => Surface;
            public override Color ToolStripBorder => Border;
            public override Color SeparatorDark => Border;
            public override Color SeparatorLight => Surface;
            public override Color StatusStripGradientBegin => Surface;
            public override Color StatusStripGradientEnd => Surface;
            public override Color GripDark => Border;
            public override Color GripLight => Surface;
            public override Color ImageMarginGradientBegin => Surface;
            public override Color ImageMarginGradientMiddle => Surface;
            public override Color ImageMarginGradientEnd => Surface;
            public override Color MenuBorder => Border;
            public override Color MenuItemBorder => Accent;
            public override Color MenuItemSelected => SurfaceHover;
        }
    }
}
