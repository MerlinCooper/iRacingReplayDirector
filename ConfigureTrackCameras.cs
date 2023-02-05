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

using iRacingSDK;
using iRacingSDK.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace iRacingReplayDirector
{
    public partial class ConfigureTrackCameras : Form
    {
        DataSample lastSample;

        string TrackName
        {
            get { return (string)trackList.SelectedItem; }
        }

        public ConfigureTrackCameras()
        {
            InitializeComponent();
        }

        void cameraList_SelectedIndexChanged(object sender, EventArgs e)
        {
            ratioTextBox.Enabled = cameraList.SelectedItems.Count != 0;
            if (ratioTextBox.Enabled)
            {
                ratioTextBox.Text = cameraList.SelectedItems[0].SubItems[1].Text;
                ratioLabel.Text = "{0} has a".F(cameraList.SelectedItems[0].Text);
                var context = SynchronizationContext.Current;
                new Task(() => { Thread.Sleep(200); context.Post(i => ratioTextBox.Focus(), null); }).Start();

                var trackCamera = GetCamerasForSelectedTrack().FirstOrDefault(tc => tc.CameraName == cameraList.SelectedItems[0].Text);

                cameraAngleSelection.SelectedIndex = (int)trackCamera.CameraAngle;
            }
            else
            {
                ratioTextBox.Text = "";
                ratioLabel.Text = "-- has a";
            }
        }

        void trackList_SelectedIndexChanged(object sender, EventArgs e)
        {
            InitaliseListOfCameras();

            cameraList_SelectedIndexChanged(sender, e);
            UpdateTotal();
        }

        void ratioTextBox_TextChanged(object sender, EventArgs e)
        {
            if (cameraList.SelectedItems.Count == 0)
                return;

            var trackCamera = GetCamerasForSelectedTrack().FirstOrDefault(tc => tc.CameraName == cameraList.SelectedItems[0].Text);

            int number = 0;
            int.TryParse(ratioTextBox.Text, out number);
            if (number > 99)
                number = 0;

            trackCamera.Ratio = number;
            cameraList.SelectedItems[0].SubItems[1].Text = number.ToString();

            UpdateTotal();
        }

        void cameraAngleSelection_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cameraList.SelectedItems.Count == 0)
                return;

            var allCameras = trackCameras.Where(tc => tc.CameraName == cameraList.SelectedItems[0].Text);

            foreach (var tc in allCameras)
                tc.CameraAngle = (CameraAngle)cameraAngleSelection.SelectedIndex;
        }

        IEnumerable<TrackCamera> GetCamerasForSelectedTrack()
        {
            return trackCameras.Where(tc => tc.TrackName == (string)trackList.SelectedItem);
        }

        void TrackCameraPerferences_FormClosing(object sender, FormClosingEventArgs e)
        {
            Settings.Default.lastSelectedTrackName = (string)trackList.SelectedItem;
            Settings.Default.Save();
        }

        void okButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        TrackCameras trackCameras
        {
            get
            {
                var result = Settings.Default.trackCameras;
                if (result == null)
                {
                    result = new TrackCameras();
                    Settings.Default.trackCameras = result;
                    Settings.Default.Save();
                }

                return result;
            }
        }

        void TrackCameraPerferences_Load(object sender, EventArgs e)
        {
            lastSample = new iRacingConnection().GetDataFeed().First();

            cameraAngleSelection.Items.Add("Front Facing"); // CameraAngle.LookingInfrontOfCar);
            cameraAngleSelection.Items.Add("Rear Facing"); // CameraAngle.LookingBehindCar);
            cameraAngleSelection.Items.Add("Looking at Car"); // CameraAngle.LookingAtCar);
            cameraAngleSelection.Items.Add("Static Track Camera"); // CameraAngle.LookingAtTrack);

            DiscoverAnyNewTrackCameras();

            InitialiseDropDownListOfTracks();
        }

        void InitialiseDropDownListOfTracks()
        {
            foreach (var track in trackCameras.Select(tc => tc.TrackName).Distinct().OrderBy(t => t))
                trackList.Items.Add(track);

            if (lastSample.IsConnected)
                trackList.SelectedItem = lastSample.SessionData.WeekendInfo.TrackDisplayName;
            else
                trackList.SelectedItem = Settings.Default.lastSelectedTrackName;
        }

        /// <summary>
        /// Load session data from iRacing and loads the cameras available for the current loaded
        /// track and saves the track/camera combination into the users setting
        /// </summary>
        void DiscoverAnyNewTrackCameras()
        {
            if (!lastSample.IsConnected)
                return;

            var allCamerasForTrack = trackCameras.Where(tc => tc.TrackName == lastSample.SessionData.WeekendInfo.TrackDisplayName).ToList();
            trackCameras.RemoveAll(tc => tc.TrackName == lastSample.SessionData.WeekendInfo.TrackDisplayName);

            foreach (var camera in lastSample.SessionData.CameraInfo.Groups)
            {
                var trackCamera = allCamerasForTrack.FirstOrDefault(tc => tc.TrackName == lastSample.SessionData.WeekendInfo.TrackDisplayName && tc.CameraName == camera.GroupName);
                if (trackCamera == null)
                {
                    trackCamera = new TrackCamera();

                    trackCamera.TrackName = lastSample.SessionData.WeekendInfo.TrackDisplayName;
                    trackCamera.CameraName = camera.GroupName;

                    var similiarCamera = trackCameras.FirstOrDefault(tc => tc.CameraName == trackCamera.CameraName);
                    if (similiarCamera != null)
                        trackCamera.CameraAngle = similiarCamera.CameraAngle;
                }

                trackCameras.Add(trackCamera);
            }
        }

        void InitaliseListOfCameras()
        {
            cameraList.Items.Clear();
            raceStartCamera.Items.Clear();
            incidentCamera.Items.Clear();
            lastLapCamera.Items.Clear();

            var cameras = GetCamerasForSelectedTrack();
            foreach (var tc in cameras)
            {
                var lvi = new ListViewItem { Text = tc.CameraName };
                lvi.SubItems.Add(tc.Ratio.ToString());

                cameraList.Items.Add(lvi);

                raceStartCamera.Items.Add(tc.CameraName);
                incidentCamera.Items.Add(tc.CameraName);
                lastLapCamera.Items.Add(tc.CameraName);
            }

            SetCameraDropDown(cameras, trackCameras.RaceStart, "TV2", raceStartCamera);
            SetCameraDropDown(cameras, trackCameras.Incident, "TV3", incidentCamera);
            SetCameraDropDown(cameras, trackCameras.LastLap, "TV2", lastLapCamera);
        }

        void SetCameraDropDown(IEnumerable<TrackCamera> cameras, TrackCameras._CameraSelection specialCamera, string defaultName, ComboBox dropDown)
        {
            if (specialCamera[TrackName] == null && cameras.Any(tc => tc.CameraName == defaultName))
                dropDown.SelectedItem = defaultName;
            else
                dropDown.SelectedItem = specialCamera[TrackName];
        }

        void UpdateTotal()
        {
            int total = 0;
            foreach (var tc in GetCamerasForSelectedTrack())
            {
                total += tc.Ratio;
            }

            totalRatio.Text = total.ToString();
            TotalErrorLabel.Visible = total > 100;
            noCameraAssignedErrorLabel.Visible = total < 25;
        }

        void raceStartCamera_SelectedIndexChanged(object sender, EventArgs e)
        {
            trackCameras.RaceStart[TrackName] = raceStartCamera.SelectedItem.ToString();
            Settings.Default.Save();
        }

        void incidentCamera_SelectedIndexChanged(object sender, EventArgs e)
        {
            trackCameras.Incident[TrackName] = incidentCamera.SelectedItem.ToString();
            Settings.Default.Save();
        }

        void lastLapCamera_SelectedIndexChanged(object sender, EventArgs e)
        {
            trackCameras.LastLap[TrackName] = lastLapCamera.SelectedItem.ToString();
            Settings.Default.Save();
        }
    }
}
