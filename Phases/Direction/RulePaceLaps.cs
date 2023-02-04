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

namespace iRacingReplayDirector.Phases.Direction
{
    public class RulePaceLaps : IVetoRule
    {
        readonly EditMarker restartMarker;
        readonly EditMarker battleMarker;
        readonly CameraControl cameraControl;

        bool wasUnderPaceCar;
        TimeSpan restartEndTime;
        readonly TimeSpan RestartStickyTime = 20.Seconds();
        bool restarting = false;

        public RulePaceLaps(CameraControl cameraControl, EditMarker restartMarker, EditMarker battleMarker)
        {
            this.cameraControl = cameraControl;
            this.restartMarker = restartMarker;
            this.battleMarker = battleMarker;

            wasUnderPaceCar = false;
        }

        public bool IsActive(DataSample data)
        {
            if (restarting)
            {
                if (data.Telemetry.SessionTimeSpan < restartEndTime)
                    return true;

                restartMarker.Stop();

                restarting = false;
                return false;
            }

            if (wasUnderPaceCar)
            {
                if (data.Telemetry.UnderPaceCar)
                    return true;

                restartEndTime = data.Telemetry.SessionTimeSpan + RestartStickyTime;
                restarting = true;

                TraceInfo.WriteLine("{0} Race restarting", data.Telemetry.SessionTimeSpan);
                wasUnderPaceCar = false;
                restartMarker.Start();
                return true;
            }

            wasUnderPaceCar = data.Telemetry.UnderPaceCar;
            if (wasUnderPaceCar)
            {
                TraceInfo.WriteLineIf(wasUnderPaceCar, "{0} Double Yellows. Pace Car", data.Telemetry.SessionTimeSpan);
                battleMarker.Stop();
                cameraControl.CameraOnPositon(1, cameraControl.RaceStartCameraNumber);
            }

            return wasUnderPaceCar;
        }

        public void Direct(DataSample data)
        {
        }

        public void Redirect(DataSample data)
        {
            cameraControl.CameraOnPositon(1, cameraControl.RaceStartCameraNumber);
        }

        public string Name
        {
            get { return GetType().Name; }
        }
    }
}
