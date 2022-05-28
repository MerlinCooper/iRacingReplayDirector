using System;
using System.Configuration;
using System.Diagnostics;
using WK.Libraries.HotkeyListenerNS;

namespace iRacingReplayDirector
{
    public partial class Settings : ApplicationSettingsBase
    {
        private static Settings defaultInstance = (Settings)Synchronized(new Settings());

        public static Settings Default
        {
            get { return defaultInstance; }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("")]
        public string WorkingFolder
        {
            get
            {
                return (string)this["WorkingFolder"];
            }
            set
            {
                this["WorkingFolder"] = value;
            }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("15")]
        public int videoBitRate
        {
            get
            {
                return (int)this["videoBitRate"];
            }
            set
            {
                this["videoBitRate"] = value;
            }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        public TrackCameras trackCameras
        {
            get
            {
                return (TrackCameras)this["trackCameras"];
            }
            set
            {
                this["trackCameras"] = value;
            }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("")]
        public string lastSelectedTrackName
        {
            get
            {
                return (string)this["lastSelectedTrackName"];
            }
            set
            {
                this["lastSelectedTrackName"] = value;
            }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("")]
        public string lastVideoFile
        {
            get
            {
                return (string)this["lastVideoFile"];
            }
            set
            {
                this["lastVideoFile"] = value;
            }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("00:00:20")]
        public TimeSpan CameraStickyPeriod
        {
            get
            {
                return (TimeSpan)this["CameraStickyPeriod"];
            }
            set
            {
                this["CameraStickyPeriod"] = value;
            }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("")]
        public string PreferredDriverNames
        {
            get
            {
                return (string)this["PreferredDriverNames"];
            }
            set
            {
                this["PreferredDriverNames"] = value;
            }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("00:00:01")]
        public TimeSpan BattleGap
        {
            get
            {
                return (TimeSpan)this["BattleGap"];
            }
            set
            {
                this["BattleGap"] = value;
            }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("00:02:00")]
        public TimeSpan BattleStickyPeriod
        {
            get
            {
                return (TimeSpan)this["BattleStickyPeriod"];
            }
            set
            {
                this["BattleStickyPeriod"] = value;
            }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("1.6")]
        public double BattleFactor2
        {
            get
            {
                return (double)this["BattleFactor2"];
            }
            set
            {
                this["BattleFactor2"] = value;
            }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("00:00:20")]
        public TimeSpan FollowLeaderAtRaceStartPeriod
        {
            get
            {
                return (TimeSpan)this["FollowLeaderAtRaceStartPeriod"];
            }
            set
            {
                this["FollowLeaderAtRaceStartPeriod"] = value;
            }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("00:00:20")]
        public TimeSpan FollowLeaderBeforeRaceEndPeriod
        {
            get
            {
                return (TimeSpan)this["FollowLeaderBeforeRaceEndPeriod"];
            }
            set
            {
                this["FollowLeaderBeforeRaceEndPeriod"] = value;
            }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("00:10:00")]
        public TimeSpan HighlightVideoTargetDuration
        {
            get
            {
                return (TimeSpan)this["HighlightVideoTargetDuration"];
            }
            set
            {
                this["HighlightVideoTargetDuration"] = value;
            }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("False")]
        public bool FocusOnPreferedDriver
        {
            get
            {
                return (bool)this["FocusOnPreferedDriver"];
            }
            set
            {
                this["FocusOnPreferedDriver"] = value;
            }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("False")]
        public bool DisableIncidentsSearch
        {
            get
            {
                return (bool)this["DisableIncidentsSearch"];
            }
            set
            {
                this["DisableIncidentsSearch"] = value;
            }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("False")]
        public bool SendUsageData
        {
            get
            {
                return (bool)this["SendUsageData"];
            }
            set
            {
                this["SendUsageData"] = value;
            }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("False")]
        public bool HaveAskedAboutUsage
        {
            get
            {
                return (bool)this["HaveAskedAboutUsage"];
            }
            set
            {
                this["HaveAskedAboutUsage"] = value;
            }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("")]
        public string TrackingID
        {
            get
            {
                return (string)this["TrackingID"];
            }
            set
            {
                this["TrackingID"] = value;
            }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("3")]
        public int ResultsFlashCardPosition
        {
            get
            {
                return (int)this["ResultsFlashCardPosition"];
            }
            set
            {
                this["ResultsFlashCardPosition"] = value;
            }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("50")]
        public int IncidentScanWait
        {
            get
            {
                return (int)this["IncidentScanWait"];
            }
            set
            {
                this["IncidentScanWait"] = value;
            }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("10")]
        public int IgnoreIncidentsBelowPosition
        {
            get
            {
                return (int)this["IgnoreIncidentsBelowPosition"];
            }
            set
            {
                this["IgnoreIncidentsBelowPosition"] = value;
            }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("00:00:06")]
        public TimeSpan PeriodWaitForIRacingSwitch
        {
            get
            {
                return (TimeSpan)this["PeriodWaitForIRacingSwitch"];
            }
            set
            {
                this["PeriodWaitForIRacingSwitch"] = value;
            }
        }

        [UserScopedSetting]
        [SettingsProvider(typeof(IracingReplayDirectorProvider))]
        [DebuggerNonUserCode]
        [DefaultSettingValue("False")]
        public bool NewSettings
        {
            get
            {
                return (bool)this["NewSettings"];
            }
            set
            {
                this["NewSettings"] = value;
            }
        }

        [UserScopedSetting]
        [SettingsProvider(typeof(IAVMSettingsProvider))]
        [DebuggerNonUserCode]
        [DefaultSettingValue("")]
        public string MainExecPath
        {
            get
            {
                return (string)this["MainExecPath"];
            }
            set
            {
                this["MainExecPath"] = value;
            }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("")]
        public string OverlayPluginId
        {
            get
            {
                return (string)this["OverlayPluginId"];
            }
            set
            {
                this["OverlayPluginId"] = value;
            }
        }

        //[UserScopedSetting]
        //[SettingsProvider(typeof(IAVMSettingsProvider))]
        //[DebuggerNonUserCode]
        //public GitHubReleases.GitHubCachedReleases[] GitHubCachedReleases
        //{
        //    get
        //    {
        //        return (GitHubReleases.GitHubCachedReleases[])this["GitHubCachedReleases"];
        //    }
        //    set
        //    {
        //        this["GitHubCachedReleases"] = value;
        //    }
        //}

        [UserScopedSetting]
        [SettingsProvider(typeof(IracingReplayDirectorProvider))]
        [DebuggerNonUserCode]
        public PluginProxySettings[] PluginStoredSettings
        {
            get
            {
                return (PluginProxySettings[])this["PluginStoredSettings"];
            }
            set
            {
                this["PluginStoredSettings"] = value;
            }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("1.1")]
        public double VideoSplitGap
        {
            get
            {
                return (double)this["VideoSplitGap"];
            }
            set
            {
                this["VideoSplitGap"] = value;
            }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("StandardOverlays")]
        public string PluginName
        {
            get
            {
                return (string)this["PluginName"];
            }
            set
            {
                this["PluginName"] = value;
            }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("False")]
        public bool IgnoreIncidentsDuringRaceStart
        {
            get
            {
                return (bool)this["IgnoreIncidentsDuringRaceStart"];
            }
            set
            {
                this["IgnoreIncidentsDuringRaceStart"] = value;
            }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("Alt + F10")]
        public string strHotKeyPauseResume  
        {
            get
            {
                return (string)this["strHotKeyPauseResume"];
            }
            set{
                this["strHotKeyPauseResume"] = value;
            }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("Alt + F9")]
        public string strHotKeyStopStart
        {
            get
            {
                return (string)this["strhotKeyStopStart"];
            }
            set
            {
                this["strhotKeyStopStart"] = value;
            }
        }
    }
}
