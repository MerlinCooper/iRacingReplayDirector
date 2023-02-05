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

using iRacingSDK.Support;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace iRacingReplayDirector.Phases
{
    public static class SynchronizationContextExtension
    {
        public static void Post(this SynchronizationContext self, Action action)
        {
            self.Post((ignored) => action(), null);
        }
    }

    public partial class IRacingReplay
    {
        List<Action> actions = new List<Action>();
        bool shortTestOnly;
        bool bRecordUsingPauseResume;
        bool bCloseiRacingAfterRecording;

        public IRacingReplay(bool shortTestOnly = false, bool bRecordUsingPauseResume = false, bool bCloseiRacingAfterRecording = false)
        {
            this.shortTestOnly = shortTestOnly;
            this.bRecordUsingPauseResume = bRecordUsingPauseResume;
            this.bCloseiRacingAfterRecording = bCloseiRacingAfterRecording;
        }

        private void Add(Action<Action> action, Action onComplete)
        {
            var context = SynchronizationContext.Current;
            if (context != null)
                actions.Add(() => action(() => context.Post(onComplete)));
            else
                actions.Add(() => action(onComplete));
        }

        private void Add(Action<Action<string, string>> action, Action<string, string> onComplete)
        {
            var context = SynchronizationContext.Current;

            actions.Add(() => action((a, b) => context.Post(() => onComplete(a, b))));
        }

        private void Add(Action<Action<string>> action, Action<string> onComplete)
        {
            var context = SynchronizationContext.Current;

            actions.Add(() => action(a => context.Post(() => onComplete(a))));
        }

        public IRacingReplay WhenIRacingStarts(Action onComplete)
        {
            Add(_WhenIRacingStarts, onComplete);

            return this;
        }

        public IRacingReplay AnalyseRace(Action onComplete)
        {
            Add(_AnalyseRace, onComplete);

            return this;
        }

        public IRacingReplay CaptureOpeningScenes()
        {
            Add(_CaptureOpeningScenes, () => { });

            return this;
        }

        public IRacingReplay WithWorkingFolder(string workingFolder)
        {
            _WithWorkingFolder(workingFolder);
            return this;
        }

        public IRacingReplay CaptureRace(Action<string> onComplete)
        {
            Add((a) => _CaptureRace(a), onComplete);

            return this;
        }

        public IRacingReplay CloseIRacing()
        {
            return this;
        }

        public IRacingReplay WithEncodingOf(int videoBitRate)
        {
            _WithEncodingOf(videoBitRate);
            return this;
        }

        public IRacingReplay WithOverlayFile(string overlayFile)
        {
            _WithOverlayFile(overlayFile);
            return this;
        }

        public IRacingReplay OverlayRaceDataOntoVideo(Action<long, long> progress, Action completed, bool highlightsOnly, bool shutdownAfterCompleted)
        {
            var context = SynchronizationContext.Current;

            actions.Add(
                () => _OverlayRaceDataOntoVideo(
                    (c, d) => context.Post(() => progress(c, d)),
                    () => context.Post(completed),
                    highlightsOnly,
                    shutdownAfterCompleted,
                    token
                )
            );

            return this;
        }

        public void InTheForeground()
        {
            foreach (var action in actions)
                action();
        }

        bool requestAbort = false;
        Task backgrounTask = null;
        private CancellationTokenSource cancellationTokenSource;
        private CancellationToken token;

        public void RequestAbort()
        {
            requestAbort = true;
            cancellationTokenSource.Cancel();
        }

        public IRacingReplay InTheBackground(Action<string> onComplete)
        {
            requestAbort = false;
            var context = SynchronizationContext.Current;

            cancellationTokenSource = new CancellationTokenSource();
            token = cancellationTokenSource.Token;

            backgrounTask = new Task(() =>
            {

                try
                {
                    foreach (var action in actions)
                    {
                        action();
                        if (token.IsCancellationRequested)
                            break;
                    }

                    context.Post(() => onComplete(null));
                }
                catch (Exception e)
                {
                    TraceError.WriteLine(e.Message);
                    TraceError.WriteLine(e.StackTrace);
                    TraceInfo.WriteLine("Process aborted");
                    var message = e.InnerException != null ? e.InnerException.Message : e.Message;
                    context.Post(() => onComplete("There was an error - details in Log Messages\n{0}".F(message)));
                }
                finally
                {
                    backgrounTask = null;
                    actions = new List<Action>();
                }

            });

            backgrounTask.Start();

            return this;
        }
    }
}
