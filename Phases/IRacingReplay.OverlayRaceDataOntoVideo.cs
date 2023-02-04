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

using MediaFoundation.Net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace iRacingReplayDirector.Phases
{
    public partial class IRacingReplay
    {
        int videoBitRate;
        string destinationFile;
        string destinationHighlightsFile;
        string gameDataFile;

        public void _WithEncodingOf(int videoBitRate)
        {
            this.videoBitRate = videoBitRate;
        }

        public void _WithOverlayFile(string overlayFileName)
        {
            destinationFile = Path.ChangeExtension(overlayFileName, "wmv");
            destinationHighlightsFile = Path.ChangeExtension(overlayFileName, ".highlights.wmv");

            gameDataFile = overlayFileName;
        }

        public void _OverlayRaceDataOntoVideo(Action<long, long> progress, Action completed, bool highlightOnly, bool bShutdownAfterCompleted, CancellationToken token)
        {
            bool TranscodeFull = !highlightOnly;

            var transcodeHigh = new Task(() => TranscodeAndOverlayMarshaled.Apply("HighLights", gameDataFile, videoBitRate, destinationHighlightsFile, true, highlightOnly ? progress : null, token));
            var transcodeFull = new Task(() => TranscodeAndOverlayMarshaled.Apply("Full", gameDataFile, videoBitRate, destinationFile, false, progress, token));

            using (MFSystem.Start())
            {
                var waits = new List<Task>();

                transcodeHigh.Start();
                waits.Add(transcodeHigh);

                //Seem to have some kind of bug in MediaFoundation - where if two threads attempt to open source Readers to the same file, we get exception raised.
                //To work around issue, delay the start of the second transcoder - so we dont have two threads opening at the same time.
                if (TranscodeFull)
                {
                    Thread.Sleep(10000);
                    transcodeFull.Start();
                    waits.Add(transcodeFull);
                }

                Task.WaitAll(waits.ToArray());
            }
            completed();

            //Shutdown PC after Transcoding is completed (MCooper)
            if (bShutdownAfterCompleted)
            {
                var psi = new ProcessStartInfo("shutdown", "/s /f /t 0");
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                Process.Start(psi);
            }
        }
    }
}
