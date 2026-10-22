using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PaveMetric
{
    /// <summary>
    /// Floating editor for the error catalog: every row is one error type whose code, name
    /// and color can be changed. Changes are applied to the palette layers immediately.
    /// </summary>
    internal class ErrorTypeToolbox : Form
    {
        private readonly ErrorLayerGroup _group;
        private readonly FlowLayoutPanel _flow;
        private readonly TextBox _addCodeBox;
        private readonly TextBox _addNameBox;
        private readonly Button _btnAdd;

        public event Action<ErrorLayerControl> TypeAdded;
        public event Action<ErrorLayerControl> TypeRemoved;
        /// <summary>Raised with (oldCode, newCode) after a type's code was changed.</summary>
        public event Action<int, int> CodeChanged;
        /// <summary>Raised after any edit, so the catalog can be persisted.</summary>
        public event EventHandler CatalogChanged;
        public event EventHandler UserClosed;

        /// <summary>Returns how many recorded errors use the given code (for delete confirmation).</summary>
        public Func<int, int> CountUsage { get; set; }

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
            Text = "Hibakatalógus";
            ShowInTaskbar = false;
            MinimumSize = new Size(280, 160);
            Size = new Size(340, 480);
            StartPosition = FormStartPosition.Manual;
            BackColor = Theme.Surface;
            Font = Theme.BaseFont;

            // ── column header ────────────────────────────────────────────
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 22,
                BackColor = Theme.Background,
            };
            header.Controls.Add(HeaderLabel("Szín", 4, 32));
            header.Controls.Add(HeaderLabel("Kód", 34, 40));
            header.Controls.Add(HeaderLabel("Megnevezés", 78, 120));

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

            _addCodeBox = new TextBox
            {
                Location = new Point(34, 11),
                Size = new Size(38, 22),
                Font = Theme.BaseFont,
                BackColor = Theme.Surface,
                ForeColor = Theme.Text,
            };

            _addNameBox = new TextBox
            {
                Location = new Point(78, 11),
                Size = new Size(120, 22),
                Font = Theme.BaseFont,
                BackColor = Theme.Surface,
                ForeColor = Theme.Text,
                PlaceholderText = "Új hibatípus neve",
            };
            _addNameBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { BtnAdd_Click(s, e); e.SuppressKeyPress = true; }
            };

            _btnAdd = new Button
            {
                Text = "Hozzáad",
                Location = new Point(206, 9),
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

            addPanel.Controls.Add(_addCodeBox);
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
            Controls.Add(header);
            Controls.Add(addPanel);

            Populate();
        }

        private static Label HeaderLabel(string text, int x, int width) => new Label
        {
            Text = text,
            Location = new Point(x, 0),
            Size = new Size(width, 22),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font(Theme.BaseFont.FontFamily, 7.5f),
            ForeColor = Theme.TextMuted,
        };

        public void Populate()
        {
            _flow.SuspendLayout();
            _flow.Controls.Clear();
            foreach (ErrorLayerControl layer in _group.ErrorLayers)
                _flow.Controls.Add(new TypeConfigRow(this, layer));
            _flow.ResumeLayout();

            _flow.SizeChanged -= OnFlowSizeChanged;
            _flow.SizeChanged += OnFlowSizeChanged;
            ResizeRows();
            _addCodeBox.Text = NextFreeCode().ToString();
        }

        private void OnFlowSizeChanged(object s, EventArgs e) => ResizeRows();

        private void ResizeRows()
        {
            int w = _flow.ClientSize.Width;
            foreach (Control c in _flow.Controls)
                c.Width = w;
        }

        private int NextFreeCode() =>
            _group.ErrorLayers.Count == 0 ? 10 : _group.ErrorLayers.Max(l => l.ErrorCode) + 1;

        private ErrorLayerControl FindByCode(int code, ErrorLayerControl except) =>
            _group.ErrorLayers.FirstOrDefault(l => l != except && l.ErrorCode == code);

        /// <summary>Parses and validates a code typed by the user; shows a warning when invalid.</summary>
        private bool TryGetCode(string text, ErrorLayerControl except, out int code)
        {
            if (!int.TryParse(text.Trim(), out code) || code <= 0)
            {
                Warn("A hibakód pozitív egész szám legyen.");
                return false;
            }
            var other = FindByCode(code, except);
            if (other != null)
            {
                Warn($"A(z) {code} kód már foglalt: „{other.LayerName}”.");
                return false;
            }
            return true;
        }

        private void Warn(string message) =>
            MessageBox.Show(this, message, "Hibakatalógus", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        private void BtnAdd_Click(object s, EventArgs e)
        {
            string name = _addNameBox.Text.Trim();
            if (name.Length == 0)
            {
                Warn("Adja meg az új hibatípus nevét.");
                _addNameBox.Focus();
                return;
            }
            if (!TryGetCode(_addCodeBox.Text, null, out int code))
            {
                _addCodeBox.Focus();
                return;
            }

            var newLayer = new ErrorLayerControl(_group)
            {
                ErrorCode = code,
                LayerName = name,
                LayerColor = _defaultColors[_group.ErrorLayers.Count % _defaultColors.Length],
                IsVisible = true,
            };
            _group.AddLayer(newLayer);
            TypeAdded?.Invoke(newLayer);
            _addNameBox.Text = "";
            Populate();
            OnCatalogChanged();
        }

        private void DeleteType(ErrorLayerControl layer)
        {
            int used = CountUsage?.Invoke(layer.ErrorCode) ?? 0;
            string message = used > 0
                ? $"A(z) „{layer.LayerName}” típushoz {used} rögzített hiba tartozik.\n" +
                  "A hibák megmaradnak a projektben, de típus nélkül nem jelennek meg.\n\nBiztosan törli?"
                : $"Törli a(z) „{layer.LayerName}” hibatípust?";
            if (MessageBox.Show(this, message, "Hibatípus törlése", MessageBoxButtons.YesNo,
                    used > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            TypeRemoved?.Invoke(layer);
            _group.RemoveLayer(layer);
            Populate();
            OnCatalogChanged();
        }

        private void OnCatalogChanged() => CatalogChanged?.Invoke(this, EventArgs.Empty);

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

        // ── per-type config row ──────────────────────────────────────────
        private sealed class TypeConfigRow : Panel
        {
            private const int RowH = 32;

            public TypeConfigRow(ErrorTypeToolbox owner, ErrorLayerControl layer)
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
                        owner.OnCatalogChanged();
                    }
                };

                // editable numeric code; committed on Enter or on validation when focus leaves
                // (Validating, not Leave: a message box shown from Leave swallows the click)
                var codeBox = new TextBox
                {
                    Text = layer.ErrorCode.ToString(),
                    Location = new Point(34, 7),
                    Size = new Size(38, 18),
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Theme.Surface,
                    ForeColor = Theme.Text,
                    Font = Theme.BaseFont,
                };
                void CommitCode()
                {
                    int oldCode = layer.ErrorCode;
                    if (codeBox.Text.Trim() == oldCode.ToString())
                        return;
                    if (!owner.TryGetCode(codeBox.Text, layer, out int newCode))
                    {
                        codeBox.Text = oldCode.ToString();
                        return;
                    }
                    layer.ErrorCode = newCode;
                    codeBox.Text = newCode.ToString();
                    owner.CodeChanged?.Invoke(oldCode, newCode);
                    owner.OnCatalogChanged();
                }
                codeBox.Validating += (s, e) => CommitCode();
                codeBox.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter) { CommitCode(); e.SuppressKeyPress = true; }
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
                deleteBtn.Click += (s, e) => owner.DeleteType(layer);

                // name text box (fills between code box and delete button)
                var nameBox = new TextBox
                {
                    Text = layer.LayerName,
                    Location = new Point(78, 7),
                    Height = 18,
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Theme.Surface,
                    ForeColor = Theme.Text,
                    Font = Theme.BaseFont,
                };
                nameBox.TextChanged += (s, e) =>
                {
                    // never let the palette show an empty label while typing
                    if (nameBox.Text.Trim().Length > 0)
                        layer.LayerName = nameBox.Text.Trim();
                };
                nameBox.Validating += (s, e) =>
                {
                    // an emptied name falls back to the last non-empty one
                    nameBox.Text = layer.LayerName;
                    owner.OnCatalogChanged();
                };

                Controls.Add(colorBtn);
                Controls.Add(codeBox);
                Controls.Add(nameBox);
                Controls.Add(deleteBtn);

                // adjust nameBox width whenever the row resizes
                Resize += (s, e) =>
                {
                    nameBox.Width = Math.Max(40, deleteBtn.Left - nameBox.Left - 4);
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
