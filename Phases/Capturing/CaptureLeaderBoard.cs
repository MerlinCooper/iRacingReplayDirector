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

using iRacingReplayDirector.Phases.Capturing.LeaderBoard;
using iRacingSDK;
using System;

namespace iRacingReplayDirector.Phases.Capturing
{
    public class CaptureLeaderBoard
    {
        readonly OverlayData overlayData;
        readonly CaptureLeaderBoardFirstLap captureLeaderBoardFirstLap;
        readonly CaptureLeaderBoardLastLap captureLeaderBoardLastLap;
        readonly CaptureLeaderBoardMiddleLaps captureLeaderBoardMiddleLaps;

        OverlayData.LeaderBoard leaderBoard;
        double raceStartTimeOffset = 0;

        public CaptureLeaderBoard(OverlayData overlayData, CommentaryMessages commentaryMessages, RemovalEdits removalEdits)
        {
            this.overlayData = overlayData;
            this.captureLeaderBoardFirstLap = new CaptureLeaderBoardFirstLap(this, overlayData);
            this.captureLeaderBoardLastLap = new CaptureLeaderBoardLastLap(this, overlayData, commentaryMessages);
            this.captureLeaderBoardMiddleLaps = new CaptureLeaderBoardMiddleLaps(this, overlayData, removalEdits, commentaryMessages);
        }

        public void Process(DataSample data, TimeSpan relativeTime)
        {
            if (raceStartTimeOffset == 0 && data.Telemetry.SessionState == SessionState.Racing)
                raceStartTimeOffset = data.Telemetry.SessionTime;

            if (ProcessForStarting(data))
            {
                captureLeaderBoardFirstLap.Process(data, relativeTime, ref leaderBoard);
                return;
            }

            if (ProcessForLastLap(data))
            {
                captureLeaderBoardLastLap.Process(data, relativeTime, ref leaderBoard);
                return;
            }

            captureLeaderBoardMiddleLaps.Process(data, relativeTime, ref leaderBoard);
        }

        static bool ProcessForStarting(DataSample data)
        {
            return data.Telemetry.RaceDistance <= 1.10;
        }

        static bool ProcessForLastLap(DataSample data)
        {
            return data.Telemetry.LeaderHasFinished;
        }

        internal OverlayData.LeaderBoard CreateLeaderBoard(DataSample data, TimeSpan relativeTime, OverlayData.Driver[] drivers)
        {
            var session = data.SessionData.SessionInfo.Sessions[data.Telemetry.SessionNum];


            var timespan = raceStartTimeOffset == 0 ? TimeSpan.FromSeconds(session._SessionTime / 1E4) : TimeSpan.FromSeconds(data.Telemetry.SessionTimeRemain + raceStartTimeOffset);
            var raceLapsPosition = string.Format("Lap {0}/{1}", data.Telemetry.RaceLaps, session.ResultsLapsComplete);
            var raceTimePosition = (timespan.TotalSeconds <= 0 ? TimeSpan.FromSeconds(0) : timespan).ToString(@"hh\:mm\:ss");
            var raceLapCounter = string.Format("Lap {0}", data.Telemetry.RaceLaps);

            OverrideRacePositionDetails(data, session, ref raceLapsPosition, ref raceLapCounter);

            return new OverlayData.LeaderBoard
            {
                StartTime = relativeTime.TotalSeconds,
                Drivers = drivers,
                RacePosition = session.IsLimitedSessionLaps ? raceLapsPosition : raceTimePosition,
                LapCounter = session.IsLimitedSessionLaps ? null : raceLapCounter
            };
        }

        static void OverrideRacePositionDetails(DataSample data, SessionData._SessionInfo._Sessions session, ref string raceLapsPosition, ref string raceLapCounter)
        {
            if (data.Telemetry.RaceLaps <= 0)
            {
                raceLapCounter = null;
                raceLapsPosition = "";
                return;
            }

            if (data.Telemetry.RaceLaps < session.ResultsLapsComplete)
                return;

            if (data.Telemetry.RaceLaps == session.ResultsLapsComplete)
            {
                raceLapsPosition = raceLapCounter = "Final Lap";
                return;
            }

            raceLapsPosition = raceLapCounter = "Results";
        }
    }
}
