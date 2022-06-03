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

using iRacingReplayDirector.Phases.Capturing;
using iRacingReplayDirector.Phases.Direction;
using iRacingReplayDirector.Phases.Transcoding;
using iRacingReplayDirector.Support;
using iRacingSDK;
using iRacingSDK.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Win32;

namespace iRacingReplayDirector.Phases
{
    public partial class IRacingReplay
    {
        string workingFolder;
        string introVideo;


        void _WithWorkingFolder(string workingFolder)
        {
            this.workingFolder = workingFolder;
        }

        void _WithIntroVideo(string fileName)
        {
            this.introVideo = fileName;
        }

        void _CaptureRace(Action<string> onComplete)
        {
            _CaptureRaceTest(onComplete, new iRacingConnection().GetBufferedDataFeed());
        }

        internal void _CaptureRaceTest(Action<string> onComplete, IEnumerable<DataSample> samples)
        {
            //identify wheather highlights video only is selected and OBS pause/resume can be used (to be implemented)
            if (bRecordUsingPauseResume)
            {
                //Retrieve list of raceEvents selected depending on the duration of the highlight video
                var totalRaceEvents = RaceEventExtension.GetInterestingRaceEvents(overlayData.RaceEvents.ToList());

                ApplyFirstLapCameraDirection(samples, replayControl);

                
                //Record the selected race events into a highlight video
                raceVideo.Activate(workingFolder);                                      //Active video-capturing and send start command to recording software. 

                OverlayData.CamDriver curCamDriver = overlayData.CamDrivers.First();

                //start thread to control / switch cameras while recording
                ReplayControl.cameraControl.ReplayCameraControlTask(overlayData);

                //ReplayControl.cameraControl.CameraOnDriver(short.Parse(curCamDriver.CurrentDriver.CarNumber), (short)curCamDriver.camGroupNumber);

                //cycle through all raceEvents selected for the highlight video and record them  (REMARK: Camera switching not implemented yet)
                foreach (var raceEvent in totalRaceEvents)
                {
                    TraceInfo.WriteLine("ADV_RECORDING: Type: {0} | Durations-Span: {1} | ".F(raceEvent.GetType(), raceEvent.Duration));

                    //jump to selected RaceEvent in iRacing Replay
                    int framePositionInRace = raceStartFrameNumber + (int)Math.Round(raceEvent.StartTime * 60.0);
                    iRacing.Replay.MoveToFrame(raceStartFrameNumber + (int)Math.Round(raceEvent.StartTime * 60.0));

                    iRacing.Replay.SetSpeed((int)replaySpeeds.normal);           //start iRacing Replay at selected position                     

                    raceVideo.Resume();                                         //resume recording

                    TraceDebug.WriteLine("Recording Race-Event. Frame-Position: {0}  | Duration: {1} ms".F(framePositionInRace, 1000 * raceEvent.Duration));

                    Thread.Sleep((int)(1000 * raceEvent.Duration));                       //pause thread until scene is fully recorded.
                    raceVideo.Pause();                                         //pause recording software before jumping to new position in iRacing Replay   
                }


                TraceDebug.WriteLine("Video Capture of Race-Events completed");
                raceVideo.Stop();
            } else {        //Code to be removed after being able to implment working solution where analysis phase and replay-capture phase are distinct processes. 
                //use local variables for original code instead of global variables introduced to support full analysis in analysis-phase
                
                var overlayData = new OverlayData();                          
                var removalEdits = new RemovalEdits(overlayData.RaceEvents);
                var commentaryMessages = new CommentaryMessages(overlayData);
                var recordPitStop = new RecordPitStop(commentaryMessages);
                var fastestLaps = new RecordFastestLaps(overlayData);
                var replayControl = new ReplayControl(samples.First().SessionData, incidents, removalEdits, TrackCameras);
                var sessionDataCapture = new SessionDataCapture(overlayData);
                var captureLeaderBoardEveryHalfSecond = new SampleFilter(TimeSpan.FromSeconds(0.5),
                    new CaptureLeaderBoard(overlayData, commentaryMessages, removalEdits).Process);
                var captureCamDriverEveryQuaterSecond = new SampleFilter(TimeSpan.FromSeconds(0.25),
                     new CaptureCamDriver(overlayData).Process);

                var captureCamDriverEvery4Seconds = new SampleFilter(TimeSpan.FromSeconds(4),
                    new LogCamDriver().Process);


                TraceDebug.WriteLine("Cameras:");
                TraceDebug.WriteLine(TrackCameras.ToString());



                var videoCapture = new VideoCapture();

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

                if (shortTestOnly)
                {
                    samples = samples.AtSpeed(Settings.Default.TimingFactorForShortTest);
                    Settings.AppliedTimingFactor = 1.0 / Settings.Default.TimingFactorForShortTest;
                }

                videoCapture.Activate(workingFolder);                           //Start video capturing FileName will be given by recording software. 
                var startTime = DateTime.Now;

                overlayData.CapturedVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

                foreach (var data in samples)
                {
                    var relativeTime = DateTime.Now - startTime;

                    TraceDebug.WriteLine("Recording at time: {0}", relativeTime);

                    replayControl.Process(data);
                    sessionDataCapture.Process(data);
                    captureLeaderBoardEveryHalfSecond.Process(data, relativeTime);
                    captureCamDriverEveryQuaterSecond.Process(data, relativeTime);
                    recordPitStop.Process(data, relativeTime);
                    fastestLaps.Process(data, relativeTime);
                    removalEdits.Process(data, relativeTime);
                    captureCamDriverEvery4Seconds.Process(data, relativeTime);
                }

                var files = videoCapture.Deactivate();                          //Stop video capturing - returns list with "guessed" filename. Filenmae being different from replay-script due to different time stamp.
                                                                                //investigate whether renaming of video file is necessary. 

                removalEdits.Stop();

                var overlayFile = SaveOverlayData(overlayData, files);

                iRacing.Replay.SetSpeed(0);

                AltTabBackToApp();

                if (files.Count == 0)
                    throw new Exception("Unable to find video files in '{0}' - possible wrong working folder".F(workingFolder));

                _WithOverlayFile(overlayFile);

                onComplete(overlayFile);
            }
            

            //terminate iRacing after video capture completed to free up CPU resources
            if (bCloseiRacingAfterRecording)
            {
                try
                {
                    //To be added: Option to select/deselect termination of iRacing after capturing video in new settings Dialog
                    Process[] iRacingProc = Process.GetProcessesByName("iRacingSim64DX11");
                    iRacingProc[0].Kill();
                }
                catch
                {
                    throw new Exception("Could not terminate iRacing Simulator".F(workingFolder));
                }
            }
        }

        static void AltTabBackToApp()
        {
            KeyboardEmulator.SendHotkey(KeyboardEmulator.Win32VirtualKeyCodes.VK_TAB, KeyboardEmulator.Win32VirtualKeyCodes.VK_MENU);
            /*Keyboard.keybd_event(Keyboard.VK_MENU, 0, 0, UIntPtr.Zero);
            Thread.Sleep(200);
            Keyboard.keybd_event(Keyboard.VK_TAB, 0, 0, UIntPtr.Zero);
            Thread.Sleep(200);
            Keyboard.keybd_event(Keyboard.VK_TAB, 0, Keyboard.KEYEVENTF_KEYUP, UIntPtr.Zero);
            Thread.Sleep(200);
            Keyboard.keybd_event(Keyboard.VK_MENU, 0, Keyboard.KEYEVENTF_KEYUP, UIntPtr.Zero);*/
        }

        void ApplyFirstLapCameraDirection(IEnumerable<DataSample> samples, ReplayControl replayControl)
        {
            iRacing.Replay.MoveToFrame(raceStartFrameNumber);
            iRacing.Replay.SetSpeed(1);

            iRacing.Replay.Wait();
            Thread.Sleep(1000);

            replayControl.Process(samples.First());
            
            iRacing.Replay.Wait();
            Thread.Sleep(1000);
        }

        string SaveOverlayData(OverlayData overlayData, List<CapturedVideoFile> files)
        {
            string firstFileName;

            if (files.Count == 0)
                //firstFileName = workingFolder + "/unknown_capture-{0}".F(DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));
                firstFileName = workingFolder + "/unknown_capture-{0}".F(overlayData.overlayDateTime.ToString("yyyy-MM-dd-HH-mm-ss"));
            else
                firstFileName = files.First().FileName;

            var overlayFile = Path.ChangeExtension(firstFileName, ".replayscript");
            Trace.WriteLine("Saving overlay data to {0}".F(overlayFile));

            if (this.introVideo != null)
                files = new [] { new CapturedVideoFile { FileName = this.introVideo, isIntroVideo = true } }
                        .Concat(files).ToList();

            overlayData.VideoFiles = files;
            overlayData.SaveTo(overlayFile);

            return overlayFile;
        }

        string SaveReplayScript (OverlayData overlayData)
        {
            //string fullNameReplayScript = workingFolder + "/" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + "analysis.replayscript";
            string fullNameReplayScript = workingFolder + "/" + overlayData.overlayDateTime.ToString("yyyy-MM-dd HH-mm-ss") + ".analysis.replayscript";

            Trace.WriteLine("Saving ReplayScript (analysis phase) to {0}" + fullNameReplayScript);

            overlayData.SaveTo(fullNameReplayScript);

            return fullNameReplayScript;
        }

    }
}
