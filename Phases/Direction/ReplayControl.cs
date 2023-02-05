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

using iRacingReplayDirector.Phases.Analysis;
using iRacingReplayDirector.Phases.Capturing;
using iRacingSDK;
using iRacingSDK.Support;
using System.Diagnostics;
using System.Linq;

namespace iRacingReplayDirector.Phases.Direction
{
    public class ReplayControl
    {
        readonly IDirectionRule[] directionRules;
        readonly IVetoRule ruleRandom;
        IDirectionRule currentRule;
        public static CameraControl cameraControl;

        public ReplayControl(SessionData sessionData, Incidents incidents, RemovalEdits removalEdits, TrackCameras trackCameras)
        {
            var cameras = trackCameras.Where(tc => tc.TrackName == sessionData.WeekendInfo.TrackDisplayName).ToArray();

            TraceInfo.WriteLineIf(cameras.Count() <= 0, "Track Cameras not defined for {0}", sessionData.WeekendInfo.TrackDisplayName);
            Debug.Assert(cameras.Count() > 0, "Track Cameras not defined for {0}".F(sessionData.WeekendInfo.TrackDisplayName));

            foreach (var tc in cameras)
                tc.CameraNumber = (short)sessionData.CameraInfo.Groups.First(g => g.GroupName.ToLower() == tc.CameraName.ToLower()).GroupNum;

            var camera = cameras.First(tc => tc.IsRaceStart);

            //var cameraControl = new CameraControl(cameras);
            cameraControl = new CameraControl(cameras);
            cameraControl.CameraOnPositon(1, camera.CameraNumber);

            var battleMarker = removalEdits.For(InterestState.Battle);
            var restartMarker = removalEdits.For(InterestState.Restart);

            var ruleLastSectors = new RuleLastLapPeriod(cameraControl, removalEdits);
            var ruleUnlimitedIncident = new RuleIncident(cameraControl, removalEdits, incidents, 999);
            var ruleLimitedIncident = new RuleIncident(cameraControl, removalEdits, incidents, Settings.Default.IgnoreIncidentsBelowPosition);
            var ruleFirstSectors = new RuleFirstLapPeriod(cameraControl, removalEdits);
            var rulePaceLaps = new RulePaceLaps(cameraControl, restartMarker, battleMarker);
            var ruleBattle = new RuleBattle(cameraControl, battleMarker, Settings.Default.CameraStickyPeriod, Settings.Default.BattleStickyPeriod, Settings.Default.BattleGap, Settings.Default.BattleFactor2);
            ruleRandom = new RuleRandomDriver(cameraControl, sessionData, Settings.Default.CameraStickyPeriod);

            var ruleForFirstSectors = Settings.Default.IgnoreIncidentsDuringRaceStart ? ruleFirstSectors : ruleFirstSectors.WithVeto(ruleUnlimitedIncident);

            directionRules = new IDirectionRule[] {
                ruleLastSectors,
                ruleForFirstSectors,
                rulePaceLaps.WithVeto(ruleUnlimitedIncident.WithVeto(ruleLastSectors)),
                ruleBattle.WithVeto(ruleLimitedIncident.WithVeto(ruleLastSectors)),
                ruleUnlimitedIncident.WithVeto(ruleLastSectors),
                ruleRandom.WithVeto(ruleLastSectors)
            };

            currentRule = directionRules[0];
        }

        public void Process(DataSample data)
        {
            if (ActiveRule(currentRule, data))
                return;

            foreach (var rule in directionRules)
                if (ActiveRule(rule, data))
                    return;

            currentRule = ruleRandom;
            currentRule.Direct(data);
        }

        bool ActiveRule(IDirectionRule rule, DataSample data)
        {
            if (rule.IsActive(data))
            {
                currentRule = rule;
                rule.Direct(data);
                return true;
            }

            return false;
        }
    }
}
