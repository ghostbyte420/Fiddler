using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Fiddler.Plugin.GumpCreator.UserControls
{
    public static class ClientTextHueCollection
    {
        public static List<ClientTextHue> Hues { get; private set; }

        static ClientTextHueCollection()
        {
            Hues = new List<ClientTextHue>
            {
                // Based on common UO Text Color Charts (approximations)
                // Special Hues
                new ClientTextHue(0, "Default/Black", Color.FromArgb(50, 50, 50), true), // Often a dark grey, not pure black on some clients
                new ClientTextHue(910, "Pure White", Color.White, true),

                // Standard Palette (examples, more can be added)
                // First Column from chart (approximate colors)
                new ClientTextHue(1, "System Blue", Color.FromArgb(0,0,255)),
                new ClientTextHue(2, "Dark Grey Text", Color.FromArgb(100,100,100)),
                new ClientTextHue(3, "Light Purple", Color.FromArgb(204,153,255)),
                new ClientTextHue(4, "Dark Purple", Color.FromArgb(153,102,204)),
                new ClientTextHue(5, "Light Blue", Color.FromArgb(153,204,255)),
                new ClientTextHue(6, "Medium Blue", Color.FromArgb(102,153,204)),
                new ClientTextHue(7, "Dark Blue", Color.FromArgb(51,102,153)),
                new ClientTextHue(8, "Light Green", Color.FromArgb(153,255,153)),
                new ClientTextHue(9, "Medium Green", Color.FromArgb(102,204,102)),
                new ClientTextHue(10, "Dark Green", Color.FromArgb(51,153,51)),
                new ClientTextHue(20, "Reddish Pink", Color.FromArgb(255,153,153)),
                new ClientTextHue(25, "Yellowish Green", Color.FromArgb(204,255,153)),
                new ClientTextHue(30, "Orange/Brown", Color.FromArgb(255,153,102)),
                new ClientTextHue(32, "Bright Red", Color.FromArgb(255,0,0)),
                new ClientTextHue(33, "Red", Color.FromArgb(204,0,0)),
                new ClientTextHue(34, "Dark Red", Color.FromArgb(153,0,0)),
                new ClientTextHue(35, "Darker Red", Color.FromArgb(102,0,0)),
                new ClientTextHue(38, "Yellow", Color.FromArgb(255,255,0)),
                new ClientTextHue(40, "Light Orange", Color.FromArgb(255,204,153)),
                
                // Second Column from chart (approximate colors)
                new ClientTextHue(41, "Orange", Color.FromArgb(255,150,0)),
                new ClientTextHue(52, "Green", Color.FromArgb(0,255,0)),
                new ClientTextHue(63, "Cyan", Color.FromArgb(0,255,255)),
                new ClientTextHue(78, "Greyish Blue", Color.FromArgb(150,150,200)),
                
                // Third Column from chart (approximate colors)
                new ClientTextHue(81, "Blueish Grey", Color.FromArgb(170,170,220)),
                new ClientTextHue(88, "Bright Blue", Color.FromArgb(0,128,255)),
                new ClientTextHue(90, "Deep Blue", Color.FromArgb(0,0,200)),
                new ClientTextHue(99, "Light Grey", Color.FromArgb(200,200,200)),
                new ClientTextHue(100, "Medium Grey", Color.FromArgb(150,150,150)),

                // Some higher values often used for system messages or specific colors
                new ClientTextHue(138, "Dark Orange/Speech", Color.FromArgb(255,128,0)), // Often speech color
                new ClientTextHue(160, "Faction Purple", Color.FromArgb(128,0,255)),
                new ClientTextHue(231, "Valorite Blue", Color.FromArgb(0,128,192)),
                new ClientTextHue(250, "Evil Red/Monster", Color.FromArgb(192,0,0)),
                // Many more can be added here. POL documentation or community resources
                // like UO Razor color lists can be a good source for these RGB mappings.
                // For a full list (1-1000+), a config file might be better than hardcoding.
            };
        }

        public static Color GetDisplayColor(int hueId)
        {
            var foundHue = Hues.FirstOrDefault(h => h.HueId == hueId);
            // If specific hueId is not in our predefined list, try to get a generic gump hue color
            // This is a fallback and might not be accurate for client text.
            if (foundHue.Name == null && hueId > 0 && hueId < 3000 && Ultima.Hues.List[hueId] != null)
            {
                 // Fallback: If not in our specific list, use general Hue (might not be what client renders for text)
                return Ultima.Hues.List[hueId].GetColor(16); 
            }
            return foundHue.DisplayColor; // Returns default(Color) (black) if not found by FirstOrDefault and not in fallback range
        }
    }
} 