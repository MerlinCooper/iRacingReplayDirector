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
using iRacingSDK.Support;
using System;
using System.Collections.Generic;
using System.Linq;

namespace iRacingReplayDirector.Phases.Transcoding
{
    public static class RaceEventExtension
    {
        static TimeSpan HighlightVideoDuration
        {
            get { return (Settings.Default.HighlightVideoTargetDuration.TotalMinutes * Settings.AppliedTimingFactor).Minutes(); }
        }

        /// <summary>
        /// Minimum seconds an event must contribute to be considered for gap-filling.
        /// Events shorter than this are skipped to avoid fragmenting the highlight video.
        /// </summary>
        const double MinimumEventContributionSeconds = 15;

        public static List<VideoEdit> GetRaceEdits(this IEnumerable<OverlayData.RaceEvent> raceEvents)
        {
            var edits = raceEvents._GetRaceEdits().ToList();

            foreach (var e in edits)
                TraceInfo.WriteLine("Editing from {0} to {1}. Duration {2}", e.StartTimeSpan, e.EndTimeSpan, e.Duration);

            TraceInfo.WriteLine("Total Edits time {0}", edits.Sum(e => e.Duration).Seconds());

            return edits;
        }

        public static List<VideoEdit> GetRaceEdits(this OverlayData overlayData)
        {
            var edits = overlayData.RaceEvents._GetRaceEdits(overlayData.TimeForOutroOverlay).ToList();

            foreach (var e in edits)
                TraceInfo.WriteLine("Editing from {0} to {1}. Duration {2}", e.StartTimeSpan, e.EndTimeSpan, e.Duration);

            TraceInfo.WriteLine("Total Edits time {0}", edits.Sum(e => e.Duration).Seconds());

            return edits;
        }

        static IEnumerable<VideoEdit> _GetRaceEdits(this IEnumerable<OverlayData.RaceEvent> raceEvents)
        {
            return _GetRaceEdits(raceEvents, null);
        }

        static IEnumerable<VideoEdit> _GetRaceEdits(this IEnumerable<OverlayData.RaceEvent> raceEvents, double? timeForOutroOverlay)
        {
            // Get untrimmed events first to know the original video end time
            var untrimmedEvents = GetInterestingRaceEvents(raceEvents, null);
            var originalEndTime = untrimmedEvents.Max(e => e.EndTime);

            var totalRaceEvents = timeForOutroOverlay.HasValue
                ? GetInterestingRaceEvents(raceEvents, timeForOutroOverlay)
                : untrimmedEvents;

            var previousEvent = totalRaceEvents.First();
            foreach (var re in totalRaceEvents.Skip(1))
            {
                if (re.StartTime - previousEvent.EndTime >= 10d / Settings.AppliedTimingFactor)
                {
                    TraceDebug.WriteLine("Applying edit between {0}:{1}:{2} and {3}:{4}:{5}",
                        previousEvent.Interest, previousEvent.Position, previousEvent.EndTime,
                        re.Interest, re.Position, re.StartTime);
                    yield return new VideoEdit { StartTime = previousEvent.EndTime - 1, EndTime = re.StartTime + 1 };
                }
                else
                    TraceDebug.WriteLine("Not apply edit between {0}:{1}:{2} and {3}:{4}:{5}",
                        previousEvent.Interest, previousEvent.Position, previousEvent.EndTime,
                        re.Interest, re.Position, re.StartTime);

                previousEvent = re;
            }

            // If we trimmed the last lap, cut everything after the trimmed end to the original end
            var lastEvent = totalRaceEvents.Last();
            if (timeForOutroOverlay.HasValue && lastEvent.EndTime < originalEndTime)
            {
                TraceInfo.WriteLine("Highlight Edits: Cutting trailing content from {0} to {1}",
                    lastEvent.EndTime.Seconds(), originalEndTime.Seconds());
                yield return new VideoEdit { StartTime = lastEvent.EndTime, EndTime = originalEndTime };
            }
        }

        public static IOrderedEnumerable<OverlayData.RaceEvent> GetInterestingRaceEvents(IEnumerable<OverlayData.RaceEvent> raceEvents, Boolean bFastRecording = false)
        {
            return GetInterestingRaceEvents(raceEvents, null, bFastRecording);
        }

        public static IOrderedEnumerable<OverlayData.RaceEvent> GetInterestingRaceEvents(IEnumerable<OverlayData.RaceEvent> raceEvents, double? timeForOutroOverlay, Boolean bFastRecording = false)
        {
            TraceInfo.WriteLine("Highlight Edits: Total Duration Target: {0}", HighlightVideoDuration);

            double totalTime, incidentsRatio, restartsRatio, battlesRatio, timeForRaceEvents;
            var firstAndLastLapRaceEvents = GetAllFirstAndLastLapEvents(raceEvents, timeForOutroOverlay, out totalTime);

            var incidentRaceEvents = GetAllRaceEvents(raceEvents, InterestState.Incident, 1.8, 0, out incidentsRatio);
            var restartRaceEvents = GetAllRaceEvents(raceEvents, InterestState.Restart, 1.0, 0, out restartsRatio);
            var battleRaceEvents = GetAllRaceEvents(raceEvents, InterestState.Battle, 1.4, 15, out battlesRatio);

            battleRaceEvents = NormaliseBattleEvents(battleRaceEvents, Settings.Default.BattleStickyPeriod.TotalSeconds);

            //Calculate time for incidents, battles, restarts when fastrecording is active 
            if (bFastRecording)
            {
                //calculate time of race w/o start and finish
                double timeStartFinish = firstAndLastLapRaceEvents.Sum(re => re.Duration).Seconds().TotalSeconds;
                timeForRaceEvents = (HighlightVideoDuration - firstAndLastLapRaceEvents.Sum(re => re.Duration).Seconds()).TotalSeconds;
                //totalRaceTime = totalTime - (incidentsRatio + restartsRatio + battlesRatio);
            }


            var totalRatio = incidentsRatio + restartsRatio + battlesRatio;

            var incidentPercentage = incidentsRatio / totalRatio;
            var restartPercentage = restartsRatio / totalRatio;
            var battlePercentage = battlesRatio / totalRatio;

            var incidentsEdited = ExtractEditedEvents(totalTime, incidentPercentage, incidentRaceEvents, InterestState.Incident, byPosition: true);
            var restartsEdited = ExtractEditedEvents(totalTime, restartPercentage, restartRaceEvents, InterestState.Restart);
            var battlessEdited = ExtractEditedEvents(totalTime, battlePercentage, battleRaceEvents, InterestState.Battle);

            var editedEventsList = firstAndLastLapRaceEvents
                .Concat(incidentsEdited)
                .Concat(restartsEdited)
                .Concat(battlessEdited)
                .ToList();

            // Calculate effective duration (accounting for overlapping events)
            var effectiveDuration = CalculateMergedDuration(editedEventsList);
            var targetDuration = HighlightVideoDuration.TotalSeconds;
            var gap = targetDuration - effectiveDuration;

            TraceInfo.WriteLine("Highlight Edits: Effective duration (merged): {0}, Gap to fill: {1}",
                effectiveDuration.Seconds(), gap.Seconds());

            // If there's a significant gap, fill it with additional events
            if (gap > MinimumEventContributionSeconds)
            {
                var selectedSet = new HashSet<OverlayData.RaceEvent>(editedEventsList);
                var remainingEvents = incidentRaceEvents
                    .Concat(battleRaceEvents)
                    .Concat(restartRaceEvents)
                    .Where(e => !selectedSet.Contains(e))
                    .OrderBy(e => e.StartTime)
                    .ToList();

                editedEventsList = FillGapWithAdditionalEvents(editedEventsList, remainingEvents, gap);
            }

            TraceInfo.WriteLine("Highlight Edits: Expected duration of highlight video: {0}", editedEventsList.Sum(re => re.Duration).Seconds());
            TraceInfo.WriteLine("Highlight Edits: Final effective duration (merged): {0}", CalculateMergedDuration(editedEventsList).Seconds());

            return editedEventsList.OrderBy(re => re.StartTime);
        }

        private static List<OverlayData.RaceEvent> NormaliseBattleEvents(List<OverlayData.RaceEvent> raceEvents, double maxDuration)
        {
            var result = new List<OverlayData.RaceEvent>();

            foreach (var re in raceEvents)
            {
                if (re.Duration < maxDuration)
                    result.Add(re);
                else
                {
                    var segmentCount = (int)(re.Duration / maxDuration) + 1;
                    var segmentDuration = re.Duration / segmentCount;
                    var startTime = re.StartTime;

                    for (var i = 0; i < segmentCount; i++)
                    {
                        var segment = new OverlayData.RaceEvent { Interest = re.Interest, StartTime = startTime, EndTime = startTime + segmentDuration };
                        result.Add(segment);
                        startTime += segmentDuration;
                    }
                }
            }

            return result;
        }

        public static List<OverlayData.RaceEvent> GetAllFirstAndLastLapEvents(IEnumerable<OverlayData.RaceEvent> raceEvents, out double totalTime)
        {
            return GetAllFirstAndLastLapEvents(raceEvents, null, out totalTime);
        }

        public static List<OverlayData.RaceEvent> GetAllFirstAndLastLapEvents(IEnumerable<OverlayData.RaceEvent> raceEvents, double? timeForOutroOverlay, out double totalTime)
        {
            var firstAndLastLapRaceEvents = raceEvents
                .Where(re => re.Interest == InterestState.FirstLap || re.Interest == InterestState.LastLap)
                .ToList();

            // Trim LastLap events to end when the outro overlay ends (30 seconds after trigger)
            if (timeForOutroOverlay.HasValue)
            {
                var outroEndTime = timeForOutroOverlay.Value + 30;  // 30 seconds matches TranscodeAndOverlay.cs

                for (int i = 0; i < firstAndLastLapRaceEvents.Count; i++)
                {
                    var re = firstAndLastLapRaceEvents[i];
                    if (re.Interest == InterestState.LastLap && re.EndTime > outroEndTime)
                    {
                        var trimmedEvent = new OverlayData.RaceEvent
                        {
                            Interest = re.Interest,
                            StartTime = re.StartTime,
                            EndTime = Math.Max(re.StartTime, outroEndTime),
                            WithOvertake = re.WithOvertake,
                            Position = re.Position,
                            RaceLapNumber = re.RaceLapNumber
                        };
                        firstAndLastLapRaceEvents[i] = trimmedEvent;

                        TraceInfo.WriteLine("Highlight Edits: Trimmed LastLap event from {0} to {1}",
                            re.EndTime.Seconds(), trimmedEvent.EndTime.Seconds());
                    }
                }
            }

            var firstAndLastLapDuration = firstAndLastLapRaceEvents.Sum(re => re.Duration);
            totalTime = HighlightVideoDuration.TotalSeconds - firstAndLastLapDuration;

            TraceInfo.WriteLine("Highlight Edits: First & last laps.  Duration: {0}. Remaining: {1}", firstAndLastLapDuration.Seconds(), totalTime.Seconds());

            return firstAndLastLapRaceEvents;
        }

        static void SliceEvent(List<_RaceEvent> result, List<OverlayData.RaceEvent> raceEvents, OverlayData.RaceEvent left, OverlayData.RaceEvent right, int level = 1)
        {
            var middleTime = (left.EndTime + right.StartTime) / 2;

            var middleEvent = raceEvents
                .Where(r => r.StartTime >= left.StartTime && r.StartTime <= right.EndTime)
                .OrderBy(r => Math.Abs(middleTime - r.StartTime)).FirstOrDefault();

            if (middleEvent == null)
                return;

            result.Add(new _RaceEvent { RaceEvent = middleEvent, Level = level });
            raceEvents.Remove(middleEvent);

            SliceEvent(result, raceEvents, left, middleEvent, level + 1);
            SliceEvent(result, raceEvents, middleEvent, right, level + 1);
        }

        static List<OverlayData.RaceEvent> ExtractEditedEvents(
            double totalTime,
            double percentage,
            List<OverlayData.RaceEvent> raceEvents,
            InterestState interest,
            bool byPosition = false)
        {
            TraceInfo.WriteLine("Extracting {0} from a total set of {1}", interest, raceEvents.Count);

            if (raceEvents.Count <= 2)
                return raceEvents;

            var duration = 0d;
            var targetDuration = totalTime * percentage;

            var orderedRaceEvents = raceEvents.OrderBy(r => r.StartTime).ToList();
            var firstEvent = orderedRaceEvents.First();
            var lastEvent = orderedRaceEvents.Last();

            var searchRaceEvents = new List<_RaceEvent>();
            searchRaceEvents.Add(new _RaceEvent { RaceEvent = firstEvent, Level = 0 });
            searchRaceEvents.Add(new _RaceEvent { RaceEvent = lastEvent, Level = 0 });

            var result = new List<OverlayData.RaceEvent>();

            if (byPosition)
            {
                foreach (var p in raceEvents.OrderBy(r => r.Position).Select(r => r.Position).Distinct())
                {
                    TraceInfo.WriteLine("Scanning for {0}s for position {1}", interest, p);
                    duration = ExtractUptoTargetDuration(raceEvents.Where(r => r.Position == p).ToList(), duration, targetDuration, firstEvent, lastEvent, searchRaceEvents, result);
                }
            }
            else
            {
                TraceInfo.WriteLine("Scanning for {0}s", interest);
                duration = ExtractUptoTargetDuration(raceEvents, duration, targetDuration, firstEvent, lastEvent, searchRaceEvents, result);
            }

            foreach (var r in result.OrderBy(x => x.StartTime))
                TraceInfo.WriteLine("Highlight edit {0} @ position {4}: {1} - {2}, duration: {3}", r.Interest, r.StartTime.Seconds(), r.EndTime.Seconds(), r.Duration.Seconds(), r.Position);

            TraceInfo.WriteLine("Highlight Edits: {0}.  Target Duration: {1}, Percentage: {2:00}%, Resolved Duration: {3}",
                interest.ToString(), targetDuration.Seconds(), (int)(percentage * 100), duration.Seconds());

            return result;
        }

        private static double ExtractUptoTargetDuration(List<OverlayData.RaceEvent> raceEvents, double duration, double targetDuration, OverlayData.RaceEvent firstEvent, OverlayData.RaceEvent lastEvent, List<_RaceEvent> orderRaceEvents, List<OverlayData.RaceEvent> result)
        {
            SliceEvent(orderRaceEvents, raceEvents.Where(rc => rc.WithOvertake /* && rc.Position == p*/).ToList(), firstEvent, lastEvent, 1);
            duration = SelectOrderByLevel(orderRaceEvents, targetDuration, result, duration);

            orderRaceEvents.Clear();

            SliceEvent(orderRaceEvents, raceEvents.Where(rc => !rc.WithOvertake /*&& rc.Position == p */).ToList(), firstEvent, lastEvent, 1);
            duration = SelectOrderByLevel(orderRaceEvents, targetDuration, result, duration);

            orderRaceEvents.Clear();
            return duration;
        }

        private static double SelectOrderByLevel(List<_RaceEvent> orderRaceEvents, double targetDuration, List<OverlayData.RaceEvent> result, double duration)
        {
            foreach (var re in orderRaceEvents
                .OrderBy(r => r.Level)
                .ThenBy(r => r.RaceEvent.StartTime)
                .Select(re => re.RaceEvent))
            {
                duration = result.Sum(r => r.Duration);
                if (duration > targetDuration)
                    break;

                if (duration + re.Duration < targetDuration)
                    result.Add(re);
            }
            return duration;
        }

        static List<OverlayData.RaceEvent> GetAllRaceEvents(IEnumerable<OverlayData.RaceEvent> raceEvents, InterestState interest, double factor, double minDuration, out double ratio)
        {
            var result = raceEvents
                .Where(re => re.Interest == interest)
                .Where(re => re.Duration > minDuration)
                .ToList();

            var duration = result.Sum(re => re.Duration);
            ratio = duration * factor;

            TraceInfo.WriteLine("Highlight Edits: {0}.  Duration: {1}, Factor: {2}, Ratio: {3}", interest.ToString(), duration.Seconds(), factor, ratio);

            return result;
        }

        static List<OverlayData.RaceEvent> FillGapWithAdditionalEvents(
            List<OverlayData.RaceEvent> selectedEvents,
            List<OverlayData.RaceEvent> remainingEvents,
            double gap)
        {
            var result = new List<OverlayData.RaceEvent>(selectedEvents);
            var coveredRanges = GetMergedRanges(result);

            // Filter to events that contribute new content (not fully overlapped)
            var contributingEvents = remainingEvents
                .Where(e => CalculateContribution(e, coveredRanges) > MinimumEventContributionSeconds)
                .ToList();

            if (contributingEvents.Count == 0)
                return result;

            TraceInfo.WriteLine("Highlight Edits: Filling gap with {0} candidate events", contributingEvents.Count);

            // Use the same SliceEvent distribution logic to pick additional events
            var additionalEvents = ExtractEditedEvents(gap, 1.0, contributingEvents, InterestState.Battle);
            result.AddRange(additionalEvents);

            return result;
        }

        static double CalculateMergedDuration(IEnumerable<OverlayData.RaceEvent> events)
        {
            var ranges = GetMergedRanges(events);
            return ranges.Sum(r => r.Item2 - r.Item1);
        }

        static List<Tuple<double, double>> GetMergedRanges(IEnumerable<OverlayData.RaceEvent> events)
        {
            var sortedRanges = events
                .Select(e => Tuple.Create(e.StartTime, e.EndTime))
                .OrderBy(r => r.Item1)
                .ToList();

            if (sortedRanges.Count == 0)
                return new List<Tuple<double, double>>();

            var result = new List<Tuple<double, double>>();
            var currentStart = sortedRanges[0].Item1;
            var currentEnd = sortedRanges[0].Item2;

            foreach (var range in sortedRanges.Skip(1))
            {
                if (range.Item1 <= currentEnd)
                {
                    // Overlapping or adjacent, extend current range
                    currentEnd = Math.Max(currentEnd, range.Item2);
                }
                else
                {
                    // Gap found, save current range and start new one
                    result.Add(Tuple.Create(currentStart, currentEnd));
                    currentStart = range.Item1;
                    currentEnd = range.Item2;
                }
            }
            result.Add(Tuple.Create(currentStart, currentEnd));

            return result;
        }

        static double CalculateContribution(OverlayData.RaceEvent re, List<Tuple<double, double>> coveredRanges)
        {
            // Calculate how much of this event's time range is NOT already covered
            var uncoveredParts = new List<Tuple<double, double>> { Tuple.Create(re.StartTime, re.EndTime) };

            foreach (var range in coveredRanges)
            {
                var newUncovered = new List<Tuple<double, double>>();

                foreach (var part in uncoveredParts)
                {
                    var partStart = part.Item1;
                    var partEnd = part.Item2;
                    var rangeStart = range.Item1;
                    var rangeEnd = range.Item2;

                    if (rangeEnd <= partStart || rangeStart >= partEnd)
                    {
                        // No overlap with this part
                        newUncovered.Add(part);
                    }
                    else if (rangeStart <= partStart && rangeEnd >= partEnd)
                    {
                        // Part is fully covered, remove it (don't add to newUncovered)
                    }
                    else if (rangeStart > partStart && rangeEnd < partEnd)
                    {
                        // Range is inside part, split into two uncovered segments
                        newUncovered.Add(Tuple.Create(partStart, rangeStart));
                        newUncovered.Add(Tuple.Create(rangeEnd, partEnd));
                    }
                    else if (rangeStart <= partStart)
                    {
                        // Range overlaps start of part
                        newUncovered.Add(Tuple.Create(rangeEnd, partEnd));
                    }
                    else
                    {
                        // Range overlaps end of part
                        newUncovered.Add(Tuple.Create(partStart, rangeStart));
                    }
                }

                uncoveredParts = newUncovered;
            }

            return uncoveredParts.Sum(p => p.Item2 - p.Item1);
        }

        struct _RaceEvent
        {
            public OverlayData.RaceEvent RaceEvent;
            public int Level;
        }
    }
}
