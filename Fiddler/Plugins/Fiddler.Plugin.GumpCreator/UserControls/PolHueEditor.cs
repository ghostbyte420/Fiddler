using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design; // Required for IWindowsFormsEditorService

namespace Fiddler.Plugin.GumpCreator.UserControls
{
    public class PolHueEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if (provider != null)
            {
                var edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
                if (edSvc != null)
                {
                    int initialHue = (value is int val) ? val : 0;
                    using (var form = new PolHueEditorForm(initialHue))
                    {
                        if (edSvc.ShowDialog(form) == DialogResult.OK)
                        {
                            return form.SelectedHue;
                        }
                    }
                }
            }
            return value; // Return original value if service is unavailable or dialog is cancelled
        }
    }
} 