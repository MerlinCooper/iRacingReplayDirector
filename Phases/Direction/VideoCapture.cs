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
using System.IO;
using System.Linq;
using System.Timers;
using System.Collections.Generic;
using iRacingReplayDirector.Phases.Capturing;
using iRacingReplayDirector.Support;
using WK.Libraries.HotkeyListenerNS;

namespace iRacingReplayDirector.Phases.Direction
{
    enum videoStatus { stopped, running, paused };

    public class VideoCapture
    {
        string workingFolder;
        DateTime started;
        Timer timer;
        List<CapturedVideoFile> captureFileNames = new List<CapturedVideoFile>();
        videoStatus curVideoStatus = videoStatus.stopped;

        ~VideoCapture()
        {
            if(curVideoStatus != videoStatus.stopped)
                SendKeyStroke_StartStopp();
        }
        
        public void Activate(string workingFolder, bool bStartRecording = true)
        {
            this.workingFolder = workingFolder;
            this.started = DateTime.Now;

            timer = new Timer(500);
            timer.Elapsed += CaptureNewFileNames; 
            timer.AutoReset = false;
            timer.Enabled = true;
            
            if (bStartRecording && (curVideoStatus == videoStatus.stopped))
            {
                SendKeyStroke_StartStopp();     //Send hot-key to start recording
                curVideoStatus = videoStatus.running;
            }else if( curVideoStatus == videoStatus.paused){
                Resume();
            }
        }

        private void CaptureNewFileNames(object sender, ElapsedEventArgs e)
        {
            try
            {
                var guessedFileName = Directory.GetFiles(workingFolder, "*.avi")
                    .Concat(Directory.GetFiles(workingFolder, "*.mp4"))
                    .Select(fn => new { FileName = fn, CreationTime = File.GetCreationTime(fn) })
                    .Where(f => f.CreationTime >= started)
                    .OrderByDescending(f => f.CreationTime)
                    .FirstOrDefault();

                if (guessedFileName != null && !captureFileNames.Any(c => c.FileName == guessedFileName.FileName))
                {
                    TraceInfo.WriteLine("Found video file {0}", guessedFileName.FileName);
                    captureFileNames.Add(new CapturedVideoFile { FileName = guessedFileName.FileName });
                }
            }
            catch (Exception ee)
            {
                TraceError.WriteLine(ee.Message);
                TraceError.WriteLine(ee.StackTrace);
            }
            finally
            {
                if (timer != null)
                    timer.Start();
            }
        }

        public List<CapturedVideoFile> Deactivate(bool bRecordUsingPauseResume=false)
        {
            if (timer != null)
            {
                var t = timer;
                timer = null;
                t.Stop();
                t.Dispose();
                
            }

            if (bRecordUsingPauseResume && curVideoStatus != videoStatus.paused)
            {
                Pause();
                curVideoStatus = videoStatus.paused;
            } else
            {
                SendKeyStroke_StartStopp();
                curVideoStatus = videoStatus.stopped;
            }

            System.Threading.Thread.Sleep(2000);

            CaptureNewFileNames(null, null);

            TraceInfo.WriteLineIf(captureFileNames.Count == 0, "Unable to find video files in folder '{0}' - check your Video Working folder", workingFolder);

            return captureFileNames;
        }

        //methode sending key-stroke command to pause recording software
        public void Pause()
        {
            if(curVideoStatus == videoStatus.running && curVideoStatus != videoStatus.paused)
            {
                SendKeyStroke_PauseResume();
                curVideoStatus = videoStatus.paused;
            }
        }

        //methode sending key-stroke command to resume recording software
        public void Resume()
        {
            if (curVideoStatus == videoStatus.paused)
            {
                SendKeyStroke_PauseResume();
                curVideoStatus = videoStatus.running;
            }
        }

        public void Stop()
        {
            if(curVideoStatus == videoStatus.running || curVideoStatus == videoStatus.paused)
            {
                SendKeyStroke_StartStopp();
                curVideoStatus = videoStatus.stopped;
            }
        }


        private static void SendKeyStroke_StartStopp()
        {
            TraceInfo.WriteLine("Sending key event to start/stopp recording {0}", Settings.Default.strHotKeyStopStart);

            KeyboardEmulator.SendHotkey(new Hotkey(Settings.Default.strHotKeyStopStart));
        }

        private static void SendKeyStroke_PauseResume()
        {
            TraceInfo.WriteLine("Sending key event to pause/resume recording {0}", Settings.Default.strHotKeyPauseResume);

            KeyboardEmulator.SendHotkey(new Hotkey(Settings.Default.strHotKeyPauseResume));
        }
    }
}
