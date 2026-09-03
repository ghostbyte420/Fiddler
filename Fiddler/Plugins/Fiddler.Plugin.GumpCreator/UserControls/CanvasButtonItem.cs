using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms; // For TextRenderer, SystemFonts
using Ultima; // For Gumps
using System; // For Exception

namespace Fiddler.Plugin.GumpCreator.UserControls
{
    public class CanvasButtonItem : CanvasElement
    {
        private int _releasedGumpId = 247; // Default UO button
        private int _pressedGumpId = 248;  // Default UO button pressed
        private Bitmap _image; // This will hold the image for the ReleasedGumpId

        [Category("Button")]
        [Description("Gump ID for the button's normal (released) state.")]
        public int ReleasedGumpId 
        { 
            get => _releasedGumpId; 
            set { _releasedGumpId = value; UpdateImageAndBounds(); }
        }

        [Category("Button")]
        [Description("Gump ID for the button's pressed state.")]
        public int PressedGumpId 
        { 
            get => _pressedGumpId; 
            set { _pressedGumpId = value; /* Does not change visual in editor currently, but could load _pressedImage here if needed */ } 
        }

        [Category("Button Behavior")]
        [Description("Page number to jump to when this button is clicked. Set to 0 if not changing page.")]
        public int TargetPageId { get; set; } = 0;

        [Category("Button Behavior")]
        [Description("The unique integer value this button returns to the script.")]
        public int ReturnValue { get; set; } = 1; // Default to a common "OK" or action value

        public override CanvasElementType ElementType => CanvasElementType.Button;

        public CanvasButtonItem(int x, int y, int page) : base(x, y, page)
        {
            UpdateImageAndBounds();
        }

        private void UpdateImageAndBounds()
        {
            // _image?.Dispose(); // DO NOT dispose image from Gumps.GetGump() if it's shared
            _image = null; // Clear previous reference
            try
            {
                if (Gumps.IsValidIndex(_releasedGumpId))
                {
                    _image = Gumps.GetGump(_releasedGumpId);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading Gump ID {_releasedGumpId} for Button: {ex.Message}");
                _image = null;
            }

            if (_image != null && _image.Width > 0 && _image.Height > 0)
            {
                OriginalWidth = Width = _image.Width;
                OriginalHeight = Height = _image.Height;
            }
            else 
            {
                OriginalWidth = Width = 50; // Fallback size if gump is invalid or has zero dimensions
                OriginalHeight = Height = 20; 
            }
        }

        public override void Draw(Graphics g)
        {
            if (_image != null && _image.Width > 0 && _image.Height > 0)
            {
                try
                {
                    g.DrawImage(_image, Bounds);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error drawing button Gump ID {_releasedGumpId}: {ex.Message}");
                    g.DrawRectangle(Pens.DarkBlue, Bounds);
                    TextRenderer.DrawText(g, $"Err ID:{_releasedGumpId}", SystemFonts.DefaultFont, Bounds, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
                }
            }
            else 
            { 
                g.DrawRectangle(Pens.DarkBlue, Bounds); 
                TextRenderer.DrawText(g, $"Btn ID:{_releasedGumpId}", SystemFonts.DefaultFont, Bounds, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
            }
            base.DrawSelection(g);
        }

        public override void DisposeResources() 
        {
            // DO NOT dispose _image here if it's a shared resource from Gumps.GetGump()
            // _image?.Dispose(); 
            _image = null; // Just release the reference
        }
    }
} 