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

using iRacingReplayDirector.Phases.Capturing;
using iRacingSDK;
using iRacingSDK.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace iRacingReplayDirector.Phases.Direction
{
    public class CameraControl
    {
        readonly TrackCamera[] cameras;
        readonly Random random;
        readonly TrackCamera defaultCamera;
        OverlayData overlayData;


        public short LastLapCameraNumber
        {
            get
            {
                return cameras.First(tc => tc.IsLastLap).CameraNumber;
            }
        }

        public short IncidentCameraNumber
        {
            get
            {
                return cameras.First(tc => tc.IsIncident).CameraNumber;
            }
        }

        public short RaceStartCameraNumber
        {
            get
            {
                return cameras.First(tc => tc.IsRaceStart).CameraNumber;
            }
        }

        public CameraControl(TrackCamera[] cameras)
        {
            this.cameras = cameras;
            this.random = new Random();

            defaultCamera = cameras.First(tc => tc.IsRaceStart);
        }

        public TrackCamera FindACamera(IEnumerable<CameraAngle> cameraAngles, TrackCamera adjustedCamera = null, int adjustRatioBy = 1)
        {
            if (adjustedCamera != null)
                TraceInfo.WriteLine("Adjusting ratio for camera {0} by 1/{1}", adjustedCamera.CameraName, adjustRatioBy);

            var rand = 0;
            var offset = 0;
            var camera = defaultCamera;

            var selectableCameras = cameras.Where(x => cameraAngles.Contains(x.CameraAngle));
            int total = selectableCameras.Sum(x => x == adjustedCamera ? x.Ratio / adjustRatioBy : x.Ratio);

            // If no camera within specified cameraAngles has non zero ratio select among all
            if (total == 0)
            {
                selectableCameras = cameras;
                rand = random.Next(100);
            }
            else
                rand = random.Next(total);

            foreach (var tc in selectableCameras)
            {
                var ratio = tc == adjustedCamera ? tc.Ratio / adjustRatioBy : tc.Ratio;

                if (rand < ratio + offset)
                {
                    camera = tc;
                    break;
                }
                offset += ratio;
            }

            return camera;
        }

        //Start worker thread to switch cameras as stored in camDriversList during race analyis when FastRecording is used. 
        public async Task ReplayCameraControlTask(OverlayData overlayData)
        {
            this.overlayData = overlayData;
            await Task.Run(() => CameraSwitcherWorkerTask());
        }

        //worker thread to switch cameras.
        private void CameraSwitcherWorkerTask()
        {
            for (var iCamDriverIndex = 0; iCamDriverIndex < overlayData.CamDrivers.Count; iCamDriverIndex++)
            {
                //get object of current CamDriver
                var curCamDriver = overlayData.CamDrivers[iCamDriverIndex];
                //Set camera to current camDriver Object
                ReplayControl.cameraControl.CameraOnDriver(short.Parse(curCamDriver.CurrentDriver.CarNumber), (short)curCamDriver.camGroupNumber);
                //remember start-time of current camers
                double startTimeCurCamera = curCamDriver.StartTime;
                try
                {
                    double startTimeNextCamera = overlayData.CamDrivers[iCamDriverIndex + 1].StartTime;
                    int camDuration = (int)(1000 * (startTimeNextCamera - startTimeCurCamera));
                    TraceDebug.WriteLine("Camera switched to driver {0} using camera-group {1}. Remaining Time: {2}".F(curCamDriver.CurrentDriver.UserName, curCamDriver.camGroupNumber, camDuration));
                    Thread.Sleep(camDuration);
                }
                catch
                {
                    TraceDebug.Write("No more entries in list of CamDriver -> no more camera switches");
                }
            }
        }

        public void CameraOnDriver(short carNumber, short group, short camera = 0)
        {
            iRacing.Replay.CameraOnDriver(carNumber, group, camera);
            //TraceDebug.WriteLine("CameraOnDriver called for carNumber: {0} cameraGroup: {1} camera: {2}".F(carNumber, group, camera));
        }

        public void CameraOnPositon(short carPosition, short group, short camera = 0)
        {
            iRacing.Replay.CameraOnPositon(carPosition, group, camera);
            //TraceDebug.WriteLine("CameraOnPositon called for carPosition: {0} cameraGroup: {1} camera: {2}".F(carPosition, group, camera));
        }
    }
}
