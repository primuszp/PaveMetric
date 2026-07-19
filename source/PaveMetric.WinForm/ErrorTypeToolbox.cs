using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PaveMetric
{
    internal class ErrorTypeToolbox : Form
    {
        private readonly ErrorLayerGroup _group;
        private readonly FlowLayoutPanel _flow;
        private readonly ComboBox _comboAdd;
        private readonly TextBox _addNameBox;
        private readonly Button _btnAdd;

        public event Action<ErrorLayerControl> TypeAdded;
        public event Action<ErrorLayerControl> TypeRemoved;
        public event EventHandler UserClosed;

        private static readonly Color[] _defaultColors =
        {
            Color.FromArgb(220,  50,  50),   // red
            Color.FromArgb( 37, 99, 235),   // blue
            Color.FromArgb( 22, 163,  74),   // green
            Color.FromArgb(234, 111,  21),   // orange
            Color.FromArgb(124,  58, 237),   // purple
            Color.FromArgb(180, 105,  25),   // brown
            Color.FromArgb( 13, 148, 136),   // teal
            Color.FromArgb(219,  39, 119),   // pink
        };

        public ErrorTypeToolbox(ErrorLayerGroup group)
        {
            _group = group;
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            Text = "Hibatípusok szerkesztése";
            ShowInTaskbar = false;
            MinimumSize = new Size(260, 120);
            Size = new Size(300, 340);
            StartPosition = FormStartPosition.Manual;
            BackColor = Theme.Surface;
            Font = Theme.BaseFont;

            // ── add-new row at bottom ─────────────────────────────────────
            var addPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                BackColor = Theme.Background,
            };
            addPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.Border);
                e.Graphics.DrawLine(pen, 0, 0, addPanel.Width, 0);
            };

            _comboAdd = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(4, 10),
                Size = new Size(90, 24),
                Font = Theme.BaseFont,
                FlatStyle = FlatStyle.Flat,
            };
            _comboAdd.SelectedIndexChanged += (s, e) =>
            {
                if (_comboAdd.SelectedItem is CodeItem item)
                    _addNameBox.Text = item.Name;
            };

            _addNameBox = new TextBox
            {
                Location = new Point(98, 11),
                Size = new Size(90, 22),
                Font = Theme.BaseFont,
                BackColor = Theme.Surface,
                ForeColor = Theme.Text,
            };

            _btnAdd = new Button
            {
                Text = "Hozzáad",
                Location = new Point(196, 9),
                Size = new Size(72, 26),
                FlatStyle = FlatStyle.Flat,
                Font = Theme.BaseFont,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                TabStop = false,
                BackColor = Theme.AccentSoft,
                ForeColor = Theme.Accent,
            };
            _btnAdd.FlatAppearance.BorderColor = Theme.Accent;
            _btnAdd.Click += BtnAdd_Click;

            addPanel.Controls.Add(_comboAdd);
            addPanel.Controls.Add(_addNameBox);
            addPanel.Controls.Add(_btnAdd);
            addPanel.Resize += (s, e) =>
            {
                int btnX = addPanel.Width - _btnAdd.Width - 4;
                _btnAdd.Left = btnX;
                _addNameBox.Width = Math.Max(40, btnX - _addNameBox.Left - 4);
            };

            // ── type list ────────────────────────────────────────────────
            _flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                AutoScroll = true,
                WrapContents = false,
                Padding = new Padding(0),
            };

            Controls.Add(_flow);
            Controls.Add(addPanel);

            Populate();
        }

        public void Populate()
        {
            _flow.Controls.Clear();
            foreach (ErrorLayerControl layer in _group.ErrorLayers)
                _flow.Controls.Add(new TypeConfigRow(layer, OnDeleteRow));

            _flow.SizeChanged -= OnFlowSizeChanged;
            _flow.SizeChanged += OnFlowSizeChanged;
            ResizeRows();
            RefreshCombo();
        }

        private void OnFlowSizeChanged(object s, EventArgs e) => ResizeRows();

        private void ResizeRows()
        {
            int w = _flow.ClientSize.Width;
            foreach (Control c in _flow.Controls)
                c.Width = w;
        }

        private void RefreshCombo()
        {
            _comboAdd.Items.Clear();
            var used = new HashSet<ErrorCodes>(_group.ErrorLayers.Select(l => l.ErrorCode));
            foreach (ErrorCodes code in Enum.GetValues(typeof(ErrorCodes)))
                if (!used.Contains(code))
                    _comboAdd.Items.Add(new CodeItem(code, DefaultName(code)));

            bool any = _comboAdd.Items.Count > 0;
            _btnAdd.Enabled = any;
            if (any)
            {
                _comboAdd.SelectedIndex = 0;
                _addNameBox.Text = ((CodeItem)_comboAdd.Items[0]).Name;
            }
            else
            {
                _addNameBox.Text = "";
            }
            _addNameBox.Enabled = any;
        }

        private void BtnAdd_Click(object s, EventArgs e)
        {
            if (_comboAdd.SelectedItem is not CodeItem item) return;
            string name = _addNameBox.Text.Trim();
            if (name.Length == 0) name = item.Name;
            var newLayer = new ErrorLayerControl(_group)
            {
                ErrorCode = item.Code,
                LayerName = name,
                LayerColor = DefaultColor(item.Code),
                IsVisible = true,
            };
            _group.AddLayer(newLayer);
            TypeAdded?.Invoke(newLayer);
            Populate();
        }

        private void OnDeleteRow(ErrorLayerControl layer)
        {
            TypeRemoved?.Invoke(layer);
            _group.RemoveLayer(layer);
            Populate();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                UserClosed?.Invoke(this, e);
            }
            base.OnFormClosing(e);
        }

        private static string DefaultName(ErrorCodes code) => code switch
        {
            ErrorCodes.MapCrack => "Hálós repedés",
            ErrorCodes.AlligatorCrack => "Hálós repedés deformációval",
            ErrorCodes.LongitudinalCrack => "Hosszirányú repedés",
            ErrorCodes.CrossCrack => "Keresztirányú repedés",
            ErrorCodes.Pothole => "Kátyú",
            ErrorCodes.FilledPothole => "Kitöltött kátyú",
            ErrorCodes.SurfacePeelOff => "Felületi hámlás",
            ErrorCodes.SurfacePerspiration => "Izzadás",
            _ => code.ToString()
        };

        private static Color DefaultColor(ErrorCodes code) =>
            _defaultColors[(int)code % _defaultColors.Length];

        private sealed class CodeItem
        {
            public readonly ErrorCodes Code;
            public readonly string Name;
            public CodeItem(ErrorCodes code, string name) { Code = code; Name = name; }
            public override string ToString() => $"{(int)Code}";
        }

        // ── per-type config row ──────────────────────────────────────────
        private sealed class TypeConfigRow : Panel
        {
            private const int RowH = 32;

            public TypeConfigRow(ErrorLayerControl layer, Action<ErrorLayerControl> onDelete)
            {
                Height = RowH;
                Margin = new Padding(0);
                BackColor = Theme.Surface;

                // color swatch
                var colorBtn = new Button
                {
                    Location = new Point(6, 5),
                    Size = new Size(22, 22),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = layer.LayerColor,
                    TabStop = false,
                    Cursor = Cursors.Hand,
                };
                colorBtn.FlatAppearance.BorderColor = Theme.Border;
                colorBtn.FlatAppearance.BorderSize = 1;
                colorBtn.Click += (s, e) =>
                {
                    using var dlg = new ColorDialog { Color = layer.LayerColor, FullOpen = true };
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        layer.LayerColor = dlg.Color;
                        colorBtn.BackColor = dlg.Color;
                    }
                };

                // numeric code label
                var codeLabel = new Label
                {
                    Text = ((int)layer.ErrorCode).ToString(),
                    Location = new Point(32, 0),
                    Size = new Size(22, RowH),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font(Theme.BaseFont.FontFamily, 7.5f),
                    ForeColor = Theme.TextMuted,
                };

                // delete button (right-anchored)
                var deleteBtn = new Button
                {
                    Text = "×",
                    Size = new Size(20, 20),
                    Location = new Point(Width - 24, 6),
                    FlatStyle = FlatStyle.Flat,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    TabStop = false,
                    Font = new Font(Theme.BaseFont.FontFamily, 10f),
                    ForeColor = Theme.TextMuted,
                };
                deleteBtn.FlatAppearance.BorderSize = 0;
                deleteBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 230, 230);
                deleteBtn.Click += (s, e) => onDelete(layer);

                // name text box (fills between code label and delete button)
                var nameBox = new TextBox
                {
                    Text = layer.LayerName,
                    Location = new Point(57, 7),
                    Height = 18,
                    BorderStyle = BorderStyle.None,
                    BackColor = Theme.Surface,
                    ForeColor = Theme.Text,
                    Font = Theme.BaseFont,
                };
                nameBox.TextChanged += (s, e) => layer.LayerName = nameBox.Text;

                Controls.Add(colorBtn);
                Controls.Add(codeLabel);
                Controls.Add(nameBox);
                Controls.Add(deleteBtn);

                // adjust nameBox width whenever the row resizes
                Resize += (s, e) =>
                {
                    nameBox.Width = deleteBtn.Left - nameBox.Left - 4;
                };

                // bottom separator line
                Paint += (s, e) =>
                {
                    using var pen = new Pen(Theme.Border);
                    e.Graphics.DrawLine(pen, 0, RowH - 1, Width, RowH - 1);
                };
            }
        }
    }
}
