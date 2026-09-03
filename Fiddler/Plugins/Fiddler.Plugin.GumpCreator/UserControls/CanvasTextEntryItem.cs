using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Text;
using Ultima;
using System.Windows.Forms;

namespace Fiddler.Plugin.GumpCreator.UserControls
{
    public class CanvasTextEntryItem : CanvasElement
    {
        public override CanvasElementType ElementType => CanvasElementType.Text;

        private string _initialText;
        private int _textColorHue;
        private int _textId;
        private int _charLimit; // 0 for no limit

        // Default UO Text Colors (approximate ARGB for display)
        private static readonly Color DefaultUoTextColor = Color.FromArgb(255, 220, 220, 220); // A light gray/off-white

        [Category("Gump Specific")]
        [Description("The initial text displayed in the text entry field.")]
        public string InitialText
        {
            get => _initialText;
            set { _initialText = value; }
        }

        [Category("Gump Specific")]
        [Description("The hue of the text color (0-3171 or specific client values). 0 for default.")]
        public int TextColorHue
        {
            get => _textColorHue;
            set { _textColorHue = value; }
        }

        [Category("Gump Specific")]
        [Description("The ID for this text entry, used to retrieve its value on gump submission.")]
        public int TextId
        {
            get => _textId;
            set { _textId = value; }
        }

        [Category("Gump Specific")]
        [Description("The maximum number of characters allowed in the text entry (0 for no limit).")]
        public int CharacterLimit
        {
            get => _charLimit;
            set { _charLimit = value; }
        }

        public CanvasTextEntryItem(int x, int y, int page, int width, int height, string initialText = "", int textColorHue = 0, int textId = 0, int charLimit = 0)
            : base(x, y, page)
        {
            Width = width;
            Height = height;
            OriginalWidth = width;
            OriginalHeight = height;
            _initialText = initialText;
            _textColorHue = textColorHue;
            _textId = textId;
            _charLimit = charLimit;
        }

        public override void Draw(Graphics g)
        {
            if (g == null) return;

            // Draw background (typically a gump part, like 0x0BBC or 0x0DAC)
            // For now, a simple rectangle to represent the text entry area
            using (var bgBrush = new SolidBrush(Color.FromArgb(50, Color.Black))) // Semi-transparent black
            using (var borderPen = new Pen(Color.DarkGray))
            {
                g.FillRectangle(bgBrush, X, Y, Width, Height);
                g.DrawRectangle(borderPen, X, Y, Width - 1, Height - 1);
            }

            // Determine text color
            Color displayColor = DefaultUoTextColor;
            if (_textColorHue > 0)
            {
                try
                {
                    ushort clientHue = (ushort)(_textColorHue + 1); // UO hues are often 1-based in some contexts
                    if (clientHue < Hues.List.Length && Hues.List[clientHue] != null)
                    {
                         // Using one of the middle colors of the hue for display.
                        displayColor = Hues.List[clientHue].GetColor(16); 
                    }
                }
                catch { /* Use default if hue conversion fails */ }
            }

            // Draw text
            using (var textBrush = new SolidBrush(displayColor))
            using (var font = new Font("Arial", 10F)) // Placeholder font, UO uses specific fonts
            {
                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                Rectangle textRect = new Rectangle(X + 2, Y + 2, Width - 4, Height - 4);
                TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding;
                
                string displayText = string.IsNullOrEmpty(_initialText) ? "[TextEntry]" : _initialText;
                if (Selected && string.IsNullOrEmpty(_initialText)) displayText = "[TextEntry]"; // Ensure placeholder visible when selected and empty
                else if (string.IsNullOrEmpty(_initialText)) displayText = "";

                TextRenderer.DrawText(g, displayText, font, textRect, displayColor, flags);
            }

            if (Selected)
            {
                DrawSelection(g);
            }
        }

        public override void DisposeResources()
        {
            // No specific resources to dispose for this class
        }
    }
} 