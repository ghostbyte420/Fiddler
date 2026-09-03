using System;
using System.ComponentModel;
using System.Drawing;
using Ultima;
using System.Windows.Forms; // For TextRenderer, SystemFonts

namespace Fiddler.Plugin.GumpCreator.UserControls
{
    public class CanvasCheckboxItem : CanvasElement
    {
        public override CanvasElementType ElementType => CanvasElementType.Checkbox;

        private int _uncheckedId;
        private int _checkedId;
        private bool _initialStatus; // true for checked, false for unchecked
        private int _buttonValue; // Corresponds to btn_value in GFCheckBox

        [Category("Gump Specific")]
        [Description("The Gump ID for the unchecked state of the checkbox.")]
        public int UncheckedId
        {
            get => _uncheckedId;
            set { _uncheckedId = value; RecalculateBoundsBasedOnGump(); }
        }

        [Category("Gump Specific")]
        [Description("The Gump ID for the checked state of the checkbox.")]
        public int CheckedId
        {
            get => _checkedId;
            set { _checkedId = value; RecalculateBoundsBasedOnGump(); }
        }

        [Category("Gump Specific")]
        [Description("The initial state of the checkbox (Checked = true, Unchecked = false).")]
        public bool InitialStatus
        {
            get => _initialStatus;
            set { _initialStatus = value; }
        }

        [Category("Gump Specific")]
        [Description("The value associated with this checkbox, returned when the gump is submitted.")]
        public int ButtonValue
        {
            get => _buttonValue;
            set { _buttonValue = value; }
        }

        public CanvasCheckboxItem(int x, int y, int page, int uncheckedId = 210, int checkedId = 211, bool initialStatus = false, int buttonValue = 0)
            : base(x, y, page)
        {
            _uncheckedId = uncheckedId;
            _checkedId = checkedId;
            _initialStatus = initialStatus;
            _buttonValue = buttonValue;
            RecalculateBoundsBasedOnGump();
        }

        public void RecalculateBoundsBasedOnGump()
        {
            int gumpIdToUse = _initialStatus ? _checkedId : _uncheckedId;
            if (gumpIdToUse <= 0) gumpIdToUse = _uncheckedId > 0 ? _uncheckedId : _checkedId;
            if (gumpIdToUse <= 0) gumpIdToUse = 210;

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
                System.Diagnostics.Debug.WriteLine($"Error loading gump ID {gumpIdToUse} in RecalculateBounds (Checkbox): {ex.Message}");
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

            int gumpIdToDraw = _initialStatus ? _checkedId : _uncheckedId;
            if (gumpIdToDraw <= 0) gumpIdToDraw = _uncheckedId > 0 ? _uncheckedId : _checkedId;
            if (gumpIdToDraw <= 0) gumpIdToDraw = (_initialStatus ? 211 : 210); // Fallback to default IDs
            
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
                System.Diagnostics.Debug.WriteLine($"Error loading gump ID {gumpIdToDraw} in Draw (Checkbox): {ex.Message}");
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
                    System.Diagnostics.Debug.WriteLine($"Error drawing checkbox Gump ID {gumpIdToDraw}: {ex.Message}");
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
                g.FillRectangle(brush, X, Y, this.Width, this.Height);
                g.DrawRectangle(pen, X, Y, this.Width - 1 , this.Height - 1);
                if (_initialStatus) 
                {
                    g.DrawLine(pen, X + 2, Y + 2, X + this.Width - 3, Y + this.Height - 3);
                    g.DrawLine(pen, X + this.Width - 3, Y + 2, X + 2, Y + this.Height - 3);
                }
            }
        }

        public override void DisposeResources()
        {
            // No owned bitmap resources to dispose if we don't clone/own them.
        }
    }
} 