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
using System;
using System.Collections.Generic;
using System.Linq;

namespace iRacingReplayDirector.Phases.Direction
{
    public class RuleRandomDriver : IVetoRule
    {
        readonly CameraControl cameraControl;
        readonly SessionData sessionData;
        readonly TimeSpan stickyTime;
        readonly long[] allCarIndexes;        
        readonly long[] preferredCarIndexes;
        readonly Random randomDriverNumber;

        bool isWatchingRandomDriver;
        TimeSpan finishWatchingRandomDriverAt;
        CarDetails car;
        TrackCamera camera;

        public RuleRandomDriver(CameraControl cameraControl, SessionData sessionData, TimeSpan stickyTime)
        {
            this.cameraControl = cameraControl;
            this.sessionData = sessionData;
            this.stickyTime = stickyTime;

            allCarIndexes = sessionData.DriverInfo.CompetingDrivers.Where(x => !x.IsPaceCar).Select(x => x.CarIdx).ToArray();

            if (Settings.Default.PreferredDriverNames != null && Settings.Default.PreferredDriverNames.Length > 0)
            {
                preferredCarIndexes = sessionData.DriverInfo.CompetingDrivers
                    .Where(x => Settings.Default.PreferredDrivers.Contains(x.UserName.ToLower()))
                    .Select(x => x.CarIdx)
                    .ToArray();
            }
            else
                preferredCarIndexes = allCarIndexes;

            randomDriverNumber = new Random();
        }

        public bool IsActive(DataSample data)
        {
            if (isWatchingRandomDriver && data.Telemetry.SessionTimeSpan < finishWatchingRandomDriverAt)
                return true;

            isWatchingRandomDriver = false;
            return false;
        }

        public void Direct(DataSample data)
        {
            if (isWatchingRandomDriver)
                return;

            isWatchingRandomDriver = true;

            finishWatchingRandomDriverAt = data.Telemetry.SessionTimeSpan + stickyTime;

            //code to be added that pit-lane camera is only choosen if car is in the pit-lane / not on track
            car = FindADriver(data);
            do
            {
                camera = cameraControl.FindACamera(new[] { CameraAngle.LookingInfrontOfCar, CameraAngle.LookingAtCar, CameraAngle.LookingAtTrack });
                if((camera.CameraName == "Pit Lane") && !car.IsOnPitRoad) 
                    TraceInfo.WriteLine("Changing camera again because driver {0} would not be captured by camera: {1} | On-PitRoad: {2}", car.UserName, camera.CameraName, car.IsOnPitRoad);
            } while ((camera.CameraName == "Pit Lane") && !car.IsOnPitRoad);
            
            

            TraceInfo.WriteLine("{0} Changing camera to random driver: {1}; camera: {2}", data.Telemetry.SessionTimeSpan, car.UserName, camera.CameraName);
            cameraControl.CameraOnDriver((short)car.CarNumberRaw, camera.CameraNumber);
        }

        public void Redirect(DataSample data)
        {
            TraceInfo.WriteLine("{0} Changing camera back to driver: {1}; camera: {2}", data.Telemetry.SessionTimeSpan, car.UserName, camera.CameraName);
            cameraControl.CameraOnDriver((short)car.CarNumberRaw, camera.CameraNumber);
        }

        CarDetails FindADriver(DataSample data)
        {
            var activeDrivers = GetDriversOnTrack(data, preferredCarIndexes);

            if( activeDrivers.Count == 0)
                activeDrivers = GetDriversOnTrack(data, allCarIndexes);

            var next = randomDriverNumber.Next(activeDrivers.Count);

            return activeDrivers[next];
        }

        private List<CarDetails> GetDriversOnTrack(DataSample data, long[] carIndexes)
        {
            return carIndexes
                .Select(carIdx => data.Telemetry.Cars[carIdx])
                .Where(c => c.HasData && c.TrackSurface != TrackLocation.InPitStall)
                .Select(c => c.Details)
                .ToList();
        }

        public string Name
        {
            get { return GetType().Name; }
        }
    }
}
