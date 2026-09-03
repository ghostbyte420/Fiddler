using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design; // Required for UITypeEditor
using System.Windows.Forms;
// Ultima using is not directly needed here anymore if all hue logic uses ClientTextHueCollection

namespace Fiddler.Plugin.GumpCreator.UserControls
{
    public class CanvasTextItem : CanvasElement
    {
        private string _textString = "New Text";
        [Category("Text")]
        public string Text 
        {
            get { return _textString; } 
            set 
            {
                _textString = value;
                RecalculateBounds(); 
            }
        }

        private int _textColorHue = 0;
        [Category("Text")]
        [Description("Client Text Color Hue (e.g., 0-1000 range). Click [...] to pick a hue.")]
        [Editor(typeof(ClientTextHueEditor), typeof(UITypeEditor))]
        public int TextColorHue 
        { 
            get => _textColorHue; 
            set { 
                _textColorHue = value; 
                // Consider an Invalidate call on the canvas for live preview
                // if (_gumpCanvasPanel != null) _gumpCanvasPanel.Invalidate(); 
            } 
        }
        
        [Browsable(false)]
        public Color ActualColor 
        { 
            get 
            { 
                return ClientTextHueCollection.GetDisplayColor(TextColorHue);
            }
        }

        [Browsable(false)]
        public override int Width { get => base.Width; set { base.Width = value; /* Bounds are primarily text-driven */ } } 
        [Browsable(false)]
        public override int Height { get => base.Height; set { base.Height = value; /* Bounds are primarily text-driven */ } }

        public override CanvasElementType ElementType => CanvasElementType.Text;
        private static readonly Font DefaultTextFont = SystemFonts.DefaultFont;

        public CanvasTextItem(int x, int y, int page, string text = "New Text") : base(x, y, page)
        {
            _textString = text;
            RecalculateBounds();
        }

        public void RecalculateBounds()
        {
            Size textSize = TextRenderer.MeasureText(_textString, DefaultTextFont);
            base.Width = OriginalWidth = textSize.Width > 0 ? textSize.Width : 20; // Ensure min width
            base.Height = OriginalHeight = textSize.Height > 0 ? textSize.Height : 10; // Ensure min height
        }

        public override void Draw(Graphics g)
        {
            TextRenderer.DrawText(g, Text, DefaultTextFont, Bounds, ActualColor, TextFormatFlags.Default);
            base.DrawSelection(g); 
        }
    }
} 