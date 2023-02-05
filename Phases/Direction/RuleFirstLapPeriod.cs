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
using System.Linq;

namespace iRacingReplayDirector.Phases.Direction
{
    public class RuleFirstLapPeriod : IVetoRule
    {
        readonly CameraControl cameraControl;
        readonly EditMarker editMarker;

        DateTime reselectLeaderAt = DateTime.Now;
        bool startedFirstLapPeriod = false;
        bool completedFirstLapPeriod = false;
        bool raceHasStarted = false;
        TimeSpan raceStartTime;

        public RuleFirstLapPeriod(CameraControl cameraControl, RemovalEdits removalEdits)
        {
            editMarker = removalEdits.For(InterestState.FirstLap);
            this.cameraControl = cameraControl;
        }

        public bool IsActive(DataSample data)
        {
            var isInFirstPeriod = InFirstLapPeriod(data);

            if (isInFirstPeriod)
                OnlyOnce(ref startedFirstLapPeriod, () =>
                {
                    editMarker.Start();
                    TraceInfo.WriteLine("{0} Tracking leader from race start for period of {1}", data.Telemetry.SessionTimeSpan, Settings.Default.FollowLeaderAtRaceStartPeriod);
                });
            else
                OnlyOnce(ref completedFirstLapPeriod, () =>
                {
                    editMarker.Stop();
                    TraceInfo.WriteLine("{0} Leader has completed first lap period.  Activating normal camera/driver selection rules.", data.Telemetry.SessionTimeSpan);
                });

            return isInFirstPeriod;
        }

        public void Direct(DataSample data)
        {
            SwitchToLeader(data);
        }

        public void Redirect(DataSample data)
        {
            reselectLeaderAt = DateTime.Now - 1.Second();
            Direct(data);
        }

        bool InFirstLapPeriod(DataSample data)
        {
            if (!raceHasStarted)
            {
                raceHasStarted = data.Telemetry.SessionState == SessionState.Racing;
                raceStartTime = data.Telemetry.SessionTimeSpan;
                return true;
            }

            return data.Telemetry.SessionTimeSpan < raceStartTime + Settings.Default.FollowLeaderAtRaceStartPeriod;
        }

        void SwitchToLeader(DataSample data)
        {
            if (reselectLeaderAt < DateTime.Now)
            {
                int posLeaderOnTrack = 1;
                var leader = data.Telemetry.Cars.First(c => c.Position == posLeaderOnTrack);

                //InterestState curState = editMarker.getInterestState();
                if (editMarker.getInterestState() == InterestState.FirstLap)
                {

                    ////if in first lap make sure that first car on track is selected as leader
                    while (leader.IsInPits)
                    {
                        posLeaderOnTrack += 1;
                        leader = data.Telemetry.Cars[posLeaderOnTrack];
                        //leader = data.Telemetry.Cars.First(c => c.Position == posLeaderOnTrack);
                    }
                    ;
                }


                cameraControl.CameraOnDriver(leader.Details.CarNumberRaw, cameraControl.RaceStartCameraNumber);

                reselectLeaderAt = DateTime.Now + 1.5.Seconds();
            }
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
