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
using System;
using System.Linq;

namespace iRacingReplayDirector.Phases.Capturing.LeaderBoard
{
    public class CaptureLeaderBoardFirstLap
    {
        readonly OverlayData overlayData;
        readonly CaptureLeaderBoard captureLeaderBoard;

        public CaptureLeaderBoardFirstLap(CaptureLeaderBoard captureLeaderBoard, OverlayData overlayData)
        {
            this.captureLeaderBoard = captureLeaderBoard;
            this.overlayData = overlayData;
        }

        public void Process(DataSample data, TimeSpan relativeTime, ref OverlayData.LeaderBoard leaderBoard)
        {
            leaderBoard = captureLeaderBoard.CreateLeaderBoard(data, relativeTime, GetQualifyingOrder(data));

            overlayData.LeaderBoards.Add(leaderBoard);
        }

        static OverlayData.Driver[] GetQualifyingOrder(DataSample data)
        {
            var session = data.SessionData.SessionInfo.Sessions.Qualifying();

            if (session == null || session.ResultsPositions == null)
                return new OverlayData.Driver[0];

            return session
                .ResultsPositions
                .Where(rp => rp.CarIdx < data.SessionData.DriverInfo.CompetingDrivers.Length)
                .Select((rp, i) => new OverlayData.Driver
                {
                    CarIdx = (int)rp.CarIdx,
                    Position = i + 1,
                    CarNumber = data.SessionData.DriverInfo.CompetingDrivers[rp.CarIdx].CarNumber,
                    UserName = data.SessionData.DriverInfo.CompetingDrivers[rp.CarIdx].UserName,
                    PitStopCount = data.Telemetry.CarIdxPitStopCount[rp.CarIdx]
                })
                .ToArray();
        }
    }
}
