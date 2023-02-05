using iRacingReplayDirector.Phases;
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace iRacingDirector.Plugin.Tester
{
    public partial class Main : Form
    {
        DomainForm domainForm;
        bool isPaused = false;
        private bool moving;
        private FileSystemWatcher watcher;
        private System.Windows.Forms.Timer changedTimer;

        public Main()
        {
            InitializeComponent();
        }

        void browseBackgroundImageButton_Click(object sender, EventArgs e)
        {
            var fbd = new OpenFileDialog
            {
                Filter = "Image Files(*.BMP;*.JPG;*.GIF)|*.BMP;*.JPG;*.GIF|All files (*.*)|*.*"
            };
            if (backgroundTestImageFileName.Text != "")
            {
                fbd.FileName = Path.GetFileName(backgroundTestImageFileName.Text);
                fbd.InitialDirectory = Path.GetDirectoryName(backgroundTestImageFileName.Text);
            }

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                backgroundTestImageFileName.Text = fbd.FileName;
                backgroundTestImageFileName_Leave(null, null);
            }

            domainForm.SetBackgroundImage(backgroundTestImageFileName.Text);
        }

        void fullSizeButton_Click(object sender, EventArgs e)
        {
            domainForm.SetClientSize(new Size(1920, 1080));
        }

        void halfSizeButton_Click(object sender, EventArgs e)
        {
            domainForm.SetClientSize(new Size(1920 / 2, 1080 / 2));
        }

        void thirdSizeButton_Click(object sender, EventArgs e)
        {
            domainForm.SetClientSize(new Size(1920 / 3, 1080 / 3));
        }

        void browsePluginButton_Click(object sender, EventArgs e)
        {
            var fbd = new OpenFileDialog();
            fbd.Filter = "Assembly (*.dll)|*.dll";
            if (pluginAssemblyFileName.Text != "")
            {
                fbd.FileName = Path.GetFileName(pluginAssemblyFileName.Text);
                fbd.InitialDirectory = Path.GetDirectoryName(pluginAssemblyFileName.Text);
            }
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                pluginAssemblyFileName.Text = fbd.FileName;
                pluginAssemblyFileName_Leave(null, null);
            }
        }

        void Main_Load(object sender, EventArgs e)
        {
            Left = Screen.PrimaryScreen.WorkingArea.Width / 24;
            Top = Screen.PrimaryScreen.WorkingArea.Height / 16;

            pluginAssemblyFileName.Text = Properties.Settings.Default.PluginAssemblyFileName;
            backgroundTestImageFileName.Text = Properties.Settings.Default.BackgroundTestImageFileName;

            sampleSessionDataFileName.Text = Properties.Settings.Default.SampleSessionDataFileName;

            domainForm = DomainForm.CreateRemote(pluginAssemblyFileName.Text);
            domainForm.SetSessionDataFileName(sampleSessionDataFileName.Text);

            domainForm.SetOnError((s, m) => errorDetailsTextBox.Text = s + "\r\n" + m);
            domainForm.SetOnAnimationTick((d, f) =>
            {
                playbackTimeLabel.Text = String.Format("Time: {0} over {1}", f, d);

                double percentage = f / d;
                if (!moving)
                    replayProgress.SplitterDistance = (int)(percentage * replayProgress.Width);

            });
            if (File.Exists(backgroundTestImageFileName.Text))
            {

                domainForm.SetBackgroundImage(backgroundTestImageFileName.Text);
                domainForm.SetClientSize(new Size(1920 / 3, 1080 / 3));
                domainForm.SetPosition(this.Left, this.Top + this.Height);
            }
            domainForm.SetFramesPerSecond((int)framesPerSecond.Value);
            domainForm.Activate();
            RecreateWatcher();
        }

        void pluginAssemblyFileName_Leave(object sender, EventArgs e)
        {
            OnNewPluginPath();
        }

        void OnNewPluginPath()
        {
            Properties.Settings.Default.PluginAssemblyFileName = pluginAssemblyFileName.Text;
            Properties.Settings.Default.Save();

            domainForm.SetPluginFileName(pluginAssemblyFileName.Text);
            domainForm.Activate();

            RecreateWatcher();
        }

        private void RecreateWatcher()
        {
            if (watcher != null)
                this.watcher.Dispose();

            var context = SynchronizationContext.Current;

            this.watcher = new FileSystemWatcher(Path.GetDirectoryName(pluginAssemblyFileName.Text), Path.GetFileName(pluginAssemblyFileName.Text));
            watcher.Changed += (s, e) => context.Post(Watcher_Changed);
            this.watcher.EnableRaisingEvents = true;
        }

        private void Watcher_Changed()
        {
            if (this.changedTimer != null)
            {
                this.changedTimer.Stop();
                this.changedTimer.Dispose();
            }
            this.changedTimer = new System.Windows.Forms.Timer();
            changedTimer.Tick += (s, e) =>
            {
                changedTimer.Stop();
                changedTimer.Dispose();
                domainForm.SetPluginFileName(pluginAssemblyFileName.Text);
                domainForm.Activate();
            };
            changedTimer.Interval = 1000;
            changedTimer.Start();
        }

        void backgroundTestImageFileName_Leave(object sender, EventArgs e)
        {
            Properties.Settings.Default.BackgroundTestImageFileName = backgroundTestImageFileName.Text;
            Properties.Settings.Default.Save();
        }

        void browserSampleSessionDataButton_Click(object sender, EventArgs e)
        {
            var fbd = new OpenFileDialog();
            fbd.Filter = "Assembly (*.replayscript)|*.replayscript";
            if (sampleSessionDataFileName.Text != "")
            {
                fbd.FileName = Path.GetFileName(sampleSessionDataFileName.Text);
                fbd.InitialDirectory = Path.GetDirectoryName(sampleSessionDataFileName.Text);
            }
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                sampleSessionDataFileName.Text = fbd.FileName;
                sampleSessionDataFileName_Leave(null, null);
            }
        }

        void sampleSessionDataFileName_Leave(object sender, EventArgs e)
        {
            Properties.Settings.Default.SampleSessionDataFileName = sampleSessionDataFileName.Text;
            Properties.Settings.Default.Save();
            domainForm.SetSessionDataFileName(sampleSessionDataFileName.Text);
        }

        void framesPerSecond_ValueChanged(object sender, EventArgs e)
        {
            domainForm.SetFramesPerSecond((int)framesPerSecond.Value);
        }

        void playbackSpeed_ValueChanged(object sender, EventArgs e)
        {
            domainForm.SetPlaybackSpeed((int)playbackSpeed.Value);
        }

        void introFlashCardButton_Click(object sender, EventArgs e)
        {
            domainForm.SetAction(DrawAction.Intro);
        }

        void mainRaceButton_Click(object sender, EventArgs e)
        {
            domainForm.SetAction(DrawAction.Main);
        }

        void playPauseButton_Click(object sender, EventArgs e)
        {
            isPaused = !isPaused;
            playPauseButton.Text = isPaused ? "play" : "pause";
            domainForm.SetPause(isPaused);
        }

        void replayProgress_SplitterMoving(object sender, SplitterCancelEventArgs e)
        {
            moving = true;
            domainForm.SetPositionPercentage((float)e.SplitX / replayProgress.Width);
            moving = false;
        }

        void replayProgress_MouseDown(object sender, MouseEventArgs e)
        {
            domainForm.SetPause(true);
        }

        void replayProgress_MouseUp(object sender, MouseEventArgs e)
        {
            domainForm.SetPause(isPaused);
        }

        private void outroFlashCard_Click(object sender, EventArgs e)
        {
            domainForm.SetAction(DrawAction.Outro);
        }

        private void generalSettingsButton_Click(object sender, EventArgs e)
        {
            var frm = new iRacingReplayDirector.PluginSettings(iRacingReplayDirector.Settings.Default, domainForm.GetSettingsList());
            frm.ShowDialog();

        }
    }
}
