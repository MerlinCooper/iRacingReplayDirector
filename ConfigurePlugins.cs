using iRacingSDK.Support;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace iRacingReplayDirector
{
    public partial class ConfigurePlugins : Form
    {
        public struct PluginDetails
        {
            public string FriendlyName;
            public string Owner;
            public string Repo;
            public string User;

            public string Id { get { return "{0}/{1}".F(User, Repo); } }
        }

        bool hasInited = false;

        public ConfigurePlugins()
        {
            InitializeComponent();
            //twoColumnDropDown<PluginDetails>(pluginNames, (v, i) => i == 0 ? v.FriendlyName : "by {0}".F(v.Owner));
            //twoColumnDropDown<VersionItem>(pluginVersions, (v, i) => i == 0 ? v.VersionStamp : v.DateTimeStamp);
        }

        [Serializable]
        public class Arguments
        {
            public string Path;
            public Action<string> RetrievedVersionCallback;

            public void RetrieveVersion()
            {
                var versionStamp = AssemblyName.GetAssemblyName(Path).Version.ToString();

                RetrievedVersionCallback("StandardOverlay: {0}".F(versionStamp));
            }
        }

        private void ConfigurePlugins_Load(object sender, EventArgs e)
        {
            pluginNames.Items.Clear();
            UpdatePluginDetails();
        }

        private void UpdatePluginDetails()
        {
            var myLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var info = new AppDomainSetup();
            var domain = AppDomain.CreateDomain("TranscodingDomain", null, info);

            var a = new Arguments
            {
                Path = Path.Combine(myLocation, @"plugins\iRacingDirector.Plugin.StandardOverlays\iRacingDirector.Plugin.StandardOverlays.dll"),
                RetrievedVersionCallback = t => this.currentInstalled.Text = t
            };

            domain.DoCallBack(a.RetrieveVersion);
        }

        private void ConfigurePlugins_Activated(object sender, EventArgs e)
        {
            if (hasInited)
                return;

            hasInited = true;

            var plugins = new[]
            {
                new PluginDetails {
                    User = "vipoo",
                    Repo = "iRacingDirector.Plugin.StandardOverlays",
                    Owner = "Dean Netherton",
                    FriendlyName = "StandardOverlay"
                }
            };

            pluginNames.Items.Add(plugins[0]);

            pluginNames.SelectedItem = plugins.FirstOrDefault(p => p.Id == Settings.Default.OverlayPluginId);
        }

        void twoColumnDropDown<T>(ComboBox comboBox, Func<T, int, string> getValue)
        {
            comboBox.DrawMode = DrawMode.OwnerDrawFixed;
            comboBox.DrawItem += delegate (object cmb, DrawItemEventArgs args)
            {
                args.DrawBackground();

                if (args.Index == -1)
                    return;

                var obj = (T)comboBox.Items[args.Index];

                var first = getValue(obj, 0);
                var second = " " + getValue(obj, 1);

                var r1 = args.Bounds;
                r1.Width = 130;

                using (SolidBrush sb = new SolidBrush(args.ForeColor))
                    args.Graphics.DrawString(first, args.Font, sb, r1);

                using (Pen p = new Pen(Color.Black))
                    args.Graphics.DrawLine(p, r1.Right, 0, r1.Right, r1.Bottom);

                var r2 = args.Bounds;
                r2.X = r1.Width;
                r2.Width /= 2;

                using (SolidBrush sb = new SolidBrush(args.ForeColor))
                    args.Graphics.DrawString(second, args.Font, sb, r2);
            };
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            if (pluginNames.SelectedItem == null)
            {
                Settings.Default.OverlayPluginId = null;
                return;
            }

            var selectedItem = (PluginDetails)pluginNames.SelectedItem;

            Settings.Default.OverlayPluginId = selectedItem.Id;
            Settings.Default.Save();
        }

        private async void pluginNames_SelectedIndexChanged(object sender, EventArgs e)
        {
            //    var versions = await GitHubAccess.GetVersions("vipoo", "iRacingDirector.Plugin.StandardOverlays");

            //    foreach(var v in versions)
            //        this.pluginVersions.Items.Add(v);

        }

        private void button1_Click(object sender, EventArgs e)
        {
            /*var version = (VersionItem)this.pluginVersions.SelectedItem;
            this.Enabled = false;

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Settings.Default.MainExecPath,
                    Arguments = "-update-plugin -user={0} -repo={1} -version={2}".F(
                                "vipoo", "iRacingDirector.Plugin.StandardOverlays", version.VersionStamp)
                },
                 EnableRaisingEvents = true
            };

            var context = SynchronizationContext.Current;
            process.Exited += (s, ee) => context.Post(i => {
                this.Enabled = true;
                UpdatePluginDetails();
            }, null);
            process.Start();*/
        }
    }
}
