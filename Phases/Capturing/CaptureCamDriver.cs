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
// You should have received a copy of the GNU General Public License333
// along with iRacingReplayDirector.  If not, see <http://www.gnu.org/licenses/>.

using iRacingReplayDirector.Support;
using iRacingSDK;
using iRacingSDK.Support;
using System;
using System.Linq;

namespace iRacingReplayDirector.Phases.Capturing
{
    public class CaptureCamDriver
    {
        readonly OverlayData overlayData;

        public CaptureCamDriver(OverlayData overlayData)
        {
            this.overlayData = overlayData;
        }

        OverlayData.Driver lastCamDriver = null;

        public void Process(DataSample data, TimeSpan relativeTime)
        {
            var camDriver = CreateCamDriver(data, relativeTime);
            if (camDriver != null)
            {
                if (lastCamDriver == null ||
                    lastCamDriver.UserName != camDriver.CurrentDriver.UserName ||
                    lastCamDriver.Position != camDriver.CurrentDriver.Position)
                {
                    var position = camDriver.CurrentDriver.Position != null ? camDriver.CurrentDriver.Position.Value.ToString() : "";
                    var indicator = camDriver.CurrentDriver.Position != null ? camDriver.CurrentDriver.Position.Value.Ordinal() : "";

                    TraceInfo.WriteLine("{0} Camera on {1} {2} in position {3}{4}",
                        data.Telemetry.SessionTimeSpan,
                        camDriver.CurrentDriver.UserName,
                        camDriver.CurrentDriver.CarNumber,
                        position, indicator);

                    lastCamDriver = camDriver.CurrentDriver;
                    //add camDriver to list only if either drivername or position has changed
                    overlayData.CamDrivers.Add(camDriver);
                }
            }
        }

        OverlayData.CamDriver CreateCamDriver(DataSample data, TimeSpan relativeTime)
        {
            var driver = GetCurrentDriverDetails(data);

            if (driver == null)
                return null;

            return new OverlayData.CamDriver
            {
                StartTime = relativeTime.TotalSeconds,
                CurrentDriver = driver,
                camGroupNumber = data.Telemetry.CamGroupNumber     //get current, active camera group from telemetry data
            };
        }

        static OverlayData.Driver GetCurrentDriverDetails(DataSample data)
        {
            if (data.Telemetry.CamCar == null)
                return null;

            var car = data.Telemetry.CamCar;

            var position = GetPositionFor(data, car.Details);

            var driver = new OverlayData.Driver
            {
                CarIdx = car.CarIdx,
                CarNumber = car.Details.CarNumberDisplay,
                UserName = car.Details.UserName,
                Position = position,
                PitStopCount = car.PitStopCount

            };

            return driver;
        }

        private static int? GetPositionFor(DataSample data, CarDetails carDetails)
        {
            var car = carDetails.Car(data);

            if (data.Telemetry.RaceDistance > 1.10)
                return car.Position;

            var session = data.SessionData.SessionInfo.Sessions.Qualifying();
            if (session == null || session.ResultsPositions == null)
                return null;

            var qualifyingResult = session.ResultsPositions.FirstOrDefault(p => p.CarIdx == car.CarIdx);
            if (qualifyingResult == null)
                return null;

            return (int)qualifyingResult.Position;
        }
    }
}
