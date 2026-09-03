using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
// No Ultima using needed here if we only use ClientTextHue and ClientTextHueCollection

namespace Fiddler.Plugin.GumpCreator.UserControls
{
    public partial class ClientTextHueEditorForm : Form
    {
        private ListBox _hueListBox;
        private TextBox _searchTextBox;
        private Button _okButton;
        private Button _cancelButton;
        private Panel _previewPanel;
        private Label _previewLabel;

        public int SelectedHueId { get; private set; }
        private List<ClientTextHue> _allClientHues;
        private List<ClientTextHue> _displayedClientHues;

        public ClientTextHueEditorForm(int initialHueId)
        {
            InitializeComponent();
            _allClientHues = ClientTextHueCollection.Hues.OrderBy(h => h.HueId).ToList();
            _displayedClientHues = new List<ClientTextHue>(_allClientHues);
            SelectedHueId = initialHueId;
            PopulateListBox();
            SelectHueInList(initialHueId);
        }

        private void InitializeComponent()
        {
            this.Text = "Select Client Text Hue";
            this.Size = new Size(400, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            _searchTextBox = new TextBox { Dock = DockStyle.Top, PlaceholderText = "Search Text Hue (ID or Name)" };
            _searchTextBox.TextChanged += SearchTextBox_TextChanged;

            _hueListBox = new ListBox
            {
                Dock = DockStyle.Fill,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 20, 
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
            foreach (var hue in _displayedClientHues)
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
                _displayedClientHues = new List<ClientTextHue>(_allClientHues);
            }
            else
            {
                _displayedClientHues = _allClientHues.Where(h =>
                    h.HueId.ToString().Contains(searchText) ||
                    h.Name.ToLower().Contains(searchText)
                ).ToList();
            }
            PopulateListBox();
        }

        private void HueListBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _hueListBox.Items.Count) return;
            e.DrawBackground();
            ClientTextHue clientHue = (ClientTextHue)_hueListBox.Items[e.Index];
            
            using (SolidBrush swatchBrush = new SolidBrush(clientHue.DisplayColor))
            {
                e.Graphics.FillRectangle(swatchBrush, e.Bounds.X + 2, e.Bounds.Y + 2, 16, e.Bounds.Height - 4);
            }
            e.Graphics.DrawRectangle(Pens.Black, e.Bounds.X + 2, e.Bounds.Y + 2, 16, e.Bounds.Height - 4);

            string displayText = clientHue.ToString(); // Uses override from ClientTextHue struct
            using (Brush textBrush = new SolidBrush(e.ForeColor))
            {
                e.Graphics.DrawString(displayText, e.Font, textBrush, e.Bounds.X + 22, e.Bounds.Y + (e.Bounds.Height - e.Font.Height) / 2);
            }
            e.DrawFocusRectangle();
        }

        private void HueListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_hueListBox.SelectedItem is ClientTextHue selectedClientHue)
            {
                SelectedHueId = selectedClientHue.HueId;
                _previewPanel.BackColor = selectedClientHue.DisplayColor;
                _previewLabel.Text = selectedClientHue.ToString();
                _previewLabel.ForeColor = (selectedClientHue.DisplayColor.GetBrightness() < 0.5) ? Color.White : Color.Black;
            }
        }
        
        private void SelectHueInList(int hueId)
        {
            for (int i = 0; i < _hueListBox.Items.Count; ++i)
            {
                if (((ClientTextHue)_hueListBox.Items[i]).HueId == hueId)
                {
                    _hueListBox.SelectedIndex = i;
                    return;
                }
            }
            if (_hueListBox.Items.Count > 0) _hueListBox.SelectedIndex = 0; 
        }
    }
} 