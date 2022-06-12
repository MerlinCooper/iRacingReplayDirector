// This file is part of iRacingReplayDirector.
//
// Copyright 2014 Dean Netherton
// https://github.com/vipoo/iRacingReplayDirector.net
//
// Copyright 2020 Merlin Cooper 
// https://github.com/MerlinCooper/iRacingReplayDirector
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

using iRacingReplayDirector.Phases.Analysis;
using iRacingReplayDirector.Phases.Capturing;
using iRacingReplayDirector.Phases.Direction;
using iRacingReplayDirector.Support;
using iRacingSDK;
using iRacingSDK.Support;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Collections.Generic;


namespace iRacingReplayDirector.Phases
{
    public partial class IRacingReplay
    {
        int raceStartFrameNumber = 0;
        double raceStartSessionTime = 0.0;
        internal Incidents incidents;

        enum replaySpeeds : int
        {
            pause = 0,
            normal = 1,
            FF2x = 2,
            FF4x = 4, 
            FF8x = 8,
            FF12x = 12,
            FF16x = 16
        };

        //create classes needed to analze race as global variables in the iRacingReplay instance 
        internal OverlayData overlayData = new OverlayData();
        internal RemovalEdits removalEdits;
        internal CommentaryMessages commentaryMessages;
        internal RecordPitStop recordPitStop;
        internal RecordFastestLaps fastestLaps;
        internal ReplayControl replayControl;
        internal SessionDataCapture sessionDataCapture;
        internal SampleFilter captureLeaderBoardEveryHalfSecond;
        internal SampleFilter captureCamDriverEveryQuaterSecond;
        internal SampleFilter captureCamDriverEvery4Seconds;


        public void _AnalyseRace(Action onComplete)
        {
            var hwnd = Win32.Messages.FindWindow(null, "iRacing.com Simulator");
            Win32.Messages.ShowWindow(hwnd, Win32.Messages.SW_SHOWNORMAL);
            Win32.Messages.SetForegroundWindow(hwnd);
            Thread.Sleep(Settings.Default.PeriodWaitForIRacingSwitch);

            var data = iRacing.GetDataFeed()
                .WithCorrectedPercentages()
                .AtSpeed(16)
                .RaceOnly()
                .First(d => d.Telemetry.SessionState == SessionState.Racing);

            raceStartFrameNumber = data.Telemetry.ReplayFrameNum - (60 * 20);
            raceStartSessionTime = raceStartSessionTime < 20 ? (data.Telemetry.SessionTime - 20) : 0.0;

            if (raceStartFrameNumber < 0 )
            {
                TraceInfo.WriteLine("Unable to start capturing at 20 seconds prior to race start.  Starting at start of replay file.");
                raceStartFrameNumber = 0;
            }

            TraceDebug.WriteLine(data.Telemetry.ToString());
            
            AnalyseIncidents();                                                         //Analyse incidents
            AnalyseRaceSituations(new iRacingConnection().GetBufferedDataFeed());       //Analyse race situation (all) by playing out replay at 16x speed. 

            onComplete();
        }

        void AnalyseIncidents()
        {
            iRacing.Replay.MoveToFrame(raceStartFrameNumber);

            incidents = new Incidents();

            if (!Settings.Default.DisableIncidentsSearch)
            {
                var incidentSamples = iRacing.GetDataFeed().RaceIncidents2(Settings.Default.IncidentScanWait, shortTestOnly ? 12 : int.MaxValue);

                foreach (var data in incidentSamples)
                    incidents.Process(data);
            }
        }

        //Analyse race situations at maximum replay speed w/o recording.  
        void AnalyseRaceSituations(IEnumerable<DataSample> samples)
        {
            TraceDebug.WriteLine("ADV_RECORDING: AnalyseRaceSituations started");
            int iReplaySpeedForAnalysis = (int)replaySpeeds.FF16x;                                              //set speed for analysis phase to FF16x

            //Start iRacing Replay from the beginning with maximum speed (16x)
            iRacing.Replay.MoveToFrame(raceStartFrameNumber);

            //----------------------------
            // copied from iRacing.Capturing because race events in app V1.0.x.x are identified during capturing the whole video. 
            // necessity of classes in analysis phase to be reviewed
            //----------------------------
            //var overlayData = new OverlayData();
            removalEdits = new RemovalEdits(overlayData.RaceEvents);
            commentaryMessages = new CommentaryMessages(overlayData);   
            recordPitStop = new RecordPitStop(commentaryMessages);
            fastestLaps = new RecordFastestLaps(overlayData);
            replayControl = new ReplayControl(samples.First().SessionData, incidents, removalEdits, TrackCameras);
            sessionDataCapture = new SessionDataCapture(overlayData);

            //CAPTURING LEADERBOARD, CAMERAS will be done at FF16x. TO BE DETERMINED WHETHER STANDARD INTERVALS HAVE TO BE REDUCED BY FACTOR OF 16?!
            captureLeaderBoardEveryHalfSecond = new SampleFilter(TimeSpan.FromSeconds(0.5),
                new CaptureLeaderBoard(overlayData, commentaryMessages, removalEdits).Process);
            captureCamDriverEveryQuaterSecond = new SampleFilter(TimeSpan.FromSeconds(0.25),
                    new CaptureCamDriver(overlayData).Process);

            captureCamDriverEvery4Seconds = new SampleFilter(TimeSpan.FromSeconds(4),
                new LogCamDriver().Process);

            //----------------------------
            //end copy / end review 
            //----------------------------

            ApplyFirstLapCameraDirection(samples, replayControl);

            samples = samples
                .VerifyReplayFrames()
                .WithCorrectedPercentages()
                .WithCorrectedDistances()
                .WithFastestLaps()
                .WithFinishingStatus()
                .WithPitStopCounts()
                .TakeUntil(3.Seconds()).Of(d => d.Telemetry.LeaderHasFinished && d.Telemetry.RaceCars.All(c => c.HasSeenCheckeredFlag || c.HasRetired || c.TrackSurface != TrackLocation.OnTrack))
                .TakeUntil(3.Seconds()).AfterReplayPaused();
            
            samples = samples.AtSpeed(iReplaySpeedForAnalysis);

            overlayData.CapturedVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            ulong numberOfDataProcessed = 0;
            var startTime = DateTime.Now;

            TraceDebug.WriteLine("ADV_RECORDING: Starting foreach sample analysis at {0}", startTime);
            foreach (var data in samples)
            {
                var relativeTime = (DateTime.Now - startTime).Multiply(iReplaySpeedForAnalysis);        //calculate relative time in Replay taking into account replay speed (FF)

                TraceDebug.WriteLine("ADV_RECORDING: Processing data sample {0} | relativeTime: {1} | sessionTime: {2} ".F(numberOfDataProcessed, relativeTime, data.Telemetry.SessionTime));

                replayControl.Process(data);                                            
                sessionDataCapture.Process(data);                                       
                captureLeaderBoardEveryHalfSecond.Process(data, relativeTime);          
                captureCamDriverEveryQuaterSecond.Process(data, relativeTime);          
                recordPitStop.Process(data, relativeTime);                              
                fastestLaps.Process(data, relativeTime);                                
                removalEdits.Process(data, relativeTime);                               
                captureCamDriverEvery4Seconds.Process(data, relativeTime);              

                numberOfDataProcessed += 1;
            }

            removalEdits.Stop();

            TraceDebug.WriteLine("Race analysis phase completed. {0} data samples processed with replay speed {1}".F(numberOfDataProcessed, iReplaySpeedForAnalysis));

            //save OverlayData into target folder for video ("working folder")
            SaveReplayScript(overlayData);
            TraceDebug.WriteLine("Replay Script saved to disk");

            iRacing.Replay.SetSpeed(0);

            TraceDebug.WriteLine("ADV_RECORDING: AnalyseRaceSituations completed");
        }
    }
}
