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

using iRacingReplayDirector.Support;
using iRacingReplayDirector.Video;
using iRacingSDK.Support;
using MediaFoundation.Net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace iRacingReplayDirector
{
    public partial class TestVideoConversion : Form
    {
        MyListener listener;
        Task task;

        public TestVideoConversion()
        {
            InitializeComponent();
        }

        void TestVideoCapture_Load(object sender, EventArgs e)
        {
            sourceFilenameTextBox.Text = "";

            listener = new MyListener(this.TraceMessageTextBox);
            Trace.Listeners.Add(listener);

            sourceFilenameTextBox_TextChanged(null, null);
        }

        void TestVideoCapture_FormClosed(object sender, FormClosedEventArgs e)
        {
            Trace.Listeners.Remove(listener);
        }

        void workingFolderButton_Click(object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog();

            dlg.InitialDirectory = sourceFilenameTextBox.Text;
            dlg.Filter = "AVI files (*.avi)|*.avi|MPEG 4 (*.mp4)|*.mp4|All files (*.*)|*.*";

            if (dlg.ShowDialog() == DialogResult.OK)
                sourceFilenameTextBox.Text = dlg.FileName;
        }

        void testVideoCaptureButton_Click(object sender, EventArgs e)
        {
            testVideoCaptureButton.Enabled = false;

            if (task != null)
            {
                task.Wait(1.Second());

                task.Dispose();
            }

            var workingFolder = sourceFilenameTextBox.Text;
            var context = SynchronizationContext.Current;

            task = new Task(() => RunTest(workingFolder, context));

            TraceMessageTextBox.Text = "";

            task.Start();
        }

        void RunTest(string filename, SynchronizationContext context)
        {
            try
            {
                TranscodeVideoTest(filename);
            }
            catch (Exception e)
            {
                TraceError.WriteLine(e.Message);
                TraceError.WriteLine(e.StackTrace);
            }
            finally
            {
                context.Post(ignored => testVideoCaptureButton.Enabled = true, null);
            }
        }

        void TranscodeVideoTest(string filename)
        {
            List<int> supportedAudioBitRates = new List<int>();

            using (MFSystem.Start())
            {
                var details = VideoAttributes.TestFor(filename);

                TraceInfo.WriteLine("Frame Rate: {0}, Frame Size: {1}x{2}, Video: {3} @ {4}Mbs, Audio: {5}, {6}Khz @ {7}Kbs, ".F
                        (details.FrameRate,
                        details.FrameSize.Width,
                        details.FrameSize.Height,
                        details.VideoEncoding,
                        details.BitRate == 0 ? "-- " : details.BitRate.ToString(),
                        details.AudioEncoding,
                        details.AudioSamplesPerSecond / 1000,
                        details.AudioAverageBytesPerSecond / 1000));

                TraceInfo.WriteLine("Begining video re-encoding.");

                details.Transcoder.ProcessVideo((readers, saveToSink) =>
                {
                    int lastSecond = 0;
                    var fn = AVOperations.FadeIn(saveToSink);

                    readers.First().SourceReader.Samples(sample =>
                    {
                        if (sample.Stream.CurrentMediaType.IsVideo && sample.Sample != null)
                        {
                            var s = (int)sample.Sample.SampleTime.FromNanoToSeconds();
                            if (s != lastSecond)
                                TraceInfo.WriteLine("Converted: {0} seconds", s);
                            lastSecond = s;

                            if (s > 10)
                                return false;
                        }

                        return fn(sample);

                    });
                });

                TraceInfo.WriteLine("Video converted.  Review the video file {0} to confirm it looks OK.", details.Transcoder.DestinationFile);
                TraceInfo.WriteLine("Success!");
            }
        }

        private void sourceFilenameTextBox_TextChanged(object sender, EventArgs e)
        {
            var exists = File.Exists(sourceFilenameTextBox.Text);
            this.testVideoCaptureButton.Enabled = exists;
        }
    }
}
