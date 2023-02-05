using iRacingReplayDirector;
using System;
using System.Drawing;

namespace iRacingDirector.Plugin.Tester
{
    public class RemoteImageViewer : MarshalByRefObject
    {
        private ImageViewer frm;

        public RemoteImageViewer()
        {
            frm = new ImageViewer();
        }

        public Size ClientSize
        {
            get
            {
                return frm.ClientSize;
            }
            set
            {
                frm.ClientSize = value;
            }
        }

        public void SetBackgroundImage(string imagePath)
        {
            frm.BackgroundImage = Image.FromFile(imagePath);
        }

        public void Show()
        {
            frm.Show();
        }

        internal void Activate()
        {
            frm.Activate();
        }

        internal void Refresh()
        {
            frm.Refresh();
        }

        public void InitPlugin(string pluginPath)
        {
            frm.InitPlugin(pluginPath);
        }

        public void SetOnError(Action<string, string> onError)
        {
            frm.SetOnError(onError);
        }

        internal void SetPosition(int left, int top)
        {
            frm.Left = left;
            frm.Top = top;
        }

        internal void Hide()
        {
            frm.Hide();
        }

        internal void SetFramesPerSecond(int framesPerSecond)
        {
            frm.SetFramesPerSecond(framesPerSecond);
        }

        internal void SetPlaybackSpeed(int value)
        {
            frm.SetPlaybackSpeed(value);
        }

        internal void SetOnAnimationTick(Action<double, double> p)
        {
            frm.SetOnAnimationTick(p);
        }

        internal void SetDrawAction(DrawAction drawAction)
        {
            frm.SetDrawAction(drawAction);
        }

        internal void SetPaused(bool isPaused)
        {
            frm.SetPaused(isPaused);
        }

        internal void SetPositionPercentage(float positionPercentage)
        {
            frm.SetPositionPercentage(positionPercentage);
        }

        internal PluginProxySettings[] GetSettingsList()
        {
            return frm.GetSettingsList();
        }

        internal void SetSessionDataPath(string sessionDataPath)
        {
            frm.SetSessionDataPath(sessionDataPath);
        }
    }
}
