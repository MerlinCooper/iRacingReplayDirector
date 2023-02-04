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

using iRacingReplayDirector.Support;
using iRacingSDK;
using iRacingSDK.Support;
using System;
using System.Linq;

namespace iRacingReplayDirector.Phases.Capturing.LeaderBoard
{
    public class CaptureLeaderBoardLastLap
    {
        readonly OverlayData overlayData;
        readonly CaptureLeaderBoard captureLeaderBoard;
        readonly CommentaryMessages commentaryMessages;

        bool[] haveNotedCheckerdFlag = new bool[64];

        public CaptureLeaderBoardLastLap(CaptureLeaderBoard captureLeaderBoard, OverlayData overlayData, CommentaryMessages commentaryMessages)
        {
            this.captureLeaderBoard = captureLeaderBoard;
            this.overlayData = overlayData;
            this.commentaryMessages = commentaryMessages;
        }

        public void Process(DataSample data, TimeSpan relativeTime, ref OverlayData.LeaderBoard leaderBoard)
        {
            var session = data.SessionData.SessionInfo.Sessions[data.Telemetry.SessionNum];

            leaderBoard = captureLeaderBoard.CreateLeaderBoard(data, relativeTime, leaderBoard.Drivers);

            for (int i = 1; i < data.SessionData.DriverInfo.CompetingDrivers.Length; i++)
            {
                AnnounceIfDriverHasFinished(data, relativeTime, session, i, ref leaderBoard);
                MarkResultFlashCardStart(data, relativeTime, session, i);
            }

            overlayData.LeaderBoards.Add(leaderBoard);
        }

        void MarkResultFlashCardStart(DataSample data, TimeSpan relativeTime, SessionData._SessionInfo._Sessions session, int i)
        {
            if (!data.LastSample.Telemetry.Cars[i].HasSeenCheckeredFlag || overlayData.TimeForOutroOverlay != null)
                return;

            var position = (int)session.ResultsPositions.First(r => r.CarIdx == i).Position;

            if (position == Settings.Default.ResultsFlashCardPosition)
            {
                overlayData.TimeForOutroOverlay = relativeTime.TotalSeconds;
                TraceInfo.WriteLine("{0} Mark show results flash card.", data.Telemetry.SessionTimeSpan);
            }
        }

        void AnnounceIfDriverHasFinished(DataSample data, TimeSpan relativeTime, SessionData._SessionInfo._Sessions session, int i, ref OverlayData.LeaderBoard leaderBoard)
        {
            if (!data.LastSample.Telemetry.Cars[i].HasSeenCheckeredFlag || haveNotedCheckerdFlag[i])
                return;

            haveNotedCheckerdFlag[i] = true;

            var driver = data.SessionData.DriverInfo.CompetingDrivers[i];
            var position = (int)session.ResultsPositions.First(r => r.CarIdx == i).Position;
            var pitStopCount = data.Telemetry.Cars[i].PitStopCount;

            var drivers = leaderBoard.Drivers.Where(d => d.CarIdx != i)
                .Select(d => d.Clone())
                .ToList();

            drivers.Insert((int)position - 1, new OverlayData.Driver
            {
                CarNumber = driver.CarNumber,
                UserName = driver.UserName,
                Position = position,
                CarIdx = i,
                PitStopCount = pitStopCount

            });

            var p = 1;
            foreach (var d in drivers)
                d.Position = p++;

            leaderBoard = captureLeaderBoard.CreateLeaderBoard(data, relativeTime, drivers.ToArray());

            var msg = string.Format("{0} finished in {1}{2}", driver.UserName, position, position.Ordinal());
            TraceInfo.WriteLine("{0} {1}", data.Telemetry.SessionTimeSpan, msg);
            commentaryMessages.Add(msg, relativeTime.TotalSeconds);
        }
    }
}
