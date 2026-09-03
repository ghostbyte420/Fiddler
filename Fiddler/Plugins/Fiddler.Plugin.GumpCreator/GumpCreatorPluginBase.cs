using System;
using System.Windows.Forms;
using Fiddler.Controls.Plugin;
using Fiddler.Controls.Plugin.Interfaces;
using Fiddler.Plugin.GumpCreator.UserControls;

namespace Fiddler.Plugin.GumpCreator
{
    public class GumpCreatorPluginBase : PluginBase
    {
        /// <summary>
        /// Name of the plugin
        /// </summary>
        public override string Name { get; } = "Gump Creator";

        /// <summary>
        /// Description of the Plugin's purpose
        /// </summary>
        public override string Description { get; } = "Plugin for creating gumps";

        /// <summary>
        /// Author of the plugin
        /// </summary>
        public override string Author { get; } = "Elanon";

        /// <summary>
        /// Version of the plugin
        /// </summary>
        public override string Version { get; } = "0.1.0";

        /// <summary>
        /// Host of the plugin
        /// </summary>
        public override IPluginHost Host { get; set; }

        public override void Initialize()
        {
            // Initialize plugin
        }

        public override void Unload()
        {
            // Cleanup when plugin is unloaded
        }

        public override void ModifyTabPages(TabControl tabControl)
        {
            TabPage page = new TabPage
            {
                Tag = tabControl.TabCount + 1,
                Text = "Gump Creator"
            };
            page.Controls.Add(new GumpCreatorControl());
            tabControl.TabPages.Add(page);
        }

        public override void ModifyPluginToolStrip(ToolStripDropDownButton toolStrip)
        {
            ToolStripMenuItem item = new ToolStripMenuItem
            {
                Text = "Gump Creator"
            };
            item.Click += (sender, e) => new GumpCreatorControl().Show();
            toolStrip.DropDownItems.Add(item);
        }
    }
} 