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

using iRacingSDK;
using iRacingSDK.Support;
using System.Diagnostics;

namespace iRacingReplayDirector.Phases.Direction
{
    public static class RuleVetoExtensions
    {
        public static IDirectionRule WithVeto(this IVetoRule mainRule, IDirectionRule vetoRule)
        {
            return new RuleVeto(mainRule, vetoRule);
        }
    }

    public class RuleVeto : IDirectionRule
    {
        readonly IVetoRule mainRule;
        readonly IDirectionRule vetoRule;

        bool isVetoed = false;
        private bool wasVetored;

        public RuleVeto(IVetoRule mainRule, IDirectionRule vetoRule)
        {
            this.mainRule = mainRule;
            this.vetoRule = vetoRule;
        }

        public bool IsActive(DataSample data)
        {
            if (isVetoed = vetoRule.IsActive(data))
            {
                Trace.WriteLineIf(!wasVetored, "{0}. Vetoing rule {1} with {2}".F(data.Telemetry.SessionTimeSpan, mainRule.Name, vetoRule.Name));
                wasVetored = true;
                return true;
            }

            return mainRule.IsActive(data);
        }

        public string Name
        {
            get { return isVetoed ? vetoRule.Name : mainRule.Name; }
        }

        public void Direct(DataSample data)
        {
            if (isVetoed)
            {
                vetoRule.Direct(data);
                return;
            }

            if (wasVetored)
                mainRule.Redirect(data);

            wasVetored = false;

            mainRule.Direct(data);
        }
    }
}
