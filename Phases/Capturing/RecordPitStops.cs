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

using iRacingSDK;
using iRacingSDK.Support;
using System;
using System.Linq;

namespace iRacingReplayDirector.Phases.Capturing
{
    public class RecordPitStop
    {
        readonly CommentaryMessages commentaryMessages;

        enum PitStopState { None, Entering, Inside, Exiting };
        PitStopState[] pitStopState = new PitStopState[64];

        public RecordPitStop(CommentaryMessages commentaryMessages)
        {
            this.commentaryMessages = commentaryMessages;
        }

        public void Process(DataSample data, TimeSpan relativeTime)
        {
            foreach (var car in data.Telemetry.Cars.Where(c => !c.Details.IsPaceCar))
            {
                if (pitStopState[car.CarIdx] == PitStopState.None && car.TrackSurface == TrackLocation.InPitStall)
                {
                    pitStopState[car.CarIdx] = PitStopState.Entering;
                    var msg = "{0} has pitted".F(car.Details.UserName);
                    TraceInfo.WriteLine("{0} {1}", data.Telemetry.SessionTimeSpan, msg);
                    commentaryMessages.Add(msg, relativeTime);
                }

                if (pitStopState[car.CarIdx] == PitStopState.Entering && car.TrackSurface != TrackLocation.InPitStall)
                    pitStopState[car.CarIdx] = PitStopState.None;
            }
        }
    }
}
