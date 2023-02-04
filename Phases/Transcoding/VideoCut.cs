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

using iRacingSDK.Support;
using System;

namespace iRacingReplayDirector.Phases.Transcoding
{
    public class VideoEdit
    {
        public double StartTime;
        public double EndTime;

        public double Duration { get { return EndTime - StartTime; } }
        public TimeSpan StartTimeSpan { get { return StartTime.Seconds(); } }
        public TimeSpan EndTimeSpan { get { return EndTime.Seconds(); } }
        public TimeSpan DurationSpan { get { return TimeSpan.FromSeconds(Duration); } }
    }
}
