using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.IO; // For File operations
using System.Text.Json; // For JSON serialization
// using System.Text.Json.Serialization; // For potential JsonPolymorphic attributes if we go that route

namespace Fiddler.Plugin.GumpCreator.UserControls
{
    public class ElementPropertiesWindow : Form
    {
        private PropertyGrid _propertyGrid;
        private Panel _gumpSettingsPanel;
        private CheckBox _closableCheckBox, _movableCheckBox, _disposableCheckBox;
        private Button _exportButton, _exportUOXButton;
        private Button _saveGumpButton;
        private Button _loadGumpButton;
        private FlowLayoutPanel _fileButtonsPanel; // To hold save/load/export

        private GumpCreatorControl _gumpCreatorControlRef; // Reference to the main control
        private bool _allowHardClose = false; // Flag to allow actual closing

        // Public properties for Gump Settings
        public bool IsGumpClosable { get; private set; }
        public bool IsGumpMovable { get; private set; }
        public bool IsGumpDisposable { get; private set; }

        public ElementPropertiesWindow(GumpCreatorControl gumpCreatorControl)
        {
            _gumpCreatorControlRef = gumpCreatorControl;

            InitializeComponent();
            // Initialize settings from the main control's current state or defaults
            IsGumpClosable = _gumpCreatorControlRef.IsGumpClosable;
            IsGumpMovable = _gumpCreatorControlRef.IsGumpMovable;
            IsGumpDisposable = _gumpCreatorControlRef.IsGumpDisposable;
            _closableCheckBox.Checked = IsGumpClosable;
            _movableCheckBox.Checked = IsGumpMovable;
            _disposableCheckBox.Checked = IsGumpDisposable;

            this.Text = "Element & Gump Properties";
            this.Size = new Size(480, 700); // Increased height for new buttons
            this.MinimumSize = new Size(480, 450);
            this.StartPosition = FormStartPosition.Manual; // Or wherever you prefer
            this.FormClosing += ElementPropertiesWindow_FormClosing;
        }

        private void ElementPropertiesWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Only hide if user closes the window via 'X' and hard close is not requested
            if (e.CloseReason == CloseReason.UserClosing && !_allowHardClose)
            {
                e.Cancel = true;
                this.Hide();
            }
            // Otherwise, allow the form to close (e.g., if _allowHardClose is true or Application.Exit, etc.)
        }

        public void PrepareForHardClose()
        {
            _allowHardClose = true;
        }

        public void SetGumpSettings(bool closable, bool movable, bool disposable)
        {
            IsGumpClosable = closable;
            IsGumpMovable = movable;
            IsGumpDisposable = disposable;

            // Update checkboxes to reflect the new settings
            if (_closableCheckBox.InvokeRequired)
            {
                _closableCheckBox.Invoke(new Action(() => _closableCheckBox.Checked = IsGumpClosable));
            }
            else
            {
                _closableCheckBox.Checked = IsGumpClosable;
            }

            if (_movableCheckBox.InvokeRequired)
            {
                _movableCheckBox.Invoke(new Action(() => _movableCheckBox.Checked = IsGumpMovable));
            }
            else
            {
                _movableCheckBox.Checked = IsGumpMovable;
            }

            if (_disposableCheckBox.InvokeRequired)
            {
                _disposableCheckBox.Invoke(new Action(() => _disposableCheckBox.Checked = IsGumpDisposable));
            }
            else
            {
                _disposableCheckBox.Checked = IsGumpDisposable;
            }
        }

        private void InitializeComponent()
        {
            _propertyGrid = new PropertyGrid { Dock = DockStyle.Fill, PropertySort = PropertySort.Categorized };

            _gumpSettingsPanel = new Panel { Dock = DockStyle.Bottom, Height = 75, Padding = new Padding(5) };
            var gumpSettingsFlowPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown };
            _closableCheckBox = new CheckBox { Text = "Closable", AutoSize = true }; 
            _closableCheckBox.CheckedChanged += (s, ev) => IsGumpClosable = _closableCheckBox.Checked;
            _movableCheckBox = new CheckBox { Text = "Movable", AutoSize = true }; 
            _movableCheckBox.CheckedChanged += (s, ev) => IsGumpMovable = _movableCheckBox.Checked;
            _disposableCheckBox = new CheckBox { Text = "Disposable", AutoSize = true }; 
            _disposableCheckBox.CheckedChanged += (s, ev) => IsGumpDisposable = _disposableCheckBox.Checked;
            
            gumpSettingsFlowPanel.Controls.AddRange(new Control[] { _closableCheckBox, _movableCheckBox, _disposableCheckBox });
            _gumpSettingsPanel.Controls.Add(gumpSettingsFlowPanel);

            _fileButtonsPanel = new FlowLayoutPanel 
            { 
                Dock = DockStyle.Bottom, 
                FlowDirection = FlowDirection.TopDown, 
                AutoSize = true, 
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10, 5, 10, 5),
                WrapContents = false
            };

            _saveGumpButton = new Button { Text = "Save Gump to File...", Width = 200, Height = 35, Margin = new Padding(0, 2, 0, 2) };
            _saveGumpButton.Click += SaveGumpButton_Click;

            _loadGumpButton = new Button { Text = "Load Gump from File...", Width = 200, Height = 35, Margin = new Padding(0, 2, 0, 2) };
            _loadGumpButton.Click += LoadGumpButton_Click;

            _exportButton = new Button { Text = "Export to POL", Width = 200, Height = 35, Margin = new Padding(0, 2, 0, 2) };
            _exportButton.Click += ExportButton_Click;

            _exportUOXButton = new Button { Text = "Export to UOX", Width = 200, Height = 35, Margin = new Padding(0, 2, 0, 2) };
            _exportUOXButton.Click += ExportUOXButton_Click;

            // Add buttons one by one to ensure vertical layout
            _fileButtonsPanel.Controls.Add(_saveGumpButton);
            _fileButtonsPanel.Controls.Add(_loadGumpButton);
            _fileButtonsPanel.Controls.Add(_exportButton);
            _fileButtonsPanel.Controls.Add(_exportUOXButton);
            
            // Order of adding controls matters for Dock.Bottom
            this.Controls.Add(_propertyGrid);      // Fills remaining space
            this.Controls.Add(_fileButtonsPanel);  // This should be at the very bottom now
            this.Controls.Add(_gumpSettingsPanel); // This should be above _fileButtonsPanel

            // ===== NEW CODE: use the same icon as the main UOFiddler window =====
            ApplyApplicationIcon();
        }

        private void ApplyApplicationIcon()
        {
            try
            {
                // 1) Prefer the icon of the main UOFiddler window (its title-bar icon)
                Form mainForm = Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null;
                if (mainForm?.Icon != null)
                {
                    this.Icon = mainForm.Icon;
                    return;
                }

                // 2) Fallback: pull the icon straight from the running .exe
                string exePath = System.Reflection.Assembly.GetEntryAssembly()?.Location;
                if (!string.IsNullOrEmpty(exePath))
                {
                    this.Icon = Icon.ExtractAssociatedIcon(exePath);
                }
            }
            catch
            {
                // If the icon can't be resolved, just keep the default icon.
            }
        }

        public void UpdateSelectedObject(object selectedObject)
        {
            _propertyGrid.SelectedObject = selectedObject;
        }
        
        public void LinkPropertyValueChangedEvent(PropertyValueChangedEventHandler handler)
        {
            _propertyGrid.PropertyValueChanged += handler;
        }

        private void SaveGumpButton_Click(object sender, EventArgs e)
        {
            GumpSaveData saveData = new GumpSaveData
            {
                IsClosable = this.IsGumpClosable,
                IsMovable = this.IsGumpMovable,
                IsDisposable = this.IsGumpDisposable,
                MaxPageIndex = _gumpCreatorControlRef.GetCurrentMaxPageIndex(),
                Elements = new List<object>() // Initialize as List<object>
            };

            foreach (var element in _gumpCreatorControlRef.GetCurrentCanvasItems())
            {
                // CanvasElementSaveData elData = null; // No longer needed here

                if (element is CanvasGumpPicItem gPic)
                {
                    var gPicSaveData = new CanvasGumpPicItemSaveData 
                    {
                        ElementType = element.ElementType,
                        X = element.X, Y = element.Y, Width = element.Width, Height = element.Height, Page = element.Page, Z = element.Z,
                        GumpId = gPic.GumpId, Hue = gPic.Hue 
                    };
                    saveData.Elements.Add(gPicSaveData); 
                }
                else if (element is CanvasTextItem tItem)
                {
                    var tItemSaveData = new CanvasTextItemSaveData 
                    {
                        ElementType = element.ElementType,
                        X = element.X, Y = element.Y, Width = element.Width, Height = element.Height, Page = element.Page, Z = element.Z,
                        Text = tItem.Text, TextColorHue = tItem.TextColorHue 
                    };
                    saveData.Elements.Add(tItemSaveData);
                }
                else if (element is CanvasButtonItem bItem)
                {
                    var bItemSaveData = new CanvasButtonItemSaveData 
                    { 
                        ElementType = element.ElementType,
                        X = element.X, Y = element.Y, Width = element.Width, Height = element.Height, Page = element.Page, Z = element.Z,
                        ReleasedGumpId = bItem.ReleasedGumpId, PressedGumpId = bItem.PressedGumpId, 
                        ReturnValue = bItem.ReturnValue, TargetPageId = bItem.TargetPageId 
                    };
                    saveData.Elements.Add(bItemSaveData);
                }
                else if (element is CanvasCheckboxItem chkBox)
                {
                    var chkBoxSaveData = new CanvasCheckboxItemSaveData 
                    { 
                        ElementType = element.ElementType,
                        X = element.X, Y = element.Y, Width = element.Width, Height = element.Height, Page = element.Page, Z = element.Z,
                        UncheckedId = chkBox.UncheckedId, CheckedId = chkBox.CheckedId, 
                        InitialStatus = chkBox.InitialStatus, ButtonValue = chkBox.ButtonValue 
                    };
                    saveData.Elements.Add(chkBoxSaveData);
                }
                else if (element is CanvasRadioButtonItem rdoBtn)
                {
                    var rdoBtnSaveData = new CanvasRadioButtonItemSaveData 
                    { 
                        ElementType = element.ElementType,
                        X = element.X, Y = element.Y, Width = element.Width, Height = element.Height, Page = element.Page, Z = element.Z,
                        UnpressedId = rdoBtn.UnpressedId, PressedId = rdoBtn.PressedId, 
                        InitialStatus = rdoBtn.InitialStatus, ButtonValue = rdoBtn.ButtonValue, GroupId = rdoBtn.GroupId 
                    };
                    saveData.Elements.Add(rdoBtnSaveData);
                }
                else if (element is CanvasTextEntryItem teItem)
                {
                    var teItemSaveData = new CanvasTextEntryItemSaveData 
                    { 
                        ElementType = element.ElementType,
                        X = element.X, Y = element.Y, Width = element.Width, Height = element.Height, Page = element.Page, Z = element.Z,
                        InitialText = teItem.InitialText, TextColorHue = teItem.TextColorHue, 
                        TextId = teItem.TextId, CharacterLimit = teItem.CharacterLimit 
                    };
                    saveData.Elements.Add(teItemSaveData);
                }
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Gump Creator JSON Files (*.gumpjson)|*.gumpjson|All Files (*.*)|*.*";
                sfd.Title = "Save Gump Design";
                sfd.DefaultExt = "gumpjson";
                sfd.AddExtension = true;
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        string jsonString = JsonSerializer.Serialize(saveData, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(sfd.FileName, jsonString);
                        MessageBox.Show(this, "Gump design saved successfully!", "Save Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, $"Error saving gump design: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void LoadGumpButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Gump Creator JSON Files (*.gumpjson)|*.gumpjson|All Files (*.*)|*.*";
                ofd.Title = "Load Gump Design";
                ofd.DefaultExt = "gumpjson";
                ofd.CheckFileExists = true;
                ofd.CheckPathExists = true;

                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        string jsonString = File.ReadAllText(ofd.FileName);
                        GumpSaveData loadedData = JsonSerializer.Deserialize<GumpSaveData>(jsonString);
                        
                        if (loadedData != null)
                        {
                            _gumpCreatorControlRef.LoadGumpFromData(loadedData);
                            // Update this window's gump settings display based on loaded data
                            IsGumpClosable = loadedData.IsClosable;
                            IsGumpMovable = loadedData.IsMovable;
                            IsGumpDisposable = loadedData.IsDisposable;
                            _closableCheckBox.Checked = IsGumpClosable;
                            _movableCheckBox.Checked = IsGumpMovable;
                            _disposableCheckBox.Checked = IsGumpDisposable;

                            MessageBox.Show(this, "Gump design loaded successfully!", "Load Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show(this, "Failed to deserialize gump data. File might be corrupt or in the wrong format.", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        MessageBox.Show(this, $"Error deserializing JSON: {jsonEx.Message}\n\nFile: {ofd.FileName}", "JSON Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, $"Error loading gump design: {ex.Message}\n\nFile: {ofd.FileName}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ExportButton_Click(object sender, EventArgs e)
        {
            // Get canvas items from the main control
            var canvasItems = _gumpCreatorControlRef.GetCurrentCanvasItems(); 
            PolGumpExporter.Export(this, canvasItems, IsGumpClosable, IsGumpMovable, IsGumpDisposable);
        }

        private void ExportUOXButton_Click(object sender, EventArgs e)
        {
            // Get canvas items from the main control
            var canvasItems = _gumpCreatorControlRef.GetCurrentCanvasItems(); 
            UOXGumpExporter.Export(this, canvasItems, IsGumpClosable, IsGumpMovable, IsGumpDisposable);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.FormClosing -= ElementPropertiesWindow_FormClosing; // Unsubscribe to avoid issues during dispose
                // Unhook events if necessary, dispose controls
                _propertyGrid?.Dispose();
                _gumpSettingsPanel?.Dispose();
                _closableCheckBox?.Dispose();
                _movableCheckBox?.Dispose();
                _disposableCheckBox?.Dispose();
                _exportButton?.Dispose();
                _saveGumpButton?.Dispose();
                _loadGumpButton?.Dispose();
                _fileButtonsPanel?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
} 