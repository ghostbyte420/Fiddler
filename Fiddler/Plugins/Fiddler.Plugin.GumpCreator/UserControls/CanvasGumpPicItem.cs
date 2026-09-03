using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Windows.Forms;
using Ultima;

namespace Fiddler.Plugin.GumpCreator.UserControls
{
    public class CanvasGumpPicItem : CanvasElement
    {
        private int _gumpId;
        private int _hue = 0;
        private Bitmap _originalImage;
        private Bitmap _huedImage;

        [Category("GumpPic")]
        public int GumpId 
        { 
            get => _gumpId; 
            set { 
                _gumpId = value; 
                LoadImage(); 
            } 
        }

        [Category("GumpPic")]
        [Description("Hue of the GumpPic (0-2999 primarily). Click [...] to pick a hue. Default is 0 (no hue).")]
        [Editor(typeof(PolHueEditor), typeof(UITypeEditor))]
        public int Hue 
        { 
            get => _hue;
            set { 
                _hue = value; 
                ApplyHueToImage(); 
            } 
        }

        [Browsable(false)]
        public Bitmap ImageToDraw => _huedImage ?? _originalImage;
        
        public override CanvasElementType ElementType => CanvasElementType.GumpPic;

        public CanvasGumpPicItem(int gumpId, int x, int y, int page) : base(x, y, page)
        {
            _gumpId = gumpId;
            LoadImage();
        }

        private void LoadImage()
        {
            _huedImage?.Dispose();
            _huedImage = null;

            Bitmap tempOriginal = null;
            try 
            {
                if (Gumps.IsValidIndex(_gumpId))
                {
                    tempOriginal = Gumps.GetGump(_gumpId); 
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Gumps.GetGump({_gumpId}): {ex.Message}");
                tempOriginal = null;
            }
            
            _originalImage = tempOriginal;

            if (_originalImage != null && _originalImage.Width > 0 && _originalImage.Height > 0)
            {
                OriginalWidth = Width = _originalImage.Width;
                OriginalHeight = Height = _originalImage.Height;
                ApplyHueToImage();
            }
            else
            {
                OriginalWidth = Width = 32; 
                OriginalHeight = Height = 32;
            }
        }

        private void ApplyHueToImage()
        {
            _huedImage?.Dispose();
            _huedImage = null;

            if (_originalImage != null && _originalImage.Width > 0 && _originalImage.Height > 0 && _hue > 0 && _hue < 3000 && Hues.List[_hue] != null)
            {
                try
                {
                    _huedImage = (Bitmap)_originalImage.Clone();
                    Hues.List[_hue].ApplyTo(_huedImage, false);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error applying hue to GumpId {_gumpId}: {ex.Message}");
                    _huedImage?.Dispose();
                    _huedImage = null;
                }
            }
        }

        public override void Draw(Graphics g)
        {
            Bitmap currentImage = ImageToDraw;
            if (currentImage != null && currentImage.Width > 0 && currentImage.Height > 0)
            {
                try
                {
                    g.DrawImage(currentImage, Bounds);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error drawing GumpId {GumpId}: {ex.Message}. Bounds: {Bounds}");
                    DrawErrorPlaceholder(g, $"Draw Err: {GumpId}");
                }
            }
            else
            {
                DrawErrorPlaceholder(g, $"Bad Img: {GumpId}");
            }

            base.DrawSelection(g);
        }

        private void DrawErrorPlaceholder(Graphics g, string message)
        {
            g.DrawRectangle(Pens.Red, Bounds);
            TextRenderer.DrawText(g, message, SystemFonts.DefaultFont, Bounds, Color.Red, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
        }

        public override void DisposeResources() 
        {
            _huedImage?.Dispose();
            _huedImage = null;
        }
    }
} 