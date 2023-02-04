using iRacingReplayDirector;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace iRacingDirector.Plugin.Tester
{
    public partial class ImageViewer : Form
    {
        const long OneNanoSecond = 10000000;
        long Duration = OneNanoSecond * 60;

        Timer animationTimer;
        PluginProxy PluginProxy;
        Action<string, string> onError;
        long timestamp = 0;
        int framesPerSecond;
        DateTime offset;
        int playbackSpeed = 1;
        Action<double, double> onAnimationTick = (d, t) => { };
        long lastTimerTick = 0;
        Action<Graphics> drawAction = g => { };
        private bool isPaused;

        public void SetOnError(Action<string, string> onError)
        {
            this.onError = onError;
        }

        public void SetOnAnimationTick(Action<double, double> onAnimationTick)
        {
            this.onAnimationTick = onAnimationTick;
        }

        public void InitPlugin(string pluginPath)
        {
            if (PluginProxy != null)
                throw new Exception("Attempt to load plugin twice");

            PluginProxy = new PluginProxy(pluginPath);
        }

        public void SetSessionDataPath(string replayConfigPath)
        {
            PluginProxy.SetReplayConfig(replayConfigPath);
            PluginProxy.InjectFields(0, new string[0]);
        }

        public void SetFramesPerSecond(int framesPerSecond)
        {
            this.framesPerSecond = framesPerSecond;
            if (animationTimer == null)
            {
                animationTimer = new Timer();
                animationTimer.Tick += (s, e) => OnAnimate();
            }
            animationTimer.Interval = 1000 / framesPerSecond;
            animationTimer.Start();
            offset = DateTime.Now;
        }

        internal void SetPlaybackSpeed(int value)
        {
            this.playbackSpeed = value;
        }

        internal void SetDrawAction(DrawAction drawAction)
        {
            switch (drawAction)
            {
                case DrawAction.Intro:
                    this.drawAction = this.DrawActionIntro;
                    this.Duration = 60 * OneNanoSecond;
                    break;
                case DrawAction.Main:
                    this.drawAction = this.DrawActionMain;
                    Duration = PluginProxy.Duration;
                    break;
                case DrawAction.Outro:
                    this.drawAction = this.DrawActionOutro;
                    this.Duration = 60 * OneNanoSecond;
                    break;
            }
        }

        internal PluginProxySettings[] GetSettingsList()
        {
            return PluginProxy.GetSettingsList();
        }

        internal void SetPositionPercentage(float positionPercentage)
        {
            this.lastTimerTick = this.timestamp = (long)(Duration * positionPercentage);
            onAnimationTick(Duration / OneNanoSecond, Math.Round((float)timestamp / OneNanoSecond, 4));

        }

        internal void SetPaused(bool isPaused)
        {
            this.isPaused = isPaused;
        }

        public ImageViewer()
        {
            InitializeComponent();
        }

        private void OnAnimate()
        {
            if (isPaused)
                return;

            var period = (DateTime.Now - offset).TotalSeconds;
            offset = DateTime.Now;

            timestamp += (long)(OneNanoSecond * period) * playbackSpeed;
            if (timestamp > Duration)
            {
                timestamp = timestamp % Duration;
                onAnimationTick(Duration / OneNanoSecond, Math.Round((float)timestamp / OneNanoSecond, 4));
                lastTimerTick = timestamp;
            }

            if (timestamp - lastTimerTick > (OneNanoSecond / 8))
            {
                onAnimationTick(Duration / OneNanoSecond, Math.Round((float)timestamp / OneNanoSecond, 1));
                lastTimerTick = timestamp;
            }

            Refresh();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            try
            {
                e.Graphics.ScaleTransform(this.ClientSize.Width / 1920f, this.ClientSize.Height / 1080f);
                e.Graphics.DrawImage(BackgroundImage, 0, 0);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                drawAction(e.Graphics);
            }
            catch (Exception ex)
            {
                if (onError != null)
                    onError(ex.Message, ex.StackTrace);
            }
        }

        void DrawActionIntro(Graphics g)
        {
            if (PluginProxy == null)
                return;

            PluginProxy.SetGraphics(g);
            PluginProxy.InjectFields(timestamp, new string[0]);
            PluginProxy.DrawIntroFlashCard(Duration);
        }

        private void DrawActionOutro(Graphics g)
        {
            if (PluginProxy == null)
                return;

            PluginProxy.SetGraphics(g);
            PluginProxy.InjectFields(timestamp, "".Split(','));
            PluginProxy.DrawOutroFlashCard(Duration, timestamp);
        }

        private void DrawActionMain(Graphics g)
        {
            if (PluginProxy == null)
                return;

            PluginProxy.SetGraphics(g);
            PluginProxy.InjectFields(timestamp, new string[0]);
            PluginProxy.RaceOverlay();
        }

        private void ImageViewer_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
    }
}
