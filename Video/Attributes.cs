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

using iRacingReplayDirector.Phases.Capturing;
using iRacingReplayDirector.Phases.Transcoding;
using MediaFoundation.Net;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace iRacingReplayDirector.Video
{
    public class VideoAttributes
    {
        public class VideoDetails
        {
            public readonly int[] SupportedAudioBitRates;
            public readonly int BitRate;
            public readonly Size FrameSize;
            public readonly int FrameRate;
            public readonly string ErrorMessage;
            public readonly int AudioSamplesPerSecond;
            public readonly string AudioEncoding;
            public readonly string VideoEncoding;
            internal readonly Transcoder Transcoder;
            public readonly int AudioAverageBytesPerSecond;

            internal VideoDetails(int[] supportedAudioBitRates,
                int frameRate,
                Size frameSize,
                int bitRate,
                int audioSamplesPerSecond,
                int audioAverageBytesPerSecond,
                string videoEncoding,
                string audioEncoding,
                Transcoder transcoder,
                string errorMessage)
            {
                this.SupportedAudioBitRates = supportedAudioBitRates;
                this.FrameRate = frameRate;
                this.FrameSize = frameSize;
                this.BitRate = bitRate;
                this.AudioAverageBytesPerSecond = audioAverageBytesPerSecond;
                this.AudioSamplesPerSecond = audioSamplesPerSecond;
                this.AudioEncoding = audioEncoding;
                this.VideoEncoding = videoEncoding;
                this.Transcoder = transcoder;
                this.ErrorMessage = errorMessage;
            }
        }

        public static VideoDetails TestFor(string videoFileName)
        {
            List<int> supportedAudioBitRates = new List<int>();

            using (MFSystem.Start())
            {
                var readWriteFactory = new ReadWriteClassFactory();

                var sourceReader = readWriteFactory.CreateSourceReaderFromURL(videoFileName, null);

                var videoStream = sourceReader.Streams.FirstOrDefault(s => s.IsSelected && s.NativeMediaType.IsVideo);
                if (videoStream.IsNull)
                    throw new Exception("Unable to find video track within file.");

                var audioStream = sourceReader.Streams.FirstOrDefault(s => s.IsSelected && s.NativeMediaType.IsAudio);
                if (audioStream.IsNull)
                    throw new Exception("Unable to find audio track within file.");

                var channels = audioStream.NativeMediaType.AudioNumberOfChannels;
                var sampleRate = audioStream.NativeMediaType.AudioSamplesPerSecond;

                var types = MFSystem.TranscodeGetAudioOutputAvailableTypes(MediaFoundation.MFMediaType.WMAudioV9, MediaFoundation.Transform.MFT_EnumFlag.All);

                foreach (var bitRate in types
                    .Where(t => t.AudioSamplesPerSecond == sampleRate)
                    .Select(t => t.AudioAverageBytesPerSecond)
                    .Distinct()
                    .OrderBy(t => t))
                {
                    supportedAudioBitRates.Add(bitRate);
                }

                int videoBitRate = 0;
                videoStream.NativeMediaType.TryGetBitRate(out videoBitRate);

                var audioSamplesPerSecond = audioStream.NativeMediaType.AudioSamplesPerSecond;
                var audioAverageBytesPerSecond = audioStream.NativeMediaType.AudioAverageBytesPerSecond;

                var audioSubTypeName = audioStream.CurrentMediaType.SubTypeName;
                var videoSubTypeName = videoStream.CurrentMediaType.SubTypeName;

                var transcoder = new Transcoder
                {
                    VideoFiles = new[] { new SourceReaderExtra(videoFileName, null) },
                    DestinationFile = Path.ChangeExtension(videoFileName, "wmv"),
                    VideoBitRate = 5000000
                };

                var errorMessage = transcoder.TestVideoConversion();

                return new VideoDetails(
                    supportedAudioBitRates.ToArray(),
                    videoStream.NativeMediaType.FrameRate.ToInt(),
                    videoStream.NativeMediaType.FrameSize,
                    videoBitRate / 1000000,
                    audioSamplesPerSecond,
                    audioAverageBytesPerSecond,
                    videoSubTypeName,
                    audioSubTypeName,
                    transcoder,
                    errorMessage);
            }
        }

        public static VideoDetails TestFor(OverlayData data)
        {
            return TestFor(data.VideoFiles.Last().FileName);
        }
    }
}
