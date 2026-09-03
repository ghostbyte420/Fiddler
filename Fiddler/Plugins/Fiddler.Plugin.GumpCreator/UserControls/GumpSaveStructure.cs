using System.Collections.Generic;
// using System.Text.Json; // No longer directly needed here if Elements is not JsonElement

namespace Fiddler.Plugin.GumpCreator.UserControls
{
    // Main container for saving/loading
    public class GumpSaveData
    {
        public bool IsClosable { get; set; }
        public bool IsMovable { get; set; }
        public bool IsDisposable { get; set; }
        public int MaxPageIndex { get; set; }
        public List<object> Elements { get; set; } = new List<object>();
    }

    // Base DTO for all canvas elements - still needed for casting during save and for reference
    public class CanvasElementSaveData // No abstract needed for simple DTOs if using manual mapping or System.Text.Json with appropriate attributes later
    {
        public CanvasElementType ElementType { get; set; } // Discriminator
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Page { get; set; }
        public int Z { get; set; }
    }

    public class CanvasGumpPicItemSaveData : CanvasElementSaveData
    {
        public int GumpId { get; set; }
        public int Hue { get; set; }
    }

    public class CanvasTextItemSaveData : CanvasElementSaveData
    {
        public string Text { get; set; }
        public int TextColorHue { get; set; }
    }

    public class CanvasButtonItemSaveData : CanvasElementSaveData
    {
        public int ReleasedGumpId { get; set; }
        public int PressedGumpId { get; set; }
        public int ReturnValue { get; set; }
        public int TargetPageId { get; set; }
    }

    public class CanvasCheckboxItemSaveData : CanvasElementSaveData
    {
        public int UncheckedId { get; set; }
        public int CheckedId { get; set; }
        public bool InitialStatus { get; set; }
        public int ButtonValue { get; set; }
    }

    public class CanvasRadioButtonItemSaveData : CanvasElementSaveData
    {
        public int UnpressedId { get; set; }
        public int PressedId { get; set; }
        public bool InitialStatus { get; set; }
        public int ButtonValue { get; set; }
        public int GroupId { get; set; }
    }

    public class CanvasTextEntryItemSaveData : CanvasElementSaveData
    {
        public string InitialText { get; set; }
        public int TextColorHue { get; set; }
        public int TextId { get; set; }
        public int CharacterLimit { get; set; }
    }
} 