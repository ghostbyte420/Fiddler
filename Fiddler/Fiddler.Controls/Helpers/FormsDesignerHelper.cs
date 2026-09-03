using System.Windows.Forms;

namespace Fiddler.Controls.Helpers
{
    public static class FormsDesignerHelper
    {
        public static bool IsInDesignMode()
        {
            return Application.ProductName.Contains("Visual Studio");
        }
    }
}
