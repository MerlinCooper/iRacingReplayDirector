using iRacingReplayDirector;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace iRacingDirector.Plugin.Tester
{
    public enum DrawAction { Intro, Main, Outro };

    public class DomainForm
    {
        RemoteImageViewer frm;
        AppDomain domain;

        string pluginPath = "";
        string sessionDataPath;
        private int framesPerSecond = 5;
        private Action<string, string> onError;
        private string backgroundImage;
        private int left;
        private int top;
        private Action<double, double> onAnimationTick = (a, b) => { };
        private int playbackSpeed = 1;
        private Size clientSize;
        private DrawAction drawAction;
        private bool isPaused;

        public static DomainForm CreateRemote(string pluginPath)
        {
            return new DomainForm(pluginPath);
        }

        private DomainForm(string pluginPath)
        {
            this.pluginPath = pluginPath;
        }

        private void Create(bool recreate = false)
        {
            if (domain == null || recreate)
            {
                if (domain != null)
                {
                    frm.Hide();
                    AppDomain.Unload(domain);
                    domain = null;
                }

                var myLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var info = new AppDomainSetup
                {
                    ShadowCopyFiles = "true"
                };

                domain = AppDomain.CreateDomain("TranscodingDomain", null, info);
                frm = (RemoteImageViewer)domain.CreateInstanceFromAndUnwrap(typeof(RemoteImageViewer).Assembly.Location, typeof(RemoteImageViewer).FullName);

                frm.InitPlugin(pluginPath);
            }

            frm.SetFramesPerSecond(framesPerSecond);
            frm.SetOnError(onError);

            if (sessionDataPath != null)
                frm.SetSessionDataPath(sessionDataPath);

            if (backgroundImage != null)
                frm.SetBackgroundImage(backgroundImage);
            frm.ClientSize = clientSize;
            frm.SetPosition(left, top);
            frm.SetPlaybackSpeed(playbackSpeed);
            frm.SetOnAnimationTick(onAnimationTick);
            frm.SetDrawAction(drawAction);
            frm.SetPaused(isPaused);

            frm.Refresh();
        }

        public void SetFramesPerSecond(int framesPerSecond)
        {
            this.framesPerSecond = framesPerSecond;
            Create();
        }

        public void SetOnError(Action<string, string> onError)
        {
            this.onError = onError;
            Create();
        }

        public void SetPluginFileName(string pluginPath)
        {
            this.pluginPath = pluginPath;

            Create(true);
        }

        public void SetBackgroundImage(string fileName)
        {
            this.backgroundImage = fileName;

            Create();
        }

        internal void SetClientSize(Size size)
        {
            this.clientSize = size;
            Create();
        }

        internal void SetPosition(int left, int top)
        {
            this.left = left;
            this.top = top;
            Create();
        }

        internal void Recreate()
        {
            Create(true);

            frm.Show();
            frm.Activate();
        }

        internal void SetSessionDataFileName(string sessionDataPath)
        {
            this.sessionDataPath = sessionDataPath;
            Create();
        }

        internal void SetPlaybackSpeed(int value)
        {
            playbackSpeed = value;
            Create();

        }

        internal void SetOnAnimationTick(Action<double, double> p)
        {
            this.onAnimationTick = p;
        }

        internal void Activate()
        {
            Create();

            frm.Show();
            frm.Activate();
        }

        internal void SetAction(DrawAction drawAction)
        {
            this.drawAction = drawAction;
            Create();
        }

        internal void SetPause(bool isPaused)
        {
            this.isPaused = isPaused;
            Create();
        }

        internal void SetPositionPercentage(float positionPercentage)
        {
            frm.SetPositionPercentage(positionPercentage);

            frm.Refresh();
        }

        internal PluginProxySettings[] GetSettingsList()
        {
            return frm.GetSettingsList();
        }
    }
}
