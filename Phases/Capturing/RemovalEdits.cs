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
using System.Collections.Generic;

namespace iRacingReplayDirector.Phases.Capturing
{
    public enum InterestState { Unknown = -1, None = 0, FirstLap = 1, LastLap = 2, Incident = 3, Restart = 4, Battle = 5 /*, Overtake = 5, Pitstp = 6 */};

    public class RemovalEdits
    {
        readonly List<OverlayData.RaceEvent> raceEvents;

        Action<DataSample, TimeSpan> nextAction = (d, t) => { };
        TimeSpan lastRelativeTime;
        TimeSpan lastStartTime;
        bool withOvertake = false;
        int position = int.MaxValue;

        readonly Stack<InterestState> events = new Stack<InterestState>();

        public RemovalEdits(List<OverlayData.RaceEvent> raceEvents)
        {
            this.raceEvents = raceEvents;
        }

        public void InterestingThingStarted(InterestState interest, int pos)
        {
            var oldAction = nextAction;
            position = pos;

            nextAction = (d, t) =>
            {
                oldAction(d, t);

                if (events.Count > 0)
                {
                    var le = events.Peek();

                    AddEvent(le, d, t);
                }
                events.Push(interest);

                lastStartTime = t;
                TraceInfo.WriteLine("{0} Starting {1}", d.Telemetry.SessionTimeSpan, interest.ToString());
            };
        }

        public void InterestingThingStopped(InterestState interest)
        {
            nextAction = (d, t) =>
            {
                InterestState le = events.Count > 0 ? events.Pop() : InterestState.Unknown;
                if (le != interest)
                    throw new Exception("RaceEvent mismatched.  Attempted to close {0}, when expecting {1}".F(interest.ToString(), le.ToString()));

                AddEvent(interest, d, t);
            };
        }

        internal void WithOvertake()
        {
            withOvertake = true;
        }

        void AddEvent(InterestState interest, DataSample d, TimeSpan t)
        {
            raceEvents.Add(new OverlayData.RaceEvent
            {
                Interest = interest,
                StartTime = lastStartTime.TotalSeconds,
                EndTime = t.TotalSeconds,
                WithOvertake = withOvertake,
                Position = position,
                RaceLapNumber = d.Telemetry.Lap
            });
            lastStartTime = t;

            TraceInfo.WriteLine("{0} Stopping {1}{2}", d.Telemetry.SessionTimeSpan, interest.ToString(), withOvertake ? " - with Overtake" : "");
            withOvertake = false;
        }


        public void Process(DataSample data, TimeSpan relativeTime)
        {
            nextAction(data, relativeTime);
            nextAction = (d, t) => { };
            lastRelativeTime = relativeTime;
        }

        public void Stop()
        {
            raceEvents.Add(new OverlayData.RaceEvent
            {
                Interest = InterestState.LastLap,
                StartTime = lastStartTime.TotalSeconds,
                EndTime = lastRelativeTime.TotalSeconds
            });
        }

        public EditMarker For(InterestState interestState)
        {
            return new EditMarker(this, interestState);
        }
    }
}
