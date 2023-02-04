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
//

using System;
using System.Collections.Generic;
using System.Linq;

namespace iRacingReplayDirector
{
    public enum CameraAngle
    {
        LookingInfrontOfCar,
        LookingBehindCar,
        LookingAtCar,
        LookingAtTrack
    }

    public class TrackCameras : List<TrackCamera>
    {
        public class _CameraSelection
        {
            TrackCameras trackCameras;
            Func<TrackCamera, bool> readAttr;
            Action<TrackCamera, bool> writeAttr;

            public _CameraSelection(TrackCameras trackCameras, Func<TrackCamera, bool> readAttr, Action<TrackCamera, bool> writeAttr)
            {
                this.trackCameras = trackCameras;
                this.readAttr = readAttr;
                this.writeAttr = writeAttr;
            }

            public string this[string trackName]
            {
                get
                {
                    var c = trackCameras.FirstOrDefault(tc => tc.TrackName == trackName && readAttr(tc));

                    return c == null ? null : c.CameraName;
                }
                set
                {
                    foreach (var tc in trackCameras.Where(tc => tc.TrackName == trackName && readAttr(tc)))
                        writeAttr(tc, false);

                    var c = trackCameras.FirstOrDefault(tc => tc.TrackName == trackName && tc.CameraName == value);

                    writeAttr(c, true);
                }
            }
        }

        public _CameraSelection RaceStart
        {
            get
            {
                return new _CameraSelection(this, tc => tc.IsRaceStart, (tc, v) => tc.IsRaceStart = v);
            }
        }

        public _CameraSelection Incident
        {
            get
            {
                return new _CameraSelection(this, tc => tc.IsIncident, (tc, v) => tc.IsIncident = v);
            }
        }

        public _CameraSelection LastLap
        {
            get
            {
                return new _CameraSelection(this, tc => tc.IsLastLap, (tc, v) => tc.IsLastLap = v);
            }
        }

        public override string ToString()
        {
            return String.Join("\n", this.Select(c => c.ToString()));
        }
    }

    public class TrackCamera
    {
        static Dictionary<string, CameraAngle> cameraAngles = new Dictionary<string, CameraAngle>
        {
            { "Nose", CameraAngle.LookingInfrontOfCar },
            { "Gearbox", CameraAngle.LookingBehindCar },
            { "Roll Bar", CameraAngle.LookingInfrontOfCar },
            { "LF Susp", CameraAngle.LookingInfrontOfCar },
            { "RF Susp", CameraAngle.LookingInfrontOfCar },
            { "LR Susp", CameraAngle.LookingBehindCar },
            { "RR Susp", CameraAngle.LookingBehindCar },
            { "Gyro", CameraAngle.LookingInfrontOfCar },
            { "Cockpit", CameraAngle.LookingInfrontOfCar },
            { "Blimp", CameraAngle.LookingAtCar },
            { "Chopper", CameraAngle.LookingAtCar },
            { "Chase", CameraAngle.LookingInfrontOfCar },
            { "Rear Chase", CameraAngle.LookingBehindCar },
            { "Far Chase", CameraAngle.LookingAtCar },
            { "TV1", CameraAngle.LookingAtCar },
            { "TV2", CameraAngle.LookingAtCar },
            { "TV3", CameraAngle.LookingAtCar },
            { "Pit Lane", CameraAngle.LookingAtTrack },
            { "Pit Lane 2", CameraAngle.LookingAtTrack }
        };

        public string TrackName;
        public string CameraName;
        public int Ratio;
        public short CameraNumber;
        public bool IsRaceStart;
        public bool IsIncident;
        public bool IsLastLap;
        public CameraAngle? cameraAngle;

        public override string ToString()
        {
            return string.Format("{0} - {1}. Ratio: {2}, Number: {3}, IsRaceStart: {4}, IsIncident: {5}, IsLastLap: {6}, Angle: {7}",
                TrackName, CameraName, Ratio, CameraNumber, IsRaceStart, IsIncident, IsLastLap, CameraAngle);
        }

        public CameraAngle CameraAngle
        {
            get
            {
                if (cameraAngle.HasValue)
                    return cameraAngle.Value;

                CameraAngle _cameraAngle;

                if (cameraAngles.TryGetValue(CameraName, out _cameraAngle))
                    return _cameraAngle;
                else
                    return CameraAngle.LookingAtCar;
            }
            set
            {
                cameraAngle = value;
            }
        }
    }
}
