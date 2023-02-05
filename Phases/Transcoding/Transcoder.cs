
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
using MediaFoundation;
using MediaFoundation.Net;
using MediaFoundation.Transform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace iRacingReplayDirector.Phases.Transcoding
{
    public class SourceReaderExtra : IDisposable
    {
        public readonly string FileName;
        public readonly SourceReader SourceReader;
        public readonly object State;

        public SourceReaderExtra(string fileName, object state)
        {
            FileName = fileName;
            State = state;
        }

        public SourceReaderExtra(string fileName, object state, SourceReader sourceReader)
        {
            FileName = fileName;
            State = state;
            SourceReader = sourceReader;
        }

        public SourceReaderExtra CreateSourceReader(ReadWriteClassFactory readWriteFactory, Attributes attributes)
        {
            TraceDebug.WriteLine("Attempting to open file {0}".F(this.FileName));
            var reader = readWriteFactory.CreateSourceReaderFromURL(this.FileName, attributes);
            TraceDebug.WriteLine("Opened file {0}.  Duration: {1}".F(this.FileName, reader.Duration.FromNanoToSeconds()));

            return new SourceReaderExtra(this.FileName, this.State, reader);
        }

        public void Dispose()
        {
            SourceReader.Dispose();
        }
    }

    class Transcoder
    {
        public IEnumerable<SourceReaderExtra> VideoFiles;
        public string DestinationFile;
        public int VideoBitRate;

        static Guid TARGET_AUDIO_FORMAT = MFMediaType.WMAudioV9;
        static Guid TARGET_VIDEO_FORMAT = MFMediaType.WMV3;

        internal string TestVideoConversion()
        {
            var readWriteFactory = new ReadWriteClassFactory();

            var attributes = new Attributes
            {
                ReadWriterEnableHardwareTransforms = true,
                SourceReaderEnableVideoProcessing = true
            };

            var readers = VideoFiles.Select(f => f.CreateSourceReader(readWriteFactory, attributes)).ToArray();

            var testOuputFile = readers.First().FileName + ".tmp.test.wmv";
            try
            {
                using (var sinkWriter = readWriteFactory.CreateSinkWriterFromURL(testOuputFile, attributes))
                {
                    var writeToSink = ConnectStreams(readers, sinkWriter);
                }
            }
            catch (Exception e)
            {
                return e.Message;
            }
            finally
            {
                if (readers != null)
                    foreach (var r in readers)
                        r.Dispose();

                File.Delete(testOuputFile);
            }

            return null;
        }

        internal void ProcessVideo(Action<IEnumerable<SourceReaderExtra>, ProcessSample> process)
        {
            var readWriteFactory = new ReadWriteClassFactory();

            var attributes = new Attributes
            {
                ReadWriterEnableHardwareTransforms = true,
                SourceReaderEnableVideoProcessing = true
            };

            var readers = VideoFiles.Select(f => f.CreateSourceReader(readWriteFactory, attributes)).ToArray();

            try
            {
                using (var sinkWriter = readWriteFactory.CreateSinkWriterFromURL(DestinationFile, attributes))
                {
                    var writeToSink = ConnectStreams(readers, sinkWriter);

                    using (sinkWriter.BeginWriting())
                        process(readers, writeToSink);
                }
            }
            finally
            {
                if (readers != null)
                    foreach (var r in readers)
                        r.Dispose();
            }
        }

        private ProcessSample ConnectStreams(IEnumerable<SourceReaderExtra> readers, SinkWriter sinkWriter)
        {
            foreach (var r in readers)
            {
                SetAudioMediaType(r.SourceReader);
                SetVideoMediaType(r.SourceReader);
            }

            var sourceAudioStream = SetAudioMediaType(readers.First().SourceReader);
            var sourceVideoStream = SetVideoMediaType(readers.First().SourceReader);

            var sinkAudioStream = AddStream(sinkWriter, sourceAudioStream.CurrentMediaType, CreateTargetAudioMediaType(sourceAudioStream.NativeMediaType));
            var sinkVideoStream = AddStream(sinkWriter, sourceVideoStream.CurrentMediaType, CreateTargetVideoMediaType(sourceVideoStream.NativeMediaType));

            var saveAudio = AVOperations.MediaTypeChange(sinkAudioStream, AVOperations.SaveTo(sinkAudioStream));
            var saveVideo = AVOperations.MediaTypeChange(sinkVideoStream, AVOperations.SaveTo(sinkVideoStream));

            return AVOperations.SeperateAudioVideo(saveAudio, saveVideo);
        }

        SourceStream SetAudioMediaType(SourceReader sourceReader)
        {
            try
            {
                var sourceStream = sourceReader.Streams.FirstOrDefault(s => s.IsSelected && s.NativeMediaType.IsAudio);

                if (sourceStream.IsNull)
                    throw new Exception("Unable to find audio track within file.");

                sourceStream.CurrentMediaType = new MediaType() { MajorType = MFMediaType.Audio, SubType = MFMediaType.PCM };

                return sourceStream;
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("Unable to decode audio stream. {0}", e.Message), e);
            }
        }

        SourceStream SetVideoMediaType(SourceReader sourceReader)
        {
            try
            {
                var sourceStream = sourceReader.Streams.FirstOrDefault(s => s.IsSelected && s.NativeMediaType.IsVideo);

                if (sourceStream.IsNull)
                    throw new Exception("Unable to find video track within file.");

                sourceStream.CurrentMediaType = new MediaType() { MajorType = MFMediaType.Video, SubType = MFMediaType.RGB32 };

                return sourceStream;
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("Unable to decode video stream. {0}", e.Message), e);
            }
        }

        SinkStream AddStream(SinkWriter sinkWriter, MediaType input, MediaType encoding)
        {
            var sinkStream = sinkWriter.AddStream(encoding);
            sinkStream.InputMediaType = input;
            return sinkStream;
        }

        MediaType CreateTargetAudioMediaType(MediaType nativeMediaType)
        {
            var numberOfChannels = nativeMediaType.AudioNumberOfChannels;
            var sampleRate = nativeMediaType.AudioSamplesPerSecond;

            var availableTypes = MFSystem.TranscodeGetAudioOutputAvailableTypes(TARGET_AUDIO_FORMAT, MFT_EnumFlag.All);

            TraceDebug.WriteLine("Searching for audio transcoding for sampleRate of {0}khz and {1} channels".F(sampleRate / 1000, numberOfChannels));

            var type = availableTypes
                            .OrderByDescending(t => t.AudioAverageBytesPerSecond)
                            .ThenByDescending(t => t.AudioNumberOfChannels)
                            .FirstOrDefault(t => t.AudioSamplesPerSecond == sampleRate && t.AudioNumberOfChannels == numberOfChannels);

            if (type == null)
            {
                TraceDebug.WriteLine("No audio transcoder found.  Searching for transcoding with 2 or fewer channels at any sample rate");
                type = availableTypes
                                            .OrderByDescending(t => t.AudioAverageBytesPerSecond)
                                            .ThenByDescending(t => t.AudioNumberOfChannels)
                                            .FirstOrDefault(t => t.AudioNumberOfChannels <= 2);
            }

            if (type == null)
            {
                TraceDebug.WriteLine("No audio transcoder found.  Search for first compatible transcoder");
                type = availableTypes
                                            .OrderByDescending(t => t.AudioAverageBytesPerSecond)
                                            .ThenByDescending(t => t.AudioNumberOfChannels)
                                            .FirstOrDefault();
            }

            if (type != null)
            {
                TraceDebug.WriteLine("Found audio track.  SampleRate: {0}, Channels: {1}, BitRate: {2}kbs".F(type.AudioSamplesPerSecond / 1000, type.AudioNumberOfChannels, type.AudioAverageBytesPerSecond * 8 / 1000));
                return new MediaType(type);
            }

            throw new Exception("Unable to find target audio format");
        }

        MediaType CreateTargetVideoMediaType(MediaType nativeMediaType)
        {
            var size = nativeMediaType.FrameSize;
            var rate = nativeMediaType.FrameRate;
            var aspect = nativeMediaType.AspectRatio;
            var bitRate = VideoBitRate;

            var mediaType = new MediaType()
            {
                MajorType = MFMediaType.Video,
                SubType = TARGET_VIDEO_FORMAT,
                FrameSize = size,
                FrameRate = rate,
                AspectRatio = aspect,
                BitRate = bitRate,
                InterlaceMode = MFVideoInterlaceMode.Progressive
            };

            return mediaType;
        }
    }
}
