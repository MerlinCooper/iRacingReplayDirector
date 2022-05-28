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

using System;
using System.Windows.Forms;

namespace iRacingReplayDirector
{
    public partial class ConfigureGeneralSettings : Form
    {
        private Action onSave;
        private Settings settings;

        public ConfigureGeneralSettings(Settings settings)
        {
            this.settings = settings;
            InitializeComponent();
            AddPanelComponents();
        }

        void AddPanelComponents()
        {
            var f = new GeneralSettingFields(panel, helpText, () => this.ActiveControl, Settings.Default);

            f.AddTimeField("Time between camera switches:", "The time period that must elapsed before a new random camera is selected.", "CameraStickyPeriod");
            f.AddTimeField("Time between battle switches:", "The time period that must elapsed before a new battle is randomly selected.", "BattleStickyPeriod");
            f.AddTimeField("Time gap between cars for battle:", "The approximate amount of time between cars to determine if they are battling.  Default 1 second.", "BattleGap");
            f.AddTimeField("Time to track leader at race start", "The amount of time, to stay focused on the leader, at race start.  After this period, the normal camera and driver tracking rules will apply.", "FollowLeaderAtRaceStartPeriod");
            f.AddTimeField("Time to track leader on last lap", "The amount of time, to stay focused on the leader, prior to the race finish.", "FollowLeaderBeforeRaceEndPeriod");
            f.AddNumberField("Factor for battle selection.", "A factor to bias the random selection of battles.  Larger numbers will tend to focus on battles at front.  Smaller numbers will increase the chance of battles futher down the order to be selected.  Recommend values between 1.3 and 2.0.\n\nShould always be more than 1.0\n\n1.0 means all battles have same chance of selection.", "BattleFactor2");

            f.AddBlankRow();
            f.AddMinuteField("Duration of Highlight video", "The duration to target for the length of the edited highlight videos.", "HighlightVideoTargetDuration");

            f.AddBlankRow();
            f.AddKeyPressField("Hot Key for Video Capture (stop/start)", "The hotkey used by your video capture program to stop/start a recording.  Default value is ALT+F9 and can only be changed using Advanced-Settings-Dialog option");
            f.AddKeyPressField("Hot Key for Video Capture (pause/resume)", "The hotkey used by your video capture program to pause/resume a recording.  Default value is ALT+F10 and can only be changed using Advanced-Settings-Dialog option");

            f.AddBlankRow();
            f.AddCheckboxField("Ignore Incidents Capture",
                @"If this option is selected, then the director will not capture or focus on incidents or crashes",
                "DisableIncidentsSearch");

            f.AddIntField("Incident Scan wait",
                @"Advanced: The number of 'samples' the application will wait for the replay to have been reposition, upon requesting to move to the next incident.

Smaller values will risk false positives for incident detections.

Larger values will cause the incident scanning phase to take longer.",
                "IncidentScanWait");

            f.AddIntField("Ignore Incident Below", @"Ignore any incidents below the specified position.", "IgnoreIncidentsBelowPosition");

            f.AddCheckboxField("Ignore Incidents during race start", 
                "If this option is select, then the camera will stay on the leader for during the race start, and will not following any incidents that happen in the field",
                "IgnoreIncidentsDuringRaceStart");

            f.AddBlankRow();
            f.AddStringField("Preferred driver names (comma separated):", "A comma seperated list of driver names, to preference in camera selection.", "PreferredDriverNames");
            f.AddCheckboxField("Only select battles for my perferred drivers",
               @"This option determines what drivers will be selected for battles.  

If selected, then the camera will only focus on battles of your preferred drivers.  All other battles will be ignored and cut from the highlighted video.

If not selected, then all battles can be selected, but your perferred drivers will be prioritised",
               "FocusOnPreferedDriver");

            f.AddBlankRow();
            f.AddIntField("Show Results after nth position", "Show the results flash cards, after the driver in the selected position finishes.", "ResultsFlashCardPosition");

            f.AddBlankRow();
            f.AddTimeField("Time to wait for IRacing to re-open", "The time after clicking 'Begin Capture', the application will wait, for iRacing to reload on the screen.  For larger replays or bigger tracks, you may need to increase this value. Default 6 seconds", "PeriodWaitForIRacingSwitch");

            f.AddBlankRow();
            f.AddNumberField("Time lost between captured video file splits", "If your video capture software, split the captured file into multiple files, it may lose a slight amount of time between the files.  Assign this field to an estimate of the averge number of seconds lost between each captured video file", "VideoSplitGap");

            f.AddBlankRow();
            f.AddPluginSelectorField();

            if (AwsKeys.HaveKeys)
            {
                f.AddBlankRow();
                f.AddCheckboxField("Allow usage data to be sent to developer",
                @"Send anonymous usage data to the developer, to assist in fixing bugs and improving the application.

What data is sent? 
The data sent is the same data that is written to your local log files.

Why should I say yes?
It will really help me to understand how people use this application, and diagnose common problems people have.

Where does the data go ?
The data is sent encypted to me - the developer - dean.netherton@gmail.com", "SendUsageData");

            }

            this.onSave = f.OnSave;
        }

        void okButton_Click(object sender, EventArgs e)
        {            
            this.onSave();
        }

        void OnFocus(object sender, EventArgs e)
        {
            helpText.Text = "";
            if (this.ActiveControl.Tag != null)
                helpText.Text = this.ActiveControl.Tag.ToString();
        }

        private void ConfigureGeneralSettings_Load(object sender, EventArgs e)
        {

        }
    }
}