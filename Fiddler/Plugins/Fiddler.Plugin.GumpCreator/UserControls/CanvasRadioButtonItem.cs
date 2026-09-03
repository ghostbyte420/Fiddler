using System;
using System.ComponentModel;
using System.Drawing;
using Ultima;
using System.Windows.Forms; // For TextRenderer, SystemFonts

namespace Fiddler.Plugin.GumpCreator.UserControls
{
    public class CanvasRadioButtonItem : CanvasElement
    {
        public override CanvasElementType ElementType => CanvasElementType.RadioButton;

        private int _unpressedId;
        private int _pressedId;
        private bool _initialStatus; // true for pressed, false for unpressed
        private int _buttonValue;
        private int _groupId; // For GFSetRadioGroup

        [Category("Gump Specific")]
        [Description("The Gump ID for the unpressed state of the radio button.")]
        public int UnpressedId
        {
            get => _unpressedId;
            set { _unpressedId = value; RecalculateBoundsBasedOnGump(); }
        }

        [Category("Gump Specific")]
        [Description("The Gump ID for the pressed state of the radio button.")]
        public int PressedId
        {
            get => _pressedId;
            set { _pressedId = value; RecalculateBoundsBasedOnGump(); }
        }

        [Category("Gump Specific")]
        [Description("The initial state of the radio button (Pressed = true, Unpressed = false).")]
        public bool InitialStatus
        {
            get => _initialStatus;
            set { _initialStatus = value; }
        }

        [Category("Gump Specific")]
        [Description("The value associated with this radio button, returned when the gump is submitted.")]
        public int ButtonValue
        {
            get => _buttonValue;
            set { _buttonValue = value; }
        }

        [Category("Gump Specific")]
        [Description("The group ID this radio button belongs to. Radio buttons in the same group are mutually exclusive.")]
        public int GroupId
        {
            get => _groupId;
            set { _groupId = value; }
        }

        public CanvasRadioButtonItem(int x, int y, int page, int unpressedId = 208, int pressedId = 209, bool initialStatus = false, int buttonValue = 0, int groupId = 1)
            : base(x, y, page)
        {
            _unpressedId = unpressedId;
            _pressedId = pressedId;
            _initialStatus = initialStatus;
            _buttonValue = buttonValue;
            _groupId = groupId;
            RecalculateBoundsBasedOnGump();
        }

        public void RecalculateBoundsBasedOnGump()
        {
            int gumpIdToUse = _initialStatus ? _pressedId : _unpressedId;
            if (gumpIdToUse <= 0) gumpIdToUse = _unpressedId > 0 ? _unpressedId : _pressedId;
            if (gumpIdToUse <= 0) gumpIdToUse = 208;

            Bitmap gump = null;
            try
            {
                if (Gumps.IsValidIndex(gumpIdToUse))
                {
                    gump = Gumps.GetGump(gumpIdToUse);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading gump ID {gumpIdToUse} in RecalculateBounds (RadioButton): {ex.Message}");
                gump = null;
            }

            if (gump != null && gump.Width > 0 && gump.Height > 0)
            {
                Width = gump.Width;
                Height = gump.Height;
            }
            else
            {
                Width = 14;
                Height = 14;
            }

            OriginalWidth = Width;
            OriginalHeight = Height;
        }

        public override void Draw(Graphics g)
        {
            if (g == null) return;

            int gumpIdToDraw = _initialStatus ? _pressedId : _unpressedId;
            if (gumpIdToDraw <= 0) gumpIdToDraw = _unpressedId > 0 ? _unpressedId : _pressedId;
            if (gumpIdToDraw <= 0) gumpIdToDraw = (_initialStatus ? 209 : 208); // Fallback to default IDs
            
            Bitmap imageToDraw = null;
            try 
            { 
                if (Gumps.IsValidIndex(gumpIdToDraw))
                {
                    imageToDraw = Gumps.GetGump(gumpIdToDraw);
                }
            }
            catch (Exception ex) 
            {
                System.Diagnostics.Debug.WriteLine($"Error loading gump ID {gumpIdToDraw} in Draw (RadioButton): {ex.Message}");
                imageToDraw = null;
            }

            if (imageToDraw != null && imageToDraw.Width > 0 && imageToDraw.Height > 0)
            {
                try
                {
                    g.DrawImage(imageToDraw, X, Y, this.Width, this.Height);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error drawing radiobutton Gump ID {gumpIdToDraw}: {ex.Message}");
                    DrawPlaceholder(g);
                }
            }
            else
            {
                DrawPlaceholder(g);
            }

            if (Selected)
            {
                DrawSelection(g);
            }
        }

        private void DrawPlaceholder(Graphics g)
        {
            using (var pen = new Pen(Color.DarkGray, 1))
            using (var brush = new SolidBrush(Color.LightGray))
            {
                g.FillEllipse(brush, X, Y, this.Width, this.Height);
                g.DrawEllipse(pen, X, Y, this.Width - 1 , this.Height - 1);
                if (_initialStatus) 
                {
                    using (var innerBrush = new SolidBrush(Color.DarkGray))
                    {
                        g.FillEllipse(innerBrush, X + this.Width / 4, Y + this.Height / 4, this.Width / 2, this.Height / 2);
                    }
                }
            }
        }

        public override void DisposeResources()
        {
            // No owned bitmap resources to dispose.
        }
    }
} 