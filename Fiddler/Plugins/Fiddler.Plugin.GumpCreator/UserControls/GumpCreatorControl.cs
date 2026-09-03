using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Ultima;
using System.Text.Json;

namespace Fiddler.Plugin.GumpCreator.UserControls
{
    public class GumpCreatorControl : UserControl
    {
        private SplitContainer _mainSplitContainer;
        private SplitContainer _canvasSplitContainer;
        private TextBox _searchTextBox;
        private ListBox _gumpListBox;
        private Panel _gumpCanvasPanel;
        private ToolStrip _toolStrip;
        private ToolStripButton _prevPageButton, _nextPageButton, _addPageButton;
        private ToolStripButton _addGumpPicButton, _addTextButton, _addButtonButton;
        private ToolStripButton _addCheckboxButton, _addRadioButton, _addTextEntryButton;
        private ToolStripLabel _currentPageLabel;
        private ElementPropertiesWindow _elementPropertiesWindow;

        private List<int> _allGumpIds = new List<int>();
        private List<int> _displayedGumpIds = new List<int>();
        private List<CanvasElement> _canvasItems = new List<CanvasElement>();

        private CanvasElement _draggedCanvasItem = null;
        private CanvasElement _selectedCanvasItem = null;
        private Point _dragOffset;
        private ResizeHandle _activeResizeHandle = ResizeHandle.None;
        private Point _resizeStartPoint;
        private Rectangle _originalResizeBounds;

        private int _currentPageIndex = 1;
        private int _maxPageIndex = 1;

        private const int SnapThreshold = 8; // Pixels for snapping sensitivity

        private ContextMenuStrip _canvasContextMenu;
        private ToolStripMenuItem _bringToFrontMenuItem;
        private ToolStripMenuItem _sendToBackMenuItem;
        private ToolStripMenuItem _bringForwardMenuItem;
        private ToolStripMenuItem _sendBackwardMenuItem;

        private CanvasElement _copiedElement = null; // For internal copy/paste
        private const int ArrowKeyStep = 1; // Base step for Shift+Arrow, not used for Alt+Arrow anymore
        private const int CtrlArrowKeyStepGumpId = 1;

        // Fields for Alt+Arrow accelerated movement
        private Keys _lastAltMoveKey = Keys.None;
        private int _altMoveConsecutiveCount = 0;
        private const int BaseAltMoveStep = 1;      // Initial step for Alt+Arrow
        private const int MaxAltMoveStep = 8;       // Maximum step for accelerated Alt+Arrow
        private const int AccelerationStartCount = 16; // Consecutive presses before acceleration starts
        private const int AccelerationIncrement = 2;  // Pixels to add to step per acceleration phase

        private Form _mainFormInstance; // To store reference to the main application window
        private Size _originalMainFormSize; // To store original size of the main form
        private bool _mainFormResizedByMe = false; // Flag to track if we resized it
        private static readonly Size PreferredMainFormSizeForGumpCreator = new Size(1400, 900); // Desired size
        private static readonly Size MinimumMainFormSizeForGumpCreator = new Size(1024, 768); // Smallest reasonable size if preferred is too big

        public GumpCreatorControl()
        {
            InitializeComponent();
            LoadGumpList();
            UpdatePageDisplay();

            _elementPropertiesWindow = new ElementPropertiesWindow(this);
            _elementPropertiesWindow.LinkPropertyValueChangedEvent(this.PropertyGrid_PropertyValueChanged);

            // Dock the properties window into the right pane instead of floating it.
            _elementPropertiesWindow.TopLevel = false;                       // make it hostable as a child control
            _elementPropertiesWindow.FormBorderStyle = FormBorderStyle.None; // no separate title bar/border
            _elementPropertiesWindow.Dock = DockStyle.Fill;                  // fill the right pane
            _canvasSplitContainer.Panel2.Controls.Add(_elementPropertiesWindow);
            _elementPropertiesWindow.Show();

            this.HandleCreated += GumpCreatorControl_HandleCreated;
            this.VisibleChanged += GumpCreatorControl_VisibleChanged; 
        }

        private void GumpCreatorControl_HandleCreated(object sender, EventArgs e)
        {
            _mainFormInstance = this.FindForm();
            if (_mainFormInstance != null)
            {
                if (this.Visible)
                {
                    GumpCreatorControl_VisibleChanged(this, EventArgs.Empty);
                }
            }
        }

        private void GumpCreatorControl_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible)
            { 
                // Resize main form
                if (_mainFormInstance != null && !_mainFormResizedByMe)
                {
                    _originalMainFormSize = _mainFormInstance.Size;
                    Screen currentScreen = Screen.FromControl(_mainFormInstance);
                    Size targetSize = PreferredMainFormSizeForGumpCreator;

                    // Adjust if preferred size is too large for the screen
                    if (targetSize.Width > currentScreen.WorkingArea.Width)
                    {
                        targetSize.Width = currentScreen.WorkingArea.Width;
                    }
                    if (targetSize.Height > currentScreen.WorkingArea.Height)
                    {
                        targetSize.Height = currentScreen.WorkingArea.Height;
                    }

                    // Ensure it's not smaller than a defined minimum (or original if original is larger than min)
                    targetSize.Width = Math.Max(targetSize.Width, MinimumMainFormSizeForGumpCreator.Width);
                    targetSize.Height = Math.Max(targetSize.Height, MinimumMainFormSizeForGumpCreator.Height);
                    // And ensure it's not smaller than what it was, if original was already quite large
                    targetSize.Width = Math.Max(targetSize.Width, _originalMainFormSize.Width);
                    targetSize.Height = Math.Max(targetSize.Height, _originalMainFormSize.Height);

                    if (_mainFormInstance.WindowState == FormWindowState.Normal && _originalMainFormSize != targetSize)
                    {
                        _mainFormInstance.Size = targetSize;
                         _mainFormResizedByMe = true;
                    }
                    else if(_mainFormInstance.WindowState != FormWindowState.Normal)
                    { // If maximized or minimized, don't try to resize, but note we would have.
                        _mainFormResizedByMe = true; // Set to true so it restores if user later normalizes and switches tab
                        _originalMainFormSize = new Size(targetSize.Width, targetSize.Height); // Store target as original for restore
                    }
                }
            }
            else // Control is becoming hidden
            {
                // Restore main form size if we changed it
                if (_mainFormInstance != null && _mainFormResizedByMe)
                {
                    if (_mainFormInstance.WindowState == FormWindowState.Normal && _mainFormInstance.Size != _originalMainFormSize)
                    {
                         _mainFormInstance.Size = _originalMainFormSize;
                    }
                    _mainFormResizedByMe = false;
                }
            }
        }

        private void InitializeComponent()
        {
            _mainSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 200 
            };

            var leftPanel = new Panel { Dock = DockStyle.Fill };
            _searchTextBox = new TextBox { Dock = DockStyle.Top, PlaceholderText = "Search Gump ID (e.g., 123 or 0x7B)" };
            _searchTextBox.TextChanged += SearchTextBox_TextChanged;
            _gumpListBox = new ListBox { Dock = DockStyle.Fill, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 75, IntegralHeight = false };
            _gumpListBox.DrawItem += GumpListBox_DrawItem;
            _gumpListBox.MouseDown += GumpListBox_MouseDown;
            leftPanel.Controls.AddRange(new Control[] { _gumpListBox, _searchTextBox });

            var canvasAreaPanel = new Panel { Dock = DockStyle.Fill };
            _toolStrip = new ToolStrip { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };
            _prevPageButton = new ToolStripButton("<") { ToolTipText = "Previous Page" }; _prevPageButton.Click += (s, e) => ChangePage(-1);
            _currentPageLabel = new ToolStripLabel("Page: 1/1");
            _nextPageButton = new ToolStripButton(">") { ToolTipText = "Next Page" }; _nextPageButton.Click += (s, e) => ChangePage(1);
            _addPageButton = new ToolStripButton("+") { ToolTipText = "Add New Page" }; _addPageButton.Click += AddPageButton_Click;
            _addGumpPicButton = new ToolStripButton("Add GumpPic") { DisplayStyle = ToolStripItemDisplayStyle.Text }; _addGumpPicButton.Click += AddGumpPicButton_Click;
            _addTextButton = new ToolStripButton("Add Text") { DisplayStyle = ToolStripItemDisplayStyle.Text }; _addTextButton.Click += AddTextButton_Click;
            _addButtonButton = new ToolStripButton("Add Button") { DisplayStyle = ToolStripItemDisplayStyle.Text }; _addButtonButton.Click += AddButtonButton_Click;
            _addCheckboxButton = new ToolStripButton("Add CheckBox") { DisplayStyle = ToolStripItemDisplayStyle.Text }; _addCheckboxButton.Click += AddCheckboxButton_Click;
            _addRadioButton = new ToolStripButton("Add RadioBtn") { DisplayStyle = ToolStripItemDisplayStyle.Text }; _addRadioButton.Click += AddRadioButton_Click;
            _addTextEntryButton = new ToolStripButton("Add TextEntry") { DisplayStyle = ToolStripItemDisplayStyle.Text }; _addTextEntryButton.Click += AddTextEntryButton_Click;
            _toolStrip.Items.AddRange(new ToolStripItem[] { _prevPageButton, _currentPageLabel, _nextPageButton, new ToolStripSeparator(), _addPageButton, new ToolStripSeparator(), _addGumpPicButton, _addTextButton, _addButtonButton, new ToolStripSeparator(), _addCheckboxButton, _addRadioButton, _addTextEntryButton });
            
            _gumpCanvasPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.DarkGray, AllowDrop = true, AutoScroll = true, TabStop = true };
            _gumpCanvasPanel.Paint += GumpCanvasPanel_Paint; _gumpCanvasPanel.DragEnter += GumpCanvasPanel_DragEnter; _gumpCanvasPanel.DragDrop += GumpCanvasPanel_DragDrop; _gumpCanvasPanel.MouseDown += GumpCanvasPanel_MouseDown; _gumpCanvasPanel.MouseMove += GumpCanvasPanel_MouseMove; _gumpCanvasPanel.MouseUp += GumpCanvasPanel_MouseUp; _gumpCanvasPanel.KeyDown += GumpCanvasPanel_KeyDown;
            _gumpCanvasPanel.AutoScrollMinSize = new Size(2000, 1500); // Larger initial blank canvas

            // Initialize ContextMenu for Z-Order
            _canvasContextMenu = new ContextMenuStrip();
            _bringToFrontMenuItem = new ToolStripMenuItem("Bring to Front", null, BringToFront_Click);
            _sendToBackMenuItem = new ToolStripMenuItem("Send to Back", null, SendToBack_Click);
            _bringForwardMenuItem = new ToolStripMenuItem("Bring Forward", null, BringForward_Click);
            _sendBackwardMenuItem = new ToolStripMenuItem("Send Backward", null, SendBackward_Click);
            _canvasContextMenu.Items.AddRange(new ToolStripItem[] { 
                _bringToFrontMenuItem, _sendToBackMenuItem, new ToolStripSeparator(), 
                _bringForwardMenuItem, _sendBackwardMenuItem 
            });
            _canvasContextMenu.Opening += CanvasContextMenu_Opening;
            _gumpCanvasPanel.ContextMenuStrip = _canvasContextMenu;

            // Split the canvas area: drawing canvas on the left, docked properties on the right.
            _canvasSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                FixedPanel = FixedPanel.Panel2,   // keep the properties pane a constant width when resizing
            };
            _canvasSplitContainer.Panel1.Controls.Add(_gumpCanvasPanel); // canvas fills the left pane
            _canvasSplitContainer.SizeChanged += (s, e) => UpdatePropertiesPaneWidth();

            canvasAreaPanel.Controls.Add(_canvasSplitContainer);
            canvasAreaPanel.Controls.Add(_toolStrip);

            _mainSplitContainer.Panel1.Controls.Add(leftPanel); 
            _mainSplitContainer.Panel2.Controls.Add(canvasAreaPanel);

            this.Load += (s, e) =>
            {
                _mainSplitContainer.SplitterDistance = 200;
                UpdatePropertiesPaneWidth();
            };

            Controls.Add(_mainSplitContainer);
            this.Size = new System.Drawing.Size(1000, 750); // Increased default size
        }

        // Target width (in pixels) for the docked Element & Gump Properties pane.
        private const int PropertiesPaneWidth = 480;

        private void UpdatePropertiesPaneWidth()
        {
            if (_canvasSplitContainer == null)
            {
                return;
            }

            int totalWidth = _canvasSplitContainer.Width;
            if (totalWidth <= 0)
            {
                return; // not laid out yet; the SizeChanged event will call us again later
            }

            // How wide we want the right (properties) pane to be, but never eat the whole canvas.
            const int minLeftWidth = 200;
            int desiredPropsWidth = PropertiesPaneWidth;
            if (totalWidth - desiredPropsWidth < minLeftWidth)
            {
                desiredPropsWidth = Math.Max(0, totalWidth - minLeftWidth);
            }

            // SplitterDistance is the LEFT pane's width. Clamp it to a legal range.
            int distance = totalWidth - desiredPropsWidth - _canvasSplitContainer.SplitterWidth;
            int min = _canvasSplitContainer.Panel1MinSize;
            int max = totalWidth - _canvasSplitContainer.Panel2MinSize - _canvasSplitContainer.SplitterWidth;

            if (distance < min) distance = min;
            if (distance > max) distance = max;

            if (distance >= min && distance <= max)
            {
                try
                {
                    _canvasSplitContainer.SplitterDistance = distance;
                }
                catch
                {
                    // If the container is momentarily too small, ignore; SizeChanged will retry.
                }
            }
        }

        private void LoadGumpList()
        {
            _allGumpIds.Clear();
            for (int i = 0; i < Gumps.GetCount(); ++i)
            {
                if (Gumps.IsValidIndex(i))
                {
                    _allGumpIds.Add(i);
                }
            }
            _displayedGumpIds = new List<int>(_allGumpIds);
            PopulateListBox();
        }

        private void PopulateListBox()
        {
            _gumpListBox.BeginUpdate();
            _gumpListBox.Items.Clear();
            foreach (var id in _displayedGumpIds)
            {
                _gumpListBox.Items.Add(id);
            }
            _gumpListBox.EndUpdate();
        }

        private void SearchTextBox_TextChanged(object sender, EventArgs e)
        {
            string searchText = _searchTextBox.Text.ToLower().Trim();
            if (string.IsNullOrEmpty(searchText))
            {
                _displayedGumpIds = new List<int>(_allGumpIds);
            }
            else
            {
                _displayedGumpIds = _allGumpIds.Where(id =>
                {
                    string idString = id.ToString();
                    string idHexString = $"0x{id:x}".ToLower();
                    return idString.Contains(searchText) || idHexString.Contains(searchText);
                }).ToList();
            }
            PopulateListBox();
        }

        private void GumpListBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _gumpListBox.Items.Count) return;
            e.DrawBackground();

            int id = (int)_gumpListBox.Items[e.Index];
            bool isSelected = (e.State & DrawItemState.Selected) != 0;
            bool isValidGumpEntry = Gumps.IsValidIndex(id);
            Bitmap gumpImage = null;
            bool isPatched = false;

            if (isValidGumpEntry)
            {
                try
                {
                    gumpImage = Gumps.GetGump(id, out isPatched);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading gump ID {id} for ListBox: {ex.Message}");
                    gumpImage = null;
                    isValidGumpEntry = false;
                }
            }

            // Highlight the 105px-wide thumbnail column, just like the Gumps tab.
            if (isSelected)
            {
                e.Graphics.FillRectangle(Brushes.LightSteelBlue, e.Bounds.X, e.Bounds.Y, 105, e.Bounds.Height);
            }
            else if (isValidGumpEntry && isPatched)
            {
                e.Graphics.FillRectangle(Brushes.LightCoral, e.Bounds.X, e.Bounds.Y, 105, e.Bounds.Height);
            }

            Brush fontBrush = (isValidGumpEntry && gumpImage != null) ? SystemBrushes.ControlText : Brushes.Red;

            if (isValidGumpEntry && gumpImage != null && gumpImage.Width > 0 && gumpImage.Height > 0)
            {
                try
                {
                    // Same proportions as the Gumps tab: draw at NATIVE size,
                    // only capped to width 100 and height (row - 6). No stretching.
                    int thumbMaxH = e.Bounds.Height - 6;
                    int width = gumpImage.Width > 100 ? 100 : gumpImage.Width;
                    int height = gumpImage.Height > thumbMaxH ? thumbMaxH : gumpImage.Height;

                    e.Graphics.DrawImage(gumpImage, new Rectangle(e.Bounds.X + 3, e.Bounds.Y + 3, width, height));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"GumpListBox_DrawItem: Error drawing gump ID {id}: {ex.Message}");
                    fontBrush = Brushes.Red;
                }
            }

            // Text starts right after the 105px thumbnail column (matches the Gumps tab).
            if (isSelected)
            {
                fontBrush = SystemBrushes.HighlightText;
            }

            string txt = $"0x{id:X} ({id})";
            e.Graphics.DrawString(txt, Font, fontBrush, e.Bounds.X + 105, e.Bounds.Y + (e.Bounds.Height - Font.Height) / 2f);
            e.DrawFocusRectangle();
        }

        private void GumpListBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (_gumpListBox.SelectedItem != null && e.Button == MouseButtons.Left)
            {
                _gumpListBox.DoDragDrop(_gumpListBox.SelectedItem, DragDropEffects.Copy);
            }
        }

        private void GumpCanvasPanel_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(int)))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void GumpCanvasPanel_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(int)))
            {
                int gumpId = (int)e.Data.GetData(typeof(int));
                Point clientPoint = _gumpCanvasPanel.PointToClient(new Point(e.X, e.Y));
                Point canvasPoint = GetCanvasPoint(clientPoint);
                var newItem = new CanvasGumpPicItem(gumpId, canvasPoint.X, canvasPoint.Y, _currentPageIndex);
                _canvasItems.Add(newItem);
                SetSelectedItem(newItem);
                _gumpCanvasPanel.Invalidate();
                UpdateCanvasScrollMinSize();
                _gumpCanvasPanel.Focus();
            }
        }

        private void GumpCanvasPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.TranslateTransform(_gumpCanvasPanel.AutoScrollPosition.X, _gumpCanvasPanel.AutoScrollPosition.Y);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            g.Clear(_gumpCanvasPanel.BackColor);
            
            // Items on page 0 (background layer for current page) are drawn first
            var pageZeroItems = _canvasItems.Where(item => item.Page == 0).ToList(); // No specific order needed beyond list itself
            foreach (var item in pageZeroItems)
            {
                item.Draw(g);
            }

            // Then items for the current page (if not page 0)
            if (_currentPageIndex != 0)
            {
                var itemsOnCurrentPage = _canvasItems.Where(item => item.Page == _currentPageIndex).ToList(); // No specific order needed
                foreach (var item in itemsOnCurrentPage)
                {
                    item.Draw(g);
                }
                if (!pageZeroItems.Any() && !itemsOnCurrentPage.Any())
                {
                     TextRenderer.DrawText(g, "Page is empty. Drop Gumps Here or switch page.", Font, _gumpCanvasPanel.ClientRectangle, SystemColors.GrayText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }
            else if (!pageZeroItems.Any()) // Current page IS 0 and it's empty
            {
                 TextRenderer.DrawText(g, "Page 0 is empty. Drop Gumps Here or switch page.", Font, _gumpCanvasPanel.ClientRectangle, SystemColors.GrayText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private void SetSelectedItem(CanvasElement itemToSelect)
        {
            if (_selectedCanvasItem != null) _selectedCanvasItem.Selected = false;
            _selectedCanvasItem = itemToSelect;
            if (_selectedCanvasItem != null)
            {
                _selectedCanvasItem.Selected = true;
                _gumpCanvasPanel.Focus();
            }
            _elementPropertiesWindow?.UpdateSelectedObject(_selectedCanvasItem);
            _gumpCanvasPanel.Invalidate();
            UpdateCanvasScrollMinSize();
        }
        
        private Point GetCanvasPoint(Point clientPoint)
        {
            return new Point(clientPoint.X - _gumpCanvasPanel.AutoScrollPosition.X, clientPoint.Y - _gumpCanvasPanel.AutoScrollPosition.Y);
        }

        private void GumpCanvasPanel_MouseDown(object sender, MouseEventArgs e)
        {
            Point canvasMousePos = GetCanvasPoint(e.Location);
            _gumpCanvasPanel.Focus();
            if (e.Button == MouseButtons.Left)
            {
                _activeResizeHandle = ResizeHandle.None;
                if (_selectedCanvasItem != null)
                {
                    _activeResizeHandle = _selectedCanvasItem.GetResizeHandleAtPoint(canvasMousePos);
                }

                if (_activeResizeHandle != ResizeHandle.None)
                {
                    _resizeStartPoint = canvasMousePos;
                    _originalResizeBounds = _selectedCanvasItem.Bounds;
                    _gumpCanvasPanel.Capture = true;
                }
                else
                {
                    CanvasElement clickedItem = null;
                    for (int i = _canvasItems.Count - 1; i >= 0; i--)
                    {
                        var item = _canvasItems[i];
                        if ((item.Page == _currentPageIndex || item.Page == 0) && item.Contains(canvasMousePos))
                        {
                            clickedItem = item;
                            break;
                        }
                    }
                    SetSelectedItem(clickedItem);
                    if (_selectedCanvasItem != null)
                    {
                        _draggedCanvasItem = _selectedCanvasItem;
                        _dragOffset = new Point(_selectedCanvasItem.X - canvasMousePos.X, _selectedCanvasItem.Y - canvasMousePos.Y);
                    }
                    else _draggedCanvasItem = null;
                }
                _gumpCanvasPanel.Invalidate();
            }
        }

        private void GumpCanvasPanel_MouseMove(object sender, MouseEventArgs e)
        {
            Point canvasMousePos = GetCanvasPoint(e.Location);
            var otherItemsOnPage = _canvasItems.Where(i => i != _selectedCanvasItem && i != _draggedCanvasItem && (i.Page == _currentPageIndex || i.Page == 0)).ToList();

            if (_activeResizeHandle != ResizeHandle.None && _selectedCanvasItem != null && e.Button == MouseButtons.Left)
            {
                int diffX = canvasMousePos.X - _resizeStartPoint.X;
                int diffY = canvasMousePos.Y - _resizeStartPoint.Y;

                int newX = _originalResizeBounds.X;
                int newY = _originalResizeBounds.Y;
                int newWidth = _originalResizeBounds.Width;
                int newHeight = _originalResizeBounds.Height;

                Func<int, List<int>, int, (int, double)> snapCoordinate = (currentCoord, targetCoords, originalCoord) =>
                {
                    int bestSnapPos = currentCoord;
                    double minDelta = SnapThreshold + 1.0;
                    foreach (int targetLine in targetCoords)
                    {
                        double delta = Math.Abs(currentCoord - targetLine);
                        if (delta < SnapThreshold && delta < minDelta)
                        {
                            minDelta = delta;
                            bestSnapPos = targetLine;
                        }
                    }
                    return (bestSnapPos, minDelta);
                };

                List<int> xTargets = new List<int>();
                List<int> yTargets = new List<int>();
                foreach (var other in otherItemsOnPage)
                {
                    xTargets.Add(other.Bounds.Left);
                    xTargets.Add(other.Bounds.Left + other.Bounds.Width / 2);
                    xTargets.Add(other.Bounds.Right);
                    yTargets.Add(other.Bounds.Top);
                    yTargets.Add(other.Bounds.Top + other.Bounds.Height / 2);
                    yTargets.Add(other.Bounds.Bottom);
                }
                xTargets = xTargets.Distinct().ToList();
                yTargets = yTargets.Distinct().ToList();

                int mouseProposedX = _originalResizeBounds.X + diffX;
                int mouseProposedY = _originalResizeBounds.Y + diffY;
                int mouseProposedWidth = _originalResizeBounds.Width + diffX;
                int mouseProposedHeight = _originalResizeBounds.Height + diffY;
                int mouseProposedWidthFromLeft = _originalResizeBounds.Width - diffX;
                int mouseProposedHeightFromTop = _originalResizeBounds.Height - diffY;

                switch (_activeResizeHandle)
                {
                    case ResizeHandle.TopLeft:
                        var snapX_TL = snapCoordinate(mouseProposedX, xTargets, _originalResizeBounds.Left);
                        var snapY_TL = snapCoordinate(mouseProposedY, yTargets, _originalResizeBounds.Top);
                        newX = snapX_TL.Item1;
                        newY = snapY_TL.Item1;
                        newWidth = _originalResizeBounds.Right - newX;
                        newHeight = _originalResizeBounds.Bottom - newY;
                        break;
                    case ResizeHandle.TopMiddle:
                        var snapY_TM = snapCoordinate(mouseProposedY, yTargets, _originalResizeBounds.Top);
                        newY = snapY_TM.Item1;
                        newHeight = _originalResizeBounds.Bottom - newY;
                        break;
                    case ResizeHandle.TopRight:
                        var snapXR_TR = snapCoordinate(_originalResizeBounds.X + mouseProposedWidthFromLeft, xTargets, _originalResizeBounds.Right);
                        var snapY_TR = snapCoordinate(mouseProposedY, yTargets, _originalResizeBounds.Top);
                        newY = snapY_TR.Item1;
                        newWidth = snapXR_TR.Item1 - _originalResizeBounds.X;
                        newHeight = _originalResizeBounds.Bottom - newY;
                        break;
                    case ResizeHandle.MiddleLeft:
                        var snapX_ML = snapCoordinate(mouseProposedX, xTargets, _originalResizeBounds.Left);
                        newX = snapX_ML.Item1;
                        newWidth = _originalResizeBounds.Right - newX;
                        break;
                    case ResizeHandle.MiddleRight:
                        var snapXR_MR = snapCoordinate(_originalResizeBounds.X + mouseProposedWidth, xTargets, _originalResizeBounds.Right);
                        newWidth = snapXR_MR.Item1 - _originalResizeBounds.X;
                        break;
                    case ResizeHandle.BottomLeft:
                        var snapX_BL = snapCoordinate(mouseProposedX, xTargets, _originalResizeBounds.Left);
                        var snapYB_BL = snapCoordinate(_originalResizeBounds.Y + mouseProposedHeightFromTop, yTargets, _originalResizeBounds.Bottom);
                        newX = snapX_BL.Item1;
                        newWidth = _originalResizeBounds.Right - newX;
                        newHeight = snapYB_BL.Item1 - _originalResizeBounds.Y;
                        break;
                    case ResizeHandle.BottomMiddle:
                        var snapYB_BM = snapCoordinate(_originalResizeBounds.Y + mouseProposedHeight, yTargets, _originalResizeBounds.Bottom);
                        newHeight = snapYB_BM.Item1 - _originalResizeBounds.Y;
                        break;
                    case ResizeHandle.BottomRight:
                        var snapXR_BR = snapCoordinate(_originalResizeBounds.X + mouseProposedWidth, xTargets, _originalResizeBounds.Right);
                        var snapYB_BR = snapCoordinate(_originalResizeBounds.Y + mouseProposedHeight, yTargets, _originalResizeBounds.Bottom);
                        newWidth = snapXR_BR.Item1 - _originalResizeBounds.X;
                        newHeight = snapYB_BR.Item1 - _originalResizeBounds.Y;
                        break;
                }
                _selectedCanvasItem.X = newX;
                _selectedCanvasItem.Y = newY;
                _selectedCanvasItem.Width = Math.Max(16, newWidth); 
                _selectedCanvasItem.Height = Math.Max(16, newHeight);
                _gumpCanvasPanel.Invalidate();
                UpdateCanvasScrollMinSize();
            }
            else if (_draggedCanvasItem != null && e.Button == MouseButtons.Left)
            {
                int targetX = canvasMousePos.X + _dragOffset.X;
                int targetY = canvasMousePos.Y + _dragOffset.Y;

                int bestSnapX = targetX;
                int bestSnapY = targetY;
                double minXDeltaFromTarget = SnapThreshold;
                double minYDeltaFromTarget = SnapThreshold;

                var currentItemRectProposed = new Rectangle(targetX, targetY, _draggedCanvasItem.Width, _draggedCanvasItem.Height);

                foreach (var other in otherItemsOnPage)
                {
                    int[] sourceXLines = { currentItemRectProposed.Left, currentItemRectProposed.Left + currentItemRectProposed.Width / 2, currentItemRectProposed.Right };
                    int[] targetOtherXLines = { other.Bounds.Left, other.Bounds.Left + other.Bounds.Width / 2, other.Bounds.Right };

                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            double delta = Math.Abs(sourceXLines[i] - targetOtherXLines[j]);
                            if (delta < SnapThreshold)
                            {
                                int correction = targetOtherXLines[j] - sourceXLines[i];
                                int snappedX = targetX + correction;
                                double deviation = Math.Abs(snappedX - targetX); 

                                if (deviation < minXDeltaFromTarget)
                                {
                                    minXDeltaFromTarget = deviation;
                                    bestSnapX = snappedX;
                                }
                            }
                        }
                    }
                    
                    int[] sourceYLines = { currentItemRectProposed.Top, currentItemRectProposed.Top + currentItemRectProposed.Height / 2, currentItemRectProposed.Bottom };
                    int[] targetOtherYLines = { other.Bounds.Top, other.Bounds.Top + other.Bounds.Height / 2, other.Bounds.Bottom };

                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            double delta = Math.Abs(sourceYLines[i] - targetOtherYLines[j]);
                            if (delta < SnapThreshold)
                            {
                                int correction = targetOtherYLines[j] - sourceYLines[i];
                                int snappedY = targetY + correction;
                                double deviation = Math.Abs(snappedY - targetY);

                                if (deviation < minYDeltaFromTarget)
                                {
                                    minYDeltaFromTarget = deviation;
                                    bestSnapY = snappedY;
                                }
                            }
                        }
                    }
                }

                _draggedCanvasItem.X = bestSnapX;
                _draggedCanvasItem.Y = bestSnapY;
                _gumpCanvasPanel.Invalidate();
                UpdateCanvasScrollMinSize();
            }
            else if (_selectedCanvasItem != null)
            {
                 ResizeHandle handleUnderCursor = _selectedCanvasItem.GetResizeHandleAtPoint(canvasMousePos);
                 SetResizeCursor(handleUnderCursor);
            }
            else
            {
                Cursor = Cursors.Default;
            }
        }
        
        private void SetResizeCursor(ResizeHandle handle)
        {
            switch (handle)
            {
                case ResizeHandle.TopLeft: case ResizeHandle.BottomRight: Cursor = Cursors.SizeNWSE; break;
                case ResizeHandle.TopRight: case ResizeHandle.BottomLeft: Cursor = Cursors.SizeNESW; break;
                case ResizeHandle.TopMiddle: case ResizeHandle.BottomMiddle: Cursor = Cursors.SizeNS; break;
                case ResizeHandle.MiddleLeft: case ResizeHandle.MiddleRight: Cursor = Cursors.SizeWE; break;
                default: Cursor = Cursors.Default; break;
            }
        }

        private void GumpCanvasPanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _draggedCanvasItem = null;
                _activeResizeHandle = ResizeHandle.None;
                _gumpCanvasPanel.Capture = false;
                _gumpCanvasPanel.Invalidate();
                 UpdateCanvasScrollMinSize();
            }
        }

        private void UpdatePageDisplay()
        {
            _currentPageLabel.Text = $"Page: {_currentPageIndex}/{_maxPageIndex}";
            _prevPageButton.Enabled = _currentPageIndex > 0;
            _nextPageButton.Enabled = _currentPageIndex < _maxPageIndex;
            _gumpCanvasPanel.Invalidate();
            UpdateCanvasScrollMinSize();
        }

        private void ChangePage(int delta)
        {
            int newPage = _currentPageIndex + delta;
            if (newPage >= 0 && newPage <= _maxPageIndex)
            {
                _currentPageIndex = newPage;
                UpdatePageDisplay();
            }
        }

        private void AddPageButton_Click(object sender, EventArgs e)
        {
            _maxPageIndex++;
            _currentPageIndex = _maxPageIndex;
            UpdatePageDisplay();
        }

        private void AddGumpPicButton_Click(object sender, EventArgs e)
        {
            int id = 0;
            if (_gumpListBox.SelectedIndex != -1)
            {
                id = (int)_gumpListBox.SelectedItem;
            }
            else if (_allGumpIds.Any())
            {
                id = _allGumpIds.First();
            }

            var newItem = new CanvasGumpPicItem(id, 10 + _gumpCanvasPanel.AutoScrollPosition.X, 10 + _gumpCanvasPanel.AutoScrollPosition.Y, _currentPageIndex);
            _canvasItems.Add(newItem);
            SetSelectedItem(newItem);
        }

        private void AddTextButton_Click(object sender, EventArgs e)
        {
            var newItem = new CanvasTextItem(10 + _gumpCanvasPanel.AutoScrollPosition.X, 10 + _gumpCanvasPanel.AutoScrollPosition.Y, _currentPageIndex, "New Text");
            _canvasItems.Add(newItem);
            SetSelectedItem(newItem);
        }

        private void AddButtonButton_Click(object sender, EventArgs e)
        {
            var newItem = new CanvasButtonItem(10 + _gumpCanvasPanel.AutoScrollPosition.X, 10 + _gumpCanvasPanel.AutoScrollPosition.Y, _currentPageIndex);
            _canvasItems.Add(newItem);
            SetSelectedItem(newItem);
        }

        private void AddCheckboxButton_Click(object sender, EventArgs e)
        {
            var newItem = new CanvasCheckboxItem(10 + _gumpCanvasPanel.AutoScrollPosition.X, 10 + _gumpCanvasPanel.AutoScrollPosition.Y, _currentPageIndex, 210, 211, false, 1);
            _canvasItems.Add(newItem);
            SetSelectedItem(newItem);
        }

        private void AddRadioButton_Click(object sender, EventArgs e)
        {
            var newItem = new CanvasRadioButtonItem(10 + _gumpCanvasPanel.AutoScrollPosition.X, 10 + _gumpCanvasPanel.AutoScrollPosition.Y, _currentPageIndex, 208, 209, false, 1, 1);
            _canvasItems.Add(newItem);
            SetSelectedItem(newItem);
        }

        private void AddTextEntryButton_Click(object sender, EventArgs e)
        {
            var newItem = new CanvasTextEntryItem(10 + _gumpCanvasPanel.AutoScrollPosition.X, 10 + _gumpCanvasPanel.AutoScrollPosition.Y, _currentPageIndex, 150, 20, "", 0, 1, 0);
            _canvasItems.Add(newItem);
            SetSelectedItem(newItem);
        }

        private void PropertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            if (_selectedCanvasItem != null)
            {
                if (_selectedCanvasItem is CanvasTextItem textItem && e.ChangedItem.PropertyDescriptor.Name == nameof(CanvasTextItem.Text))
                {
                    textItem.RecalculateBounds();
                }
                if (_selectedCanvasItem is CanvasCheckboxItem checkboxItem && 
                    (e.ChangedItem.PropertyDescriptor.Name == nameof(CanvasCheckboxItem.CheckedId) || 
                     e.ChangedItem.PropertyDescriptor.Name == nameof(CanvasCheckboxItem.UncheckedId) ||
                     e.ChangedItem.PropertyDescriptor.Name == nameof(CanvasCheckboxItem.InitialStatus)))
                {
                    checkboxItem.RecalculateBoundsBasedOnGump(); 
                }
                if (_selectedCanvasItem is CanvasRadioButtonItem radioItem && 
                    (e.ChangedItem.PropertyDescriptor.Name == nameof(CanvasRadioButtonItem.PressedId) || 
                     e.ChangedItem.PropertyDescriptor.Name == nameof(CanvasRadioButtonItem.UnpressedId) ||
                     e.ChangedItem.PropertyDescriptor.Name == nameof(CanvasRadioButtonItem.InitialStatus)))
                {
                    radioItem.RecalculateBoundsBasedOnGump();
                }

                _gumpCanvasPanel.Invalidate();
                UpdateCanvasScrollMinSize();
            }
        }

        private void UpdateCanvasScrollMinSize()
        {
            int maxX = 0;
            int maxY = 0;
            foreach (var item in _canvasItems.Where(i => i.Page == _currentPageIndex || i.Page == 0))
            {
                maxX = Math.Max(maxX, item.X + item.Width);
                maxY = Math.Max(maxY, item.Y + item.Height);
            }
            
            _gumpCanvasPanel.AutoScrollMinSize = new Size(Math.Max(_gumpCanvasPanel.ClientSize.Width, maxX + 20), Math.Max(_gumpCanvasPanel.ClientSize.Height, maxY + 20));
        }

        private void GumpCanvasPanel_KeyDown(object sender, KeyEventArgs e)
        {
            bool itemSelected = _selectedCanvasItem != null;
            bool altPressed = e.Alt;
            bool ctrlPressed = e.Control;
            bool shiftPressed = e.Shift; // Retained for potential future use or if other shortcuts use it

            if (itemSelected)
            {
                if (ctrlPressed && !altPressed && e.KeyCode == Keys.C) // Copy
                {
                    _copiedElement = CloneElement(_selectedCanvasItem);
                    ResetAltMoveState(); // Reset movement state on other actions
                    e.Handled = true;
                }
                else if (ctrlPressed && !altPressed && e.KeyCode == Keys.V) // Paste
                {
                    if (_copiedElement != null)
                    {
                        CanvasElement pastedElement = CloneElement(_copiedElement); 
                        if (pastedElement != null)
                        {
                            pastedElement.X += 10;
                            pastedElement.Y += 10;
                            pastedElement.Page = _currentPageIndex; 
                            
                            _canvasItems.Add(pastedElement);
                            SetSelectedItem(pastedElement);
                            _gumpCanvasPanel.Invalidate();
                            UpdateCanvasScrollMinSize();
                        }
                    }
                    ResetAltMoveState();
                    e.Handled = true;
                }
                else if (ctrlPressed && !altPressed && (_selectedCanvasItem is CanvasGumpPicItem selectedGumpPic)) // Ctrl + Arrows for Gump ID
                {
                    int gumpIdChange = 0;
                    switch (e.KeyCode)
                    {
                        case Keys.Up:   gumpIdChange = -CtrlArrowKeyStepGumpId; break;
                        case Keys.Down: gumpIdChange = CtrlArrowKeyStepGumpId; break;
                    }

                    if (gumpIdChange != 0)
                    {
                        int newGumpId = selectedGumpPic.GumpId + gumpIdChange;
                        if (newGumpId < 0) newGumpId = 0; 
                        selectedGumpPic.GumpId = newGumpId; 
                        _elementPropertiesWindow?.UpdateSelectedObject(_selectedCanvasItem); 
                        _gumpCanvasPanel.Invalidate();
                        ResetAltMoveState();
                        e.Handled = true;
                    }
                    else { ResetAltMoveState(); } // If Ctrl was pressed with an arrow but not Up/Down
                }
                else if (altPressed && !ctrlPressed) // Alt + Arrow keys for movement
                {
                    int currentAltStep = BaseAltMoveStep;
                    if (e.KeyCode == _lastAltMoveKey)
                    {
                        _altMoveConsecutiveCount++;
                    }
                    else if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right || e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)
                    {
                        _lastAltMoveKey = e.KeyCode;
                        _altMoveConsecutiveCount = 1;
                    }
                    else // Not an arrow key, or Alt released (handled by general reset below)
                    {
                        ResetAltMoveState();
                    }

                    if (_altMoveConsecutiveCount >= AccelerationStartCount)
                    {
                        currentAltStep += AccelerationIncrement * (_altMoveConsecutiveCount - AccelerationStartCount + 1);
                        currentAltStep = Math.Min(currentAltStep, MaxAltMoveStep);
                    }
                    
                    bool moved = false;
                    switch (e.KeyCode)
                    {
                        case Keys.Left:  _selectedCanvasItem.X -= currentAltStep; moved = true; break;
                        case Keys.Right: _selectedCanvasItem.X += currentAltStep; moved = true; break;
                        case Keys.Up:    _selectedCanvasItem.Y -= currentAltStep; moved = true; break;
                        case Keys.Down:  _selectedCanvasItem.Y += currentAltStep; moved = true; break;
                        default: ResetAltMoveState(); break; // If Alt is held but not with an arrow key
                    }

                    if (moved)
                    {
                        _gumpCanvasPanel.Invalidate();
                        UpdateCanvasScrollMinSize(); 
                        _elementPropertiesWindow?.UpdateSelectedObject(_selectedCanvasItem); 
                        e.Handled = true;
                    }
                }
                else // No specific Ctrl/Alt combo handled for selected item, or modifier released
                {
                    ResetAltMoveState();
                }
            }
            else // No item selected
            {
                ResetAltMoveState();
            }

            if (e.KeyCode == Keys.Delete && itemSelected) // Delete key
            {
                CanvasElement itemToRemove = _selectedCanvasItem;
                SetSelectedItem(null); 
                _canvasItems.Remove(itemToRemove);
                itemToRemove.DisposeResources(); 
                if (_copiedElement == itemToRemove) 
                {
                    _copiedElement = null; 
                }
                _gumpCanvasPanel.Invalidate();
                UpdateCanvasScrollMinSize();
                ResetAltMoveState();
                e.Handled = true;
            }
        }
        
        private void ResetAltMoveState()
        {
            _lastAltMoveKey = Keys.None;
            _altMoveConsecutiveCount = 0;
        }

        private CanvasElement CloneElement(CanvasElement original)
        {
            if (original == null) return null;

            // Create a new instance based on the type of the original element
            // This is a shallow copy of properties; deeper cloning might be needed for complex states
            CanvasElement clone = null;
            int x = original.X;
            int y = original.Y;
            int w = original.Width;
            int h = original.Height;
            int page = original.Page; // Consider if pasted item should always be on current page or retain original

            if (original is CanvasGumpPicItem gPic)
            {
                clone = new CanvasGumpPicItem(gPic.GumpId, x, y, page) { Hue = gPic.Hue, Width = w, Height = h };
            }
            else if (original is CanvasTextItem tItem)
            {
                clone = new CanvasTextItem(x, y, page, tItem.Text) { TextColorHue = tItem.TextColorHue, Width = w, Height = h };
                // Width/Height for TextItem are auto-calculated, but we can set them if they were explicitly resized
                ((CanvasTextItem)clone).Width = w; 
                ((CanvasTextItem)clone).Height = h;
            }
            else if (original is CanvasButtonItem bItem)
            {
                clone = new CanvasButtonItem(x, y, page) 
                {
                    ReleasedGumpId = bItem.ReleasedGumpId, 
                    PressedGumpId = bItem.PressedGumpId, 
                    ReturnValue = bItem.ReturnValue, 
                    TargetPageId = bItem.TargetPageId,
                    Width = w, Height = h
                };
            }
            else if (original is CanvasCheckboxItem chkBox)
            {
                clone = new CanvasCheckboxItem(x, y, page, chkBox.UncheckedId, chkBox.CheckedId, chkBox.InitialStatus, chkBox.ButtonValue) { Width = w, Height = h };
            }
            else if (original is CanvasRadioButtonItem rdoBtn)
            {
                clone = new CanvasRadioButtonItem(x, y, page, rdoBtn.UnpressedId, rdoBtn.PressedId, rdoBtn.InitialStatus, rdoBtn.ButtonValue, rdoBtn.GroupId) { Width = w, Height = h };
            }
            else if (original is CanvasTextEntryItem teItem)
            {
                clone = new CanvasTextEntryItem(x, y, page, w, h, teItem.InitialText, teItem.TextColorHue, teItem.TextId, teItem.CharacterLimit);
            }
            // Add other element types here if needed

            if (clone != null)
            {
                // Copy common properties not set in constructor, if any (most are covered by constructors now)
                // clone.Page = page; // Already passed to constructor for most
            }
            return clone;
        }

        private void CanvasContextMenu_Opening(object sender, CancelEventArgs e)
        {
            bool itemSelected = _selectedCanvasItem != null;
            _bringToFrontMenuItem.Enabled = itemSelected;
            _sendToBackMenuItem.Enabled = itemSelected;
            _bringForwardMenuItem.Enabled = itemSelected;
            _sendBackwardMenuItem.Enabled = itemSelected;

            // Prevent context menu if no item is selected and right-click is not on an item
            // This logic can be more sophisticated if needed, e.g., checking if mouse is over an item.
            // For now, just enabling/disabling based on _selectedCanvasItem is a start.
            // If you want the menu to not show at all when no item is selected:
            // if (!itemSelected) e.Cancel = true;
        }

        private void ReorderItem(CanvasElement item, int newIndexOffset, bool toEnd, bool toStart)
        {
            if (item == null) return;

            bool itemIsOnPageZero = item.Page == 0;
            List<CanvasElement> currentPageAndGlobalItems = _canvasItems
                .Where(i => i.Page == item.Page || (item.Page != 0 && i.Page == 0) || (itemIsOnPageZero && i.Page == 0))
                .ToList();
            List<CanvasElement> otherPagesItems = _canvasItems
                .Except(currentPageAndGlobalItems)
                .ToList();
            
            List<CanvasElement> actualPageZeroItems = new List<CanvasElement>();
            List<CanvasElement> actualCurrentPageItems = new List<CanvasElement>();

            if (itemIsOnPageZero) 
            {
                actualPageZeroItems = currentPageAndGlobalItems;
            }
            else 
            {
                actualPageZeroItems = currentPageAndGlobalItems.Where(i => i.Page == 0).ToList();
                actualCurrentPageItems = currentPageAndGlobalItems.Where(i => i.Page == item.Page).ToList();
            }

            List<CanvasElement> listToReorder = itemIsOnPageZero ? actualPageZeroItems : actualCurrentPageItems;

            if (!listToReorder.Contains(item)) return; 

            int currentItemIndexInSubList = listToReorder.IndexOf(item);
            listToReorder.RemoveAt(currentItemIndexInSubList);

            if (toEnd)
            {
                listToReorder.Add(item);
            }
            else if (toStart)
            {
                listToReorder.Insert(0, item);
            }
            else
            {
                int newSubIndex = Math.Max(0, Math.Min(listToReorder.Count, currentItemIndexInSubList + newIndexOffset));
                listToReorder.Insert(newSubIndex, item);
            }

            _canvasItems.Clear();
            _canvasItems.AddRange(otherPagesItems);
            if (!itemIsOnPageZero) _canvasItems.AddRange(actualPageZeroItems); 
            _canvasItems.AddRange(listToReorder); 
            
            _gumpCanvasPanel.Invalidate();
        }

        private void BringToFront_Click(object sender, EventArgs e)
        {
            ReorderItem(_selectedCanvasItem, 0, true, false);
        }

        private void SendToBack_Click(object sender, EventArgs e)
        {
            ReorderItem(_selectedCanvasItem, 0, false, true);
        }

        private void BringForward_Click(object sender, EventArgs e)
        {
            ReorderItem(_selectedCanvasItem, 1, false, false);
        }

        private void SendBackward_Click(object sender, EventArgs e)
        {
            ReorderItem(_selectedCanvasItem, -1, false, false);
        }

        public List<CanvasElement> GetCurrentCanvasItems()
        {
            return _canvasItems;
        }

        public int GetCurrentMaxPageIndex()
        {
            return _maxPageIndex;
        }

        public bool IsGumpClosable => _elementPropertiesWindow?.IsGumpClosable ?? false;
        public bool IsGumpMovable => _elementPropertiesWindow?.IsGumpMovable ?? false;
        public bool IsGumpDisposable => _elementPropertiesWindow?.IsGumpDisposable ?? false;

        public void ForceClosePropertiesWindow()
        {
            if (_elementPropertiesWindow != null)
            {
                _elementPropertiesWindow.PrepareForHardClose(); 
                // Attempt to close the window. If it's already closing or disposed, this might not do much or could error.
                // However, PrepareForHardClose ensures its FormClosing won't just hide it.
                try
                {
                    if (_elementPropertiesWindow.IsHandleCreated && !_elementPropertiesWindow.IsDisposed) // Check if it can be closed
                    {
                         _elementPropertiesWindow.Close(); 
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Ignore if already disposed, which is fine.
                }
                catch (InvalidOperationException)
                {
                    // Ignore if handle not created or other similar issues preventing Close().
                }

                // Ensure Dispose is called regardless of successful Close().
                if (!_elementPropertiesWindow.IsDisposed)
                {
                    _elementPropertiesWindow.Dispose();
                }
                _elementPropertiesWindow = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Restore main form size one last time if needed when GumpCreator itself is disposed
                if (_mainFormInstance != null && _mainFormResizedByMe)
                {
                     if (_mainFormInstance.WindowState == FormWindowState.Normal)
                     {
                        _mainFormInstance.Size = _originalMainFormSize;
                     }
                    _mainFormResizedByMe = false; // Clear flag
                }

                if (_mainFormInstance != null)
                {
                    _mainFormInstance = null;
                }
                this.HandleCreated -= GumpCreatorControl_HandleCreated;
                this.VisibleChanged -= GumpCreatorControl_VisibleChanged;

                ForceClosePropertiesWindow(); 
                
                _canvasContextMenu?.Dispose();

                foreach (var item in _canvasItems) item.DisposeResources();
                _canvasItems.Clear();
                _toolStrip?.Dispose();
                _prevPageButton?.Dispose();
                _nextPageButton?.Dispose();
                _addPageButton?.Dispose();
                _currentPageLabel?.Dispose();
                _addTextButton?.Dispose();
                _addGumpPicButton?.Dispose();
                _addButtonButton?.Dispose();
                _addCheckboxButton?.Dispose(); 
                _addRadioButton?.Dispose();    
                _addTextEntryButton?.Dispose(); 
            }
            base.Dispose(disposing);
        }

        public void LoadGumpFromData(GumpSaveData loadedData)
        {
            if (loadedData == null || loadedData.Elements == null)
            {
                MessageBox.Show("Loaded data is invalid.", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            foreach (var item in _canvasItems) { item.DisposeResources(); }
            _canvasItems.Clear();
            _selectedCanvasItem = null;
            _copiedElement = null;
            _currentPageIndex = 1; 
            _maxPageIndex = loadedData.MaxPageIndex > 0 ? loadedData.MaxPageIndex : 1;
            _elementPropertiesWindow.SetGumpSettings(loadedData.IsClosable, loadedData.IsMovable, loadedData.IsDisposable);

            JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var elementAsObject in loadedData.Elements) // elementAsObject is initially a JsonElement
            {
                if (!(elementAsObject is JsonElement elJsonElement))
                {
                    Console.WriteLine("Skipping element as it's not a JsonElement after initial deserialize.");
                    continue;
                }

                CanvasElement newElement = null;
                if (!elJsonElement.TryGetProperty("ElementType", out JsonElement typeElement) || 
                    !typeElement.TryGetInt32(out int elementTypeInt))
                {
                    Console.WriteLine("Skipping element due to missing or invalid ElementType property.");
                    continue;
                }
                CanvasElementType elementType = (CanvasElementType)elementTypeInt;
                string elementJson = elJsonElement.GetRawText(); // Get raw JSON for this specific element

                switch (elementType)
                {
                    case CanvasElementType.GumpPic:
                        var gpData = JsonSerializer.Deserialize<CanvasGumpPicItemSaveData>(elementJson, options);
                        if (gpData != null) newElement = new CanvasGumpPicItem(gpData.GumpId, gpData.X, gpData.Y, gpData.Page) { Hue = gpData.Hue };
                        break;
                    case CanvasElementType.Text:
                        if (elJsonElement.TryGetProperty("InitialText", out _))
                        {
                            var teData = JsonSerializer.Deserialize<CanvasTextEntryItemSaveData>(elementJson, options);
                            if (teData != null) newElement = new CanvasTextEntryItem(teData.X, teData.Y, teData.Page, teData.Width, teData.Height, teData.InitialText, teData.TextColorHue, teData.TextId, teData.CharacterLimit);
                        }
                        else
                        {
                            var tData = JsonSerializer.Deserialize<CanvasTextItemSaveData>(elementJson, options);
                            if (tData != null) newElement = new CanvasTextItem(tData.X, tData.Y, tData.Page, tData.Text) { TextColorHue = tData.TextColorHue };
                        }
                        break;
                    case CanvasElementType.Button:
                        var bData = JsonSerializer.Deserialize<CanvasButtonItemSaveData>(elementJson, options);
                        if (bData != null) newElement = new CanvasButtonItem(bData.X, bData.Y, bData.Page) 
                            { 
                                ReleasedGumpId = bData.ReleasedGumpId, PressedGumpId = bData.PressedGumpId, 
                                ReturnValue = bData.ReturnValue, TargetPageId = bData.TargetPageId
                            };
                        break;
                    case CanvasElementType.Checkbox:
                        var chkData = JsonSerializer.Deserialize<CanvasCheckboxItemSaveData>(elementJson, options);
                        if (chkData != null) newElement = new CanvasCheckboxItem(chkData.X, chkData.Y, chkData.Page, chkData.UncheckedId, chkData.CheckedId, chkData.InitialStatus, chkData.ButtonValue);
                        break;
                    case CanvasElementType.RadioButton:
                        var rdoData = JsonSerializer.Deserialize<CanvasRadioButtonItemSaveData>(elementJson, options);
                        if (rdoData != null) newElement = new CanvasRadioButtonItem(rdoData.X, rdoData.Y, rdoData.Page, rdoData.UnpressedId, rdoData.PressedId, rdoData.InitialStatus, rdoData.ButtonValue, rdoData.GroupId);
                        break;
                    default:
                        Console.WriteLine($"Unknown ElementType ({elementType}) encountered during load.");
                        break;
                }

                if (newElement != null)
                {
                    // Explicitly set Width and Height from the DTO after construction
                    if (elJsonElement.TryGetProperty("Width", out JsonElement wElem) && wElem.TryGetInt32(out int w))
                        newElement.Width = (w > 0) ? w : newElement.Width;
                    if (elJsonElement.TryGetProperty("Height", out JsonElement hElem) && hElem.TryGetInt32(out int h))
                        newElement.Height = (h > 0) ? h : newElement.Height;
                    if (elJsonElement.TryGetProperty("Z", out JsonElement zElem) && zElem.TryGetInt32(out int zValue)) // Load Z
                        newElement.Z = zValue;
                    
                    _canvasItems.Add(newElement);
                }
            }

            SetSelectedItem(null); 
            UpdatePageDisplay();   
            _gumpCanvasPanel.Invalidate(); 
            UpdateCanvasScrollMinSize(); 
        }
    }
} 