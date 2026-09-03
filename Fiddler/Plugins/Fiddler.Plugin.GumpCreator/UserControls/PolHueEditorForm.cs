using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Ultima; // Required for Hues class

namespace Fiddler.Plugin.GumpCreator.UserControls
{
    public partial class PolHueEditorForm : Form
    {
        private ListBox _hueListBox;
        private TextBox _searchTextBox;
        private Button _okButton;
        private Button _cancelButton;
        private Panel _previewPanel;
        private Label _previewLabel;

        public int SelectedHue { get; private set; }
        private List<Hue> _allHues;
        private List<Hue> _displayedHues;

        public PolHueEditorForm(int initialHue)
        {
            InitializeComponent();
            _allHues = Hues.List.Take(3000).Where(h => h != null && !string.IsNullOrWhiteSpace(h.Name) && h.Name != "Null").ToList(); // Take first 3000, filter out unnamed/null
            _displayedHues = new List<Hue>(_allHues);
            SelectedHue = initialHue;
            PopulateListBox();
            SelectHueInList(initialHue);
        }

        private void InitializeComponent()
        {
            this.Text = "Select POL Hue";
            this.Size = new Size(400, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            _searchTextBox = new TextBox { Dock = DockStyle.Top, PlaceholderText = "Search Hue (ID or Name)" };
            _searchTextBox.TextChanged += SearchTextBox_TextChanged;

            _hueListBox = new ListBox
            {
                Dock = DockStyle.Fill,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 20, // Adjust as needed
                IntegralHeight = false
            };
            _hueListBox.DrawItem += HueListBox_DrawItem;
            _hueListBox.DoubleClick += (s, e) => { if (_hueListBox.SelectedItem != null) { _okButton.PerformClick(); } };
            _hueListBox.SelectedIndexChanged += HueListBox_SelectedIndexChanged;

            _previewPanel = new Panel { Dock = DockStyle.Bottom, Height = 40, BorderStyle = BorderStyle.Fixed3D };
            _previewLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Text = "Preview" };
            _previewPanel.Controls.Add(_previewLabel);

            _okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Dock = DockStyle.Right };
            _cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Dock = DockStyle.Left };

            Panel bottomButtonPanel = new Panel { Dock = DockStyle.Bottom, Height = 30 };
            bottomButtonPanel.Controls.Add(_okButton);
            bottomButtonPanel.Controls.Add(_cancelButton);

            this.Controls.Add(_hueListBox);
            this.Controls.Add(_searchTextBox);
            this.Controls.Add(_previewPanel);
            this.Controls.Add(bottomButtonPanel);

            this.AcceptButton = _okButton;
            this.CancelButton = _cancelButton;
            this.Load += (s,e) => _searchTextBox.Focus();
        }

        private void PopulateListBox()
        {
            _hueListBox.BeginUpdate();
            _hueListBox.Items.Clear();
            foreach (var hue in _displayedHues)
            {
                _hueListBox.Items.Add(hue);
            }
            _hueListBox.EndUpdate();
        }

        private void SearchTextBox_TextChanged(object sender, EventArgs e)
        {
            string searchText = _searchTextBox.Text.ToLower().Trim();
            if (string.IsNullOrEmpty(searchText))
            {
                _displayedHues = new List<Hue>(_allHues);
            }
            else
            {
                _displayedHues = _allHues.Where(h =>
                    h.Index.ToString().Contains(searchText) ||
                    h.Name.ToLower().Contains(searchText)
                ).ToList();
            }
            PopulateListBox();
        }

        private void HueListBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _hueListBox.Items.Count) return;
            e.DrawBackground();
            Hue hue = (Hue)_hueListBox.Items[e.Index];
            Color swatchColor = hue.GetColor(16); // Use 16th color as a representative swatch
            
            using (SolidBrush swatchBrush = new SolidBrush(swatchColor))
            {
                e.Graphics.FillRectangle(swatchBrush, e.Bounds.X + 2, e.Bounds.Y + 2, 16, e.Bounds.Height - 4);
            }
            e.Graphics.DrawRectangle(Pens.Black, e.Bounds.X + 2, e.Bounds.Y + 2, 16, e.Bounds.Height - 4);

            string displayText = $"{hue.Index} - {hue.Name}";
            using (Brush textBrush = new SolidBrush(e.ForeColor))
            {
                e.Graphics.DrawString(displayText, e.Font, textBrush, e.Bounds.X + 22, e.Bounds.Y + (e.Bounds.Height - e.Font.Height) / 2);
            }
            e.DrawFocusRectangle();
        }

        private void HueListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_hueListBox.SelectedItem is Hue selectedHue)
            {
                SelectedHue = selectedHue.Index;
                _previewPanel.BackColor = selectedHue.GetColor(16);
                _previewLabel.Text = $"{selectedHue.Index} - {selectedHue.Name}";
                // Determine contrasting forecolor for preview label
                _previewLabel.ForeColor = (selectedHue.GetColor(16).GetBrightness() < 0.5) ? Color.White : Color.Black;
            }
        }
        
        private void SelectHueInList(int hueId)
        {
            for (int i = 0; i < _hueListBox.Items.Count; ++i)
            {
                if (((Hue)_hueListBox.Items[i]).Index == hueId)
                {
                    _hueListBox.SelectedIndex = i;
                    return;
                }
            }
            // If not found, select first item or none
            if (_hueListBox.Items.Count > 0) _hueListBox.SelectedIndex = 0; 
        }
    }
} 