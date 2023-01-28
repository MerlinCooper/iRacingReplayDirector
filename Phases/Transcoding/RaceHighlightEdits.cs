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

        public static List<VideoEdit> GetRaceEdits(this IEnumerable<OverlayData.RaceEvent> raceEvents)
        {
            var edits = raceEvents._GetRaceEdits().ToList();

            foreach (var e in edits)
                TraceInfo.WriteLine("Editing from {0} to {1}. Duration {2}", e.StartTimeSpan, e.EndTimeSpan, e.Duration);

            TraceInfo.WriteLine("Total Edits time {0}", edits.Sum(e => e.Duration).Seconds());

            return edits;
        }
        
        static IEnumerable<VideoEdit> _GetRaceEdits(this IEnumerable<OverlayData.RaceEvent> raceEvents)
        {
            var totalRaceEvents = GetInterestingRaceEvents(raceEvents);

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
        }

        public static IOrderedEnumerable<OverlayData.RaceEvent> GetInterestingRaceEvents(IEnumerable<OverlayData.RaceEvent> raceEvents, Boolean bFastRecording = false)
        {
            TraceInfo.WriteLine("Highlight Edits: Total Duration Target: {0}", HighlightVideoDuration);

            double totalTime, incidentsRatio, restartsRatio, battlesRatio, timeForRaceEvents;
            var firstAndLastLapRaceEvents = GetAllFirstAndLastLapEvents(raceEvents, out totalTime);
            
            var incidentRaceEvents = GetAllRaceEvents(raceEvents, InterestState.Incident, 1.8, 0, out incidentsRatio);
            var restartRaceEvents = GetAllRaceEvents(raceEvents, InterestState.Restart, 1.0, 0, out restartsRatio);
            var battleRaceEvents = GetAllRaceEvents(raceEvents, InterestState.Battle, 1.4, 15, out battlesRatio);

            battleRaceEvents = NormaliseBattleEvents(battleRaceEvents, Settings.Default.BattleStickyPeriod.TotalSeconds);

            //Calculate time for incidents, battles, restarts when fastrecording is active 
            if(bFastRecording)
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

            var editedEvents = firstAndLastLapRaceEvents.Concat(incidentsEdited).Concat(restartsEdited).Concat(battlessEdited).OrderBy(re => re.StartTime);

            TraceInfo.WriteLine("Highlight Edits: Expected duration of highlight video: {0}", editedEvents.Sum(re => re.Duration).Seconds());

            return editedEvents;
        }

        private static List<OverlayData.RaceEvent> NormaliseBattleEvents(List<OverlayData.RaceEvent> raceEvents, double maxDuration)
        {
            var result = new List<OverlayData.RaceEvent>();

            foreach( var re in raceEvents)
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

        static List<OverlayData.RaceEvent> GetAllFirstAndLastLapEvents(IEnumerable<OverlayData.RaceEvent> raceEvents, out double totalTime)
        {
            var firstAndLastLapRaceEvents = raceEvents
                .Where(re => re.Interest == InterestState.FirstLap || re.Interest == InterestState.LastLap)
                .ToList();
            var firstAndLastLapDuration = firstAndLastLapRaceEvents.Sum(re => re.Duration);
            totalTime = HighlightVideoDuration.TotalSeconds - firstAndLastLapDuration;

            TraceInfo.WriteLine("Highlight Edits: First & last laps.  Duration: {0}. Remaining: {1}", firstAndLastLapDuration.Seconds(), totalTime.Seconds());
        
            return firstAndLastLapRaceEvents;
        }

        static void SliceEvent(List<_RaceEvent> result, List<OverlayData.RaceEvent> raceEvents, OverlayData.RaceEvent left, OverlayData.RaceEvent right, int level = 1)
        {
            var middleTime = (left.EndTime + right.StartTime) / 2;

            var middleEvent = raceEvents
                .Where( r => r.StartTime >= left.StartTime && r.StartTime <= right.EndTime)
                .OrderBy(r => Math.Abs(middleTime - r.StartTime)).FirstOrDefault();

            if( middleEvent == null )
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

        struct _RaceEvent
        {
            public OverlayData.RaceEvent RaceEvent;
            public int Level;
        }
    }
}
