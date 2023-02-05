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
using System.Diagnostics;
using System.Linq;

namespace iRacingReplayDirector.Phases.Direction
{
    public class RuleLastLapPeriod : IDirectionRule
    {
        readonly EditMarker editMarker;
        readonly CameraControl cameraControl;

        int lastFinisherCarIdx = -1;
        DateTime timeOfFinisher = DateTime.Now;
        bool lastLapHasStarted;
        TimeSpan trackLeaderFrom;
        bool startLastLapPeriod = false;

        public RuleLastLapPeriod(CameraControl cameraControl, RemovalEdits removalEdits)
        {
            this.editMarker = removalEdits.For(InterestState.LastLap);
            this.cameraControl = cameraControl;
        }

        public bool IsActive(DataSample data)
        {
            if (!lastLapHasStarted && data.Telemetry.RaceLaps == data.Telemetry.Session.ResultsLapsComplete)
            {
                lastLapHasStarted = true;
                trackLeaderFrom = data.Telemetry.SessionTimeSpan + (data.Telemetry.Session.ResultsAverageLapTime.Seconds() - Settings.Default.FollowLeaderBeforeRaceEndPeriod);
                TraceInfo.WriteLine("{0} On final lap.  Tracking leader from {1}", data.Telemetry.SessionTimeSpan, trackLeaderFrom);
            }

            var isInLastPeriod = lastLapHasStarted && data.Telemetry.SessionTimeSpan > trackLeaderFrom;

            if (isInLastPeriod)
                OnlyOnce(ref startLastLapPeriod, () =>
                {
                    editMarker.Start();
                    TraceInfo.WriteLine("{0} Tracking leader on final lap", data.Telemetry.SessionTimeSpan);
                });

            return isInLastPeriod;
        }

        public void Direct(DataSample data)
        {
            SwitchToFinishingDrivers(data);
        }

        void SwitchToFinishingDrivers(DataSample data)
        {
            var session = data.SessionData.SessionInfo.Sessions[data.Telemetry.SessionNum];

            if (lastFinisherCarIdx != -1 && !data.Telemetry.Cars[lastFinisherCarIdx].HasSeenCheckeredFlag)
            {
                timeOfFinisher = DateTime.Now.AddSeconds(2.0 * Settings.AppliedTimingFactor);
                return;
            }

            if (timeOfFinisher > DateTime.Now)
                return;

            Car nextFinisher;

            if (!data.Telemetry.LeaderHasFinished)
                nextFinisher = data.Telemetry.Cars
                    .OrderBy(c => c.Position)
                    .Where(c => c.Details.Driver != null)
                    .Where(c => c.HasData)
                    .First();
            else
                nextFinisher = data.Telemetry.Cars
                        .Where(c => c.TotalDistance > 0)
                        .Where(c => !c.HasSeenCheckeredFlag)
                        .Where(c => !c.Details.IsPaceCar)
                        .Where(c => c.HasData)
                        .Where(c => c.Details.Driver != null)
                        .Where(c => c.TrackSurface == TrackLocation.OnTrack)
                        .OrderByDescending(c => c.DistancePercentage)
                        .ThenBy(c => c.OfficialPostion == 0 ? int.MaxValue : c.OfficialPostion)
                        .FirstOrDefault();

            if (nextFinisher == null)
            {
                Trace.WriteLine("{0} Found no more finishers.".F(data.Telemetry.SessionTimeSpan), "DEBUG");
                return;
            }

            Trace.WriteLine("{0} Found {1} in position {2}".F(data.Telemetry.SessionTimeSpan, nextFinisher.Details.UserName, nextFinisher.Position), "DEBUG");

            timeOfFinisher = DateTime.Now;
            lastFinisherCarIdx = nextFinisher.CarIdx;

            TraceInfo.WriteLine("{0} Switching camera to {1} as they cross finishing line in position {2}", data.Telemetry.SessionTimeSpan, nextFinisher.Details.UserName, nextFinisher.Position);

            cameraControl.CameraOnDriver(nextFinisher.Details.CarNumberRaw, cameraControl.LastLapCameraNumber);
        }

        void OnlyOnce(ref bool latch, Action action)
        {
            if (!latch)
                action();

            latch = true;
        }

        public string Name
        {
            get { return GetType().Name; }
        }
    }
}
