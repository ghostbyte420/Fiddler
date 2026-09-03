using System.Drawing;

namespace Fiddler.Plugin.GumpCreator.UserControls
{
    public struct ClientTextHue
    {
        public int HueId { get; }
        public string Name { get; }
        public Color DisplayColor { get; }
        public bool IsSpecial { get; } // e.g., for Black, White, or section headers

        public ClientTextHue(int hueId, string name, Color displayColor, bool isSpecial = false)
        {
            HueId = hueId;
            Name = name;
            DisplayColor = displayColor;
            IsSpecial = isSpecial;
        }

        public override string ToString()
        {
            return $"{HueId} - {Name}";
        }
    }
} 