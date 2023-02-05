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
// You should have received a copy of the GNU General Public License333
// along with iRacingReplayDirector.  If not, see <http://www.gnu.org/licenses/>.

using iRacingSDK;
using iRacingSDK.Support;
using System;
using System.Linq;

namespace iRacingReplayDirector.Phases.Capturing
{
    public class LogCamDriver
    {
        public LogCamDriver()
        {
        }

        public void Process(DataSample data, TimeSpan relativeTime)
        {
            try
            {
                var cameraGroupName = "";
                var cameraName = "";

                var cameraGroup = data.SessionData.CameraInfo.Groups.FirstOrDefault(g => g.GroupNum == data.Telemetry.CamGroupNumber);
                if (cameraGroup != null)
                {
                    cameraGroupName = cameraGroup.GroupName;
                    var camera = cameraGroup.Cameras.FirstOrDefault(c => c.CameraNum == data.Telemetry.CamCameraNumber);
                    if (camera != null)
                        cameraName = camera.CameraName;
                }

                TraceDebug.WriteLine("{0} Camera: Driver: {1}, GroupNumber: {2}, Number: {3}, State: {4}, GroupName: {5}, Name: {6}",
                    data.Telemetry.SessionTimeSpan,
                    data.Telemetry.CamCar.Details.Driver.UserName,
                    data.Telemetry.CamGroupNumber,
                    data.Telemetry.CamCameraNumber,
                    data.Telemetry.CamCameraState,
                    cameraGroupName,
                    cameraName);
            }
            catch (Exception e)
            {
                TraceError.WriteLine(e.Message);
                TraceError.WriteLine(e.StackTrace);
            }
        }
    }
}
