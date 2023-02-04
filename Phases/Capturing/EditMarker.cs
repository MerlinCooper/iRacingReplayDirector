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

namespace iRacingReplayDirector.Phases.Capturing
{
    public class EditMarker
    {
        readonly RemovalEdits removalEdits;
        readonly InterestState interest;

        bool wasStarted;

        public EditMarker(RemovalEdits removalEdits, InterestState interest)
        {
            this.removalEdits = removalEdits;
            this.interest = interest;
        }

        public InterestState getInterestState() { return interest; }

        internal void Start(int pos = int.MaxValue)
        {
            if (wasStarted)
                removalEdits.InterestingThingStopped(interest);

            wasStarted = true;
            removalEdits.InterestingThingStarted(interest, pos);
        }

        internal void Stop()
        {
            if (!wasStarted)
                return;

            removalEdits.InterestingThingStopped(interest);
            wasStarted = false;
        }

        internal void WithOvertake()
        {
            removalEdits.WithOvertake();
        }
    }
}
