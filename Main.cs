// This file is part of iRacingReplayDirector.
//
// Copyright 2014 Dean Netherton
// https://github.com/vipoo/iRacingReplayDirector.net
//
// iRacingReplayDirector is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// iRacingReplayDirector is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with iRacingReplayDirector.  If not, see <http://www.gnu.org/licenses/>.

using iRacingReplayDirector.Phases;
using iRacingReplayDirector.Phases.Capturing;
using iRacingReplayDirector.Support;
using iRacingReplayDirector.Video;
using iRacingSDK;
using iRacingSDK.Support;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
 

namespace iRacingReplayDirector
{
    public partial class Main : Form
    {
        internal static class NativeMethods
        {
            [DllImport("kernel32.dll")]
            public static extern uint SetThreadExecutionState(uint esFlags);
            public const uint ES_CONTINUOUS = 0x80000000;
            public const uint ES_SYSTEM_REQUIRED = 0x00000001;
            public const uint ES_DISPLAY_REQUIRED = 0x00000002;
        }

        System.Windows.Forms.Timer fileWatchTimer;

        int videoBitRateNumber = 15;
        IRacingReplay iRacingProcess;

        enum States {Idle, CapturingGameData, Transcoding};
        States _states = States.Idle;

        System.Windows.Forms.Timer lookForAudioBitRates;
        LogMessages logMessagges;
        const string DefaultLogFileName = "general.log";
        SessionData lastSession;
        bool isConnected;

        States State
        {
            set
            {
                _states = value;
                StateUpdated();
            }
            get
            {
                return _states;
            }
        }

        void StateUpdated()
        {
            var trackCamerasDefined =
                Settings.Default.trackCameras != null &&
                lastSession != null &&
                Settings.Default.trackCameras.Any(tc => tc.TrackName == lastSession.WeekendInfo.TrackDisplayName) &&
                Settings.Default.trackCameras.RaceStart[lastSession.WeekendInfo.TrackDisplayName] != null &&
                Settings.Default.trackCameras.Incident[lastSession.WeekendInfo.TrackDisplayName] != null &&
                Settings.Default.trackCameras.LastLap[lastSession.WeekendInfo.TrackDisplayName] != null;

            switch (_states)
            {
                case States.Idle:
                    BeginProcessButton.Enabled = Directory.Exists(workingFolderTextBox.Text) && isConnected && trackCamerasDefined;
                    configureTrackCamerasLabel.Visible = isConnected && !trackCamerasDefined;
                    transcodeVideoButton.Enabled = IsReadyForTranscoding();
                    transcodeCancelButton.Visible = false;
                    transcodeProgressBar.Visible = false;
                    transcodeProgressBar.Value = 0;
                    videoBitRate.Enabled =
                    workingFolderTextBox.Enabled =
                    workingFolderButton.Enabled =
                    sourceVideoTextBox.Enabled =
                    sourceVideoButton.Enabled = true;
                    break;

                case States.CapturingGameData:
                    BeginProcessButton.Enabled = false;
                    transcodeVideoButton.Enabled = false;
                    transcodeCancelButton.Visible = false;
                    transcodeProgressBar.Visible = false;
                    videoBitRate.Enabled =
                    workingFolderTextBox.Enabled =
                    workingFolderButton.Enabled =
                    sourceVideoTextBox.Enabled =
                    sourceVideoButton.Enabled = false;
                    break;

                case States.Transcoding:
                    BeginProcessButton.Enabled = Directory.Exists(workingFolderTextBox.Text) && isConnected && trackCamerasDefined;
                    configureTrackCamerasLabel.Visible = isConnected && !trackCamerasDefined;
                    transcodeVideoButton.Enabled = false;
                    transcodeCancelButton.Visible = true;
                    transcodeProgressBar.Visible = true;
                    videoBitRate.Enabled =
                    workingFolderTextBox.Enabled =
                    workingFolderButton.Enabled =
                    sourceVideoTextBox.Enabled =
                    sourceVideoButton.Enabled = false;
                    break;
            }
        }

        string transcodeMessageFormatDetails;
        string transcodeMessageErrorDetails;
        string transcodeMessageWarningDetails;

        bool isEmpty(string s)
        {
            return s == null || s == "";
        }

        void SetTanscodeMessage(string formatDetails = null, string warningDetails = null, string sourceVideoFileErrorMessage = null, string trancodingErrorMessage = null)
        {
            if (formatDetails != null)
                transcodeMessageFormatDetails = formatDetails;

            if (warningDetails != null)
                transcodeMessageWarningDetails = warningDetails;

            if (sourceVideoFileErrorMessage != null)
                transcodeMessageErrorDetails = sourceVideoFileErrorMessage;

            var message = transcodeMessageFormatDetails;
            if(!isEmpty(transcodeMessageWarningDetails))
                message = isEmpty(message) ? transcodeMessageWarningDetails : "{0}\n{1}".F(message, transcodeMessageWarningDetails);

            if (!isEmpty(transcodeMessageErrorDetails))
                message = isEmpty(message) ? transcodeMessageErrorDetails : "{0}\n{1}".F(message, transcodeMessageErrorDetails);

            if (!isEmpty(trancodingErrorMessage))
                message = isEmpty(message) ? trancodingErrorMessage : "{0}\n{1}".F(message, trancodingErrorMessage);

            VideoDetailLabel.Text = message;
        }

        public Main()
        {
            iracingEvents = new iRacingEvents();

            InitializeComponent();
        }

        string GetDefaultLogFileName()
        {
            if (Directory.Exists(workingFolderTextBox.Text))
                return Path.Combine(workingFolderTextBox.Text, "general.log");

            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "general.log");
        }

        async void Main_Load(object sender, EventArgs e)
        {
            changeVersionButton.Visible = File.Exists(Settings.Default.MainExecPath);

            Settings.Default.SettingChanging += Default_SettingChanging;
            iracingEvents.NewSessionData += iracingEvents_NewSessionData;
            iracingEvents.Connected += iracingEvents_Connected;
            iracingEvents.Disconnected += iracingEvents_Disconnected;
            iracingEvents.StartListening();

            workingFolderTextBox.Text = Settings.Default.WorkingFolder;

            //Assign applications stored in Project-Settings
            cb_ShutdownAfterEncode.Checked = Properties.Settings.Default.bShutdownPCAfterEncoding;
            cb_CloseiRacingAfterRecording.Checked = Properties.Settings.Default.bCloseiRacingAfterRecording;
            cb_EncodeVideoAfterCapture.Checked = Properties.Settings.Default.bEncodeVideoAfterCapture;
            cb_FastVideoRecording.Checked = Properties.Settings.Default.bFastVideoRecording;
            cb_ShortTestOnly.Checked = Properties.Settings.Default.bShortTestOnly;
            cb_HighLightVideoOnly.Checked = Properties.Settings.Default.bHighLightVideoOnly;
            cb_UseNewSettingsDlg.Checked = Properties.Settings.Default.bUseNewSettingsDialog;
            
            

            logMessagges = new LogMessages();
            Trace.Listeners.Add(new MyListener(logMessagges.TraceMessage));

            LogListener.ToFile(GetDefaultLogFileName());
            AwsLogListener.SetPhaseGeneral();

            new Task(LogSystemInformation).Start();

            fileWatchTimer = new System.Windows.Forms.Timer();
            fileWatchTimer.Interval = 10;
            fileWatchTimer.Tick += (s, a) => OnGameDataFileChanged();
            fileWatchTimer.Start();

            videoBitRate.Text = Settings.Default.videoBitRate.ToString();
            sourceVideoTextBox.Text = Settings.Default.lastVideoFile;

            BeginProcessButton.Enabled = false;

            iRacingProcess = new IRacingReplay()
                .WhenIRacingStarts(() => 
                {
                    BeginProcessButton.Enabled = true;
                    workingFolderTextBox_TextChanged(null, null); 
                    ProcessErrorMessageLabel.Visible = false; 
                    WaitingForIRacingLabel.Visible = false;
                })
                .InTheBackground(errorMessage => { });
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.bShutdownPCAfterEncoding = cb_ShutdownAfterEncode.Checked;
            Properties.Settings.Default.bCloseiRacingAfterRecording = cb_CloseiRacingAfterRecording.Checked;
            Properties.Settings.Default.bEncodeVideoAfterCapture = cb_EncodeVideoAfterCapture.Checked;
            Properties.Settings.Default.bFastVideoRecording = cb_FastVideoRecording.Checked;
            Properties.Settings.Default.bShortTestOnly = cb_ShortTestOnly.Checked;
            Properties.Settings.Default.bHighLightVideoOnly = cb_HighLightVideoOnly.Checked;
            //enable change of standard setting to use NewSettingsDialog only after intensive testings that all values can be reliably set using the advanced settings dialog
            //Properties.Settings.Default.bUseNewSettingsDialog = cb_UseNewSettingsDlg.Checked;
            Properties.Settings.Default.Save();
        }

        void LogSystemInformation()
        {
            var StringBuilder1 = new StringBuilder(string.Empty);
            try
            {
                var config = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.PerUserRoamingAndLocal);
                TraceDebug.WriteLine("Local user config path: {0}", config.FilePath);
                TraceDebug.WriteLine("Application Version: {0}", AboutBox1.AssemblyVersion);

                TraceDebug.WriteLine(Environment.Is64BitOperatingSystem ? "OS: 64x" : "OS: 32x");
                TraceDebug.WriteLine("ProcessorCount:  {0}".F(Environment.ProcessorCount));

                foreach (DriveInfo DriveInfo1 in DriveInfo.GetDrives())
                    TraceDebug.WriteLine("Drive: {0}, Type: {1}, Format: {2}, Size: {3}, AvailableFreeSpace: {4}".F(
                        GetValue(() => DriveInfo1.Name),
                        GetValue(() => DriveInfo1.DriveType.ToString()),
                        GetValue(() => DriveInfo1.DriveFormat),
                        GetValue(() => DriveInfo1.TotalSize.ToString()), 
                        GetValue(() => DriveInfo1.AvailableFreeSpace.ToString())));

                TraceDebug.WriteLine("SystemPageSize:  {0}".F(Environment.SystemPageSize));
                TraceDebug.WriteLine("Version:  {0}".F(Environment.Version));

                //LogDeviceInformation("Win32_Processor");
                //LogDeviceInformation("Win32_VideoController");
            }
            catch
            {
            }
        }

        private string GetValue(Func<string> fn)
        {
            try
            {
                return fn();
            }
            catch
            {
                return "";
            }
        }

        void LogDeviceInformation(string stringIn)
        {
            var mc = new ManagementClass(stringIn);
            var instances = mc.GetInstances();
            var properties = mc.Properties;
            foreach (var instance in instances)
                foreach (var property in properties)
                    TraceDebug.WriteLine("{0}: {1}".F(property.Name, GetValue(() => instance.Properties[property.Name].Value.ToString())));
        }

        private void Default_SettingChanging(object sender, System.Configuration.SettingChangingEventArgs e)
        {
            try
            {
                TraceDebug.WriteLine("Setting Changed: key: {0}, name: {1}, value: {2}".F(e.SettingKey, e.SettingName, e.NewValue));
            }
            catch(Exception ex)
            {
                TraceDebug.WriteLine("Unable to log setting change");
                TraceDebug.WriteLine(ex.Message);
            }
        }

        void iracingEvents_Disconnected()
        {
            isConnected = false;
            StateUpdated();
        }

        void iracingEvents_Connected()
        {
            isConnected = true;
            StateUpdated();
        }

        void iracingEvents_NewSessionData(DataSample data)
        {
            lastSession = data.SessionData;
            StateUpdated();
        }

        void workingFolderButton_Click(object sender, EventArgs e)
        {
            var fbd = new FolderBrowserDialog();
            fbd.SelectedPath = workingFolderTextBox.Text;

            if (fbd.ShowDialog() == DialogResult.OK)
                workingFolderTextBox.Text = fbd.SelectedPath;
        }

        void sourceVideoButton_Click(object sender, EventArgs e)
        {
            var fbd = new OpenFileDialog();
            fbd.Filter = "Replay Script Data|*.replayscript|XML|*.xml|All files (*.*)|*.*";
            fbd.FileName = sourceVideoTextBox.Text;

            if (fbd.ShowDialog() == DialogResult.OK)
                sourceVideoTextBox.Text = fbd.FileName;
        }

        void TranscodeVideo_Click(object sender, EventArgs e)
        {
            State = States.Transcoding;
            SetTanscodeMessage(trancodingErrorMessage: null);

            LogListener.ToFile(Path.ChangeExtension(sourceVideoTextBox.Text, "log"));
            AwsLogListener.SetPhaseTranscode();

            NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS | NativeMethods.ES_SYSTEM_REQUIRED);

            iRacingProcess = new IRacingReplay()
                .WithEncodingOf(videoBitRate: videoBitRateNumber * 1000000)
                .WithOverlayFile(overlayFile: sourceVideoTextBox.Text)
                .OverlayRaceDataOntoVideo(OnTranscoderProgress, OnTranscoderCompleted, cb_HighLightVideoOnly.Checked, cb_ShutdownAfterEncode.Checked)
                .InTheBackground(errorMessage => {
                    OnTranscoderCompleted();
                    SetTanscodeMessage(trancodingErrorMessage: errorMessage);
                    LogListener.ToFile(GetDefaultLogFileName());
                    AwsLogListener.SetPhaseGeneral();
                    NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS);
                });
        }

        void OnTranscoderProgress(long timestamp, long duration)
        {
            transcodeProgressBar.Value = Math.Min(transcodeProgressBar.Maximum, (int)(timestamp * transcodeProgressBar.Maximum / duration));
        }

        void OnTranscoderCompleted()
        {
            State = States.Idle;
        }
        
        void transcodeCancel_Click(object sender, EventArgs e)
        {
            iRacingProcess.RequestAbort();
            State = States.Idle;
        }

        void sourceVideoTextBox_TextChanged(object sender, EventArgs e)
        {
            Settings.Default.lastVideoFile = sourceVideoTextBox.Text;
            Settings.Default.Save();

            if (lookForAudioBitRates != null )
            {
                lookForAudioBitRates.Stop();
                lookForAudioBitRates.Dispose();
                lookForAudioBitRates = null;
            }

            OnGameDataFileChanged();

            SetTanscodeMessage("", "", "");

            if (sourceVideoTextBox.Text.Trim() == "")
                return;

            if (!File.Exists(sourceVideoTextBox.Text) ) 
            {
                SetTanscodeMessage(sourceVideoFileErrorMessage: "*File does not exist");
                return;
            }
            
            try
            {
                var data = OverlayData.FromFile(sourceVideoTextBox.Text);
                var fileName = data.VideoFiles.Last().FileName;

                if (!File.Exists(fileName))
                {
                    SetTanscodeMessage(sourceVideoFileErrorMessage: "*Captured video does not exist: " + fileName);
                    return;
                }
                
                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

                if (data.CapturedVersion != null && data.CapturedVersion != currentVersion)
                {
                    SetTanscodeMessage(warningDetails: "*Video was captured with version {0}.\nIt is recommended to transcode and capture using the same version.\nTranscoding may not work.".F(data.CapturedVersion));
                }

                var details = VideoAttributes.TestFor(data);
                
                SetTanscodeMessage(formatDetails: "Frame Rate: {0}, Frame Size: {1}x{2}, Video: {3} @ {4}Mbs, Audio: {5}, {6}Khz @ {7}Kbs, ".F
                        (details.FrameRate,
                        details.FrameSize.Width,
                        details.FrameSize.Height,
                        details.VideoEncoding,
                        details.BitRate == 0 ? "-- " : details.BitRate.ToString(),
                        details.AudioEncoding,
                        details.AudioSamplesPerSecond / 1000,
                        details.AudioAverageBytesPerSecond / 1000), 
                    sourceVideoFileErrorMessage: details.ErrorMessage);
            }
            catch(Exception ex)
            {
                SetTanscodeMessage(sourceVideoFileErrorMessage: "*Error reading the video file. {0}".F(ex.Message));

                lookForAudioBitRates = new System.Windows.Forms.Timer();
                lookForAudioBitRates.Tick += sourceVideoTextBox_TextChanged;
                lookForAudioBitRates.Interval = 5000;
                lookForAudioBitRates.Start();
            }
        }

        bool IsReadyForTranscoding()
        {
            if (transcodeMessageErrorDetails != null && transcodeMessageErrorDetails != "")
                return false;

            if (sourceVideoTextBox.Text == null || sourceVideoTextBox.Text.Length == 0)
                return false;
            
            return File.Exists(sourceVideoTextBox.Text);
        }

        void OnGameDataFileChanged()
        {
            if (State == States.Idle)
                transcodeVideoButton.Enabled = IsReadyForTranscoding();
        }

        void videoBitRate_TextChanged(object sender, EventArgs e)
        {
            if(int.TryParse(videoBitRate.Text, out videoBitRateNumber))
            {
                Settings.Default.videoBitRate = videoBitRateNumber;
                Settings.Default.Save();
            }
        }

        void BeginProcessButton_Click(object sender, EventArgs e)
        {
            BeginProcessButton.Enabled = false;
            AnalysingRaceLabel.Visible = true;
            State = States.CapturingGameData;

            LogListener.ToFile(workingFolderTextBox.Text + "\\capture.log");
            AwsLogListener.SetPhaseCapture();

            NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS | NativeMethods.ES_SYSTEM_REQUIRED | NativeMethods.ES_DISPLAY_REQUIRED);

            iRacingProcess = new IRacingReplay(shortTestOnly: cb_ShortTestOnly.Checked, bRecordUsingPauseResume:cb_FastVideoRecording.Checked, bCloseiRacingAfterRecording: cb_CloseiRacingAfterRecording.Checked)
                .WithWorkingFolder(workingFolderTextBox.Text)
                .AnalyseRace(() => { AnalysingRaceLabel.Visible = false; CapturingRaceLabel.Visible = true; })
                .CaptureOpeningScenes()
                .CaptureRace(overlayFileName =>
                {
                    sourceVideoTextBox.Text = overlayFileName;

                    LogListener.MoveToFile(Path.ChangeExtension(overlayFileName, "log"));
                })
                .CloseIRacing()
                .InTheBackground(errorMessage =>
                {
                    workingFolderTextBox_TextChanged(null, null);
                    WaitingForIRacingLabel.Visible = false;
                    AnalysingRaceLabel.Visible = false;
                    CapturingRaceLabel.Visible = false;
                    ProcessErrorMessageLabel.Visible = true;
                    ProcessErrorMessageLabel.Text = errorMessage;
                    State = States.Idle;
                    StateUpdated();

                    LogListener.ToFile(GetDefaultLogFileName());
                    AwsLogListener.SetPhaseGeneral();

                    WindowState = FormWindowState.Minimized;
                    Show();
                    WindowState = FormWindowState.Normal;
                    this.BringToFront();

                    if (errorMessage == null && transcodeVideoButton.Enabled && cb_EncodeVideoAfterCapture.Checked)
                    {
                        tabControl1.SelectedIndex = 1;
                        Thread.Sleep(1000);
                        TranscodeVideo_Click(null, null);
                    }
                    else
                        NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS);
                });
        }

        void SettingsButton_Click(object sender, EventArgs e)
        {
            var settings = new ConfigureTrackCameras();
            settings.ShowDialog();
            StateUpdated();
        }

        void configureVideoCaptureButton_Click(object sender, EventArgs e)
        {
            var settings = cb_UseNewSettingsDlg.Checked ? (Form)new AdvancedGeneralSettingsDlg(Settings.Default) : (Form)new ConfigureGeneralSettings(Settings.Default);
            //var settings = new ConfigureGeneralSettings(Settings.Default);
            settings.ShowDialog();
        }

        void logMessagesButton_Click(object sender, EventArgs e)
        {
            logMessagges.Show();
        }

        void workingFolderTextBox_TextChanged(object sender, EventArgs e)
        {
            Settings.Default.WorkingFolder = workingFolderTextBox.Text;
            Settings.Default.Save();

            StateUpdated();
        }

        public iRacingEvents iracingEvents { get; set; }

        private void verifyVideoCaptureButton_Click(object sender, EventArgs e)
        {
            var f = new TestVideoCapture();
            f.ShowDialog();

            workingFolderTextBox.Text = Settings.Default.WorkingFolder;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            (new AboutBox1()).ShowDialog(); 
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var f = new TestVideoConversion();
            f.ShowDialog();
        }

        private void Main_Activated(object sender, EventArgs e)
        {
            if(!AwsKeys.HaveKeys)
                return;

            if(!Settings.Default.HaveAskedAboutUsage)
            {
                Settings.Default.HaveAskedAboutUsage = true;
                Settings.Default.Save();
                (new UsageDataRequest()).ShowDialog();
            }
        }

        private void changeVersionButton_Click(object sender, EventArgs e)
        {
            Process.Start(Settings.Default.MainExecPath, "-update");
            this.Close();
        }

        private void configurePluginsButton_Click(object sender, EventArgs e)
        {
            var f = new ConfigurePlugins();
            f.ShowDialog();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void cb_FastVideoRecording_CheckedChanged(object sender, EventArgs e)
        {
            cb_EncodeVideoAfterCapture.Checked = !cb_FastVideoRecording.Checked;
        }

        private void EncodeVideoAfterCapture_CheckedChanged(object sender, EventArgs e)
        {
            cb_FastVideoRecording.Checked = !cb_EncodeVideoAfterCapture.Checked;
        }

        private void checkBox1_CheckedChanged_1(object sender, EventArgs e)
        {

        }

        private void cb_ShutdownPCAfterEncoding_Changed(object sender, EventArgs e)
        {
            //Properties.Settings.Default.Save();
        }

      
    }
}
