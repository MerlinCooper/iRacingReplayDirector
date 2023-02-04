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
using System;
using System.Linq;

namespace iRacingReplayDirector.Phases
{
    public partial class IRacingReplay
    {
        TrackCameras trackCameras;
        public TrackCameras TrackCameras
        {
            get
            {
                return trackCameras ?? Settings.Default.trackCameras;
            }
        }

        public IRacingReplay WithCameras(TrackCameras trackCameras)
        {
            this.trackCameras = trackCameras;

            return this;
        }

        public void _WhenIRacingStarts(Action onComplete)
        {
            foreach (var data in iRacing.GetDataFeed().TakeWhile(d => !d.IsConnected))
            {
                if (requestAbort)
                    return;

                continue;
            }

            onComplete();
        }
    }
}
