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
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace iRacingReplayDirector.Phases.Direction
{
    public class RuleIncident : IVetoRule
    {
        enum IncidentPosition { Started, Inside, Finished, Outside };

        readonly CameraControl cameraControl;
        readonly EditMarker editMarker;
        readonly IEnumerator<Incidents.Incident> nextIncident;
        readonly int limitDownTo;

        double pitBoxStartTime = 0;
        bool isInside = false;
        Action directionAction;

        public RuleIncident(CameraControl cameraControl, RemovalEdits removalEdits, Incidents incidents, int limitDownTo)
        {
            this.editMarker = removalEdits.For(InterestState.Incident);
            this.cameraControl = cameraControl;
            this.limitDownTo = limitDownTo;

            nextIncident = incidents.GetEnumerator();
            nextIncident.MoveNext();
            if (nextIncident.Current != null)
                TraceInfo.WriteLine("First incident at {0}", nextIncident.Current.StartSessionTime);
        }

        public bool IsActive(DataSample data)
        {
            var position = GetIncidentPosition(data);

            switch (position)
            {
                case IncidentPosition.Started:
                    directionAction = () =>
                    {
                        editMarker.Start(data.Telemetry.Positions[nextIncident.Current.Car.CarIdx]);
                        SwitchToIncident(data);
                    };
                    return true;

                case IncidentPosition.Inside:
                    directionAction = () => { };
                    return true;

                case IncidentPosition.Finished:
                    directionAction = () =>
                    {
                        editMarker.Stop();
                        WatchForNextIncident(data);
                    };
                    return true;

                case IncidentPosition.Outside:
                    directionAction = () => { };
                    return false;
            }

            throw new Exception("Invalid Incident Position {0}".F(position));
        }

        public void Direct(DataSample data)
        {
            directionAction();
        }

        public void Redirect(DataSample data)
        {
            // Do nothing if we got directed away from an incident
        }

        IncidentPosition GetIncidentPosition(DataSample data)
        {
            if (nextIncident.Current == null)
                return IncidentPosition.Outside;

            if (!isInside)
                SkipMissedIncidents(data);

            if (nextIncident.Current == null)
                return IncidentPosition.Outside;

            var incident = nextIncident.Current;

            if (!isInside && incident.IsInside(data.Telemetry.SessionTimeSpan))
            {
                isInside = true;
                return IncidentPosition.Started;
            }

            if (isInside && (!incident.IsInside(data.Telemetry.SessionTimeSpan) || IsInPits(data)))
            {
                isInside = false;
                return IncidentPosition.Finished;
            }

            return isInside ? IncidentPosition.Inside : IncidentPosition.Outside;
        }

        bool IsInPits(DataSample data)
        {
            if (data.Telemetry.CamCar.TrackSurface == TrackLocation.InPitStall && pitBoxStartTime == 0)
            {
                Trace.WriteLine("{0} Incident car is in pit stall".F(data.Telemetry.SessionTimeSpan));
                pitBoxStartTime = data.Telemetry.SessionTime;
            }

            if (data.Telemetry.CamCar.TrackSurface == TrackLocation.InPitStall && pitBoxStartTime + 2 < data.Telemetry.SessionTime)
            {
                Trace.WriteLine("{0} Finishing showing incident as car is in pit stall".F(data.Telemetry.SessionTimeSpan));
                return true;
            }

            return false;
        }

        void WatchForNextIncident(DataSample data)
        {
            TraceInfo.WriteLine("{0} Finishing incident from {1}", data.Telemetry.SessionTimeSpan, nextIncident.Current.StartSessionTime);

            nextIncident.MoveNext();

            if (nextIncident.Current != null)
                TraceInfo.WriteLine("{0} (Move) Next incident at {1}", data.Telemetry.SessionTimeSpan, nextIncident.Current.StartSessionTime);
        }

        void SkipMissedIncidents(DataSample data)
        {
            var incident = nextIncident.Current;

            while (incident != null && (incident.StartSessionTime + 1.Seconds()) < data.Telemetry.SessionTimeSpan)
            {
                TraceInfo.WriteLine("{0} Skipping incident with {1} at time {2}", data.Telemetry.SessionTimeSpan, incident.Car.Driver.UserName, nextIncident.Current.StartSessionTime);
                nextIncident.MoveNext();
                incident = nextIncident.Current;
            }

            while (incident != null && incident.IsInside(data.Telemetry.SessionTimeSpan) && incident.Car.Car(data).Position > this.limitDownTo)
            {
                TraceInfo.WriteLine("{0} Skipping incident with {1} due to position of {2} below {3}", data.Telemetry.SessionTimeSpan, incident.Car.Driver.UserName, incident.Car.Car(data).Position, this.limitDownTo);
                nextIncident.MoveNext();
                incident = nextIncident.Current;
            }
        }

        void SwitchToIncident(DataSample data)
        {
            pitBoxStartTime = 0;

            var incidentCar = nextIncident.Current.Car;

            TraceInfo.WriteLine("{0} Showing incident with {1} starting from {2}", data.Telemetry.SessionTimeSpan, incidentCar.UserName, nextIncident.Current.StartSessionTime);

            cameraControl.CameraOnDriver(incidentCar.CarNumberRaw, cameraControl.IncidentCameraNumber);
        }

        public string Name
        {
            get { return GetType().Name; }
        }
    }
}