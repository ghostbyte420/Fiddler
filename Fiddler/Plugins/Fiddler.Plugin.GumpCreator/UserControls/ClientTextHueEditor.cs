using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace Fiddler.Plugin.GumpCreator.UserControls
{
    public class ClientTextHueEditor : UITypeEditor
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
                    int initialHueId = (value is int val) ? val : 0;
                    using (var form = new ClientTextHueEditorForm(initialHueId))
                    {
                        if (edSvc.ShowDialog(form) == DialogResult.OK)
                        {
                            return form.SelectedHueId;
                        }
                    }
                }
            }
            return value; 
        }
    }
} 