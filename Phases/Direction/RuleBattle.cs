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
using iRacingReplayDirector.Phases.Direction.Support;
using iRacingSDK;
using iRacingSDK.Support;
using System;
using System.Linq;

namespace iRacingReplayDirector.Phases.Direction
{
    public class RuleBattle : IVetoRule
    {
        enum BattlePosition { Started, Inside, Finished, Outside };
        struct BattleState
        {
            public BattleState(BattlePosition state) : this(state, null) { }
            public BattleState(BattlePosition state, CarDetails driver)
            {
                this.State = state;
                this.Driver = driver;
            }
            public BattlePosition State;
            public CarDetails Driver;
        }

        readonly CameraControl cameraControl;
        readonly EditMarker editMarker;
        readonly TimeSpan battleStickyPeriod;
        readonly TimeSpan battleCameraChangePeriod;
        readonly TimeSpan battleGap;
        readonly double battleFactor;

        bool isInBattle = false;
        TimeSpan battleEndTime;
        TimeSpan cameraChangeTime;
        CarDetails battleFollower;
        CarDetails battleLeader;
        Action directionAction;
        TrackCamera camera;
        CarDetails car;
        private TrackCamera adjustedCamera;

        public RuleBattle(CameraControl cameraControl, EditMarker editMarker, TimeSpan cameraStickyPeriod, TimeSpan battleStickyPeriod, TimeSpan battleGap, double battleFactor)
        {
            this.cameraControl = cameraControl;
            this.editMarker = editMarker;
            this.battleStickyPeriod = battleStickyPeriod;
            this.battleCameraChangePeriod = cameraStickyPeriod;
            this.battleGap = battleGap;
            this.battleFactor = battleFactor;
        }

        public bool IsActive(DataSample data)
        {
            var state = GetBattlePosition(data);

            switch (state.State)
            {
                case BattlePosition.Started:
                    directionAction = () =>
                    {
                        adjustedCamera = null;
                        SwitchToBattle(data, state.Driver, establishingShot: true);
                        editMarker.Start(data.Telemetry.Positions[battleLeader.CarIdx]);
                    };
                    return true;

                case BattlePosition.Inside:
                    directionAction = () =>
                    {
                        UpdateBattleCamera(data);
                        UpdateCameraIfOvertake(data);
                    };
                    return true;

                case BattlePosition.Finished:
                    directionAction = () => editMarker.Stop();
                    return true;

                case BattlePosition.Outside:
                    directionAction = () => { };
                    return false;
            }

            throw new Exception("Invalid Battle state {0}".F(state));
        }

        void UpdateBattleCamera(DataSample data)
        {
            if (data.Telemetry.SessionTimeSpan <= cameraChangeTime)
                return;

            cameraChangeTime = data.Telemetry.SessionTimeSpan + this.battleCameraChangePeriod;

            var otherCar = ChangeCarForCamera(data, battleFollower);

            var cameraSet = otherCar == null ? new[] { CameraAngle.LookingInfrontOfCar, CameraAngle.LookingAtCar } : new[] { CameraAngle.LookingInfrontOfCar, CameraAngle.LookingBehindCar, CameraAngle.LookingAtCar };
            camera = cameraControl.FindACamera(cameraSet, adjustedCamera, 2);
            adjustedCamera = null;

            if (camera.CameraAngle == CameraAngle.LookingBehindCar)
            {
                TraceInfo.WriteLine("{0} Changing to forward car, with reverse camera", data.Telemetry.SessionTimeSpan);
                car = otherCar;
            }
            else
            {
                car = battleFollower;
            }
            TraceInfo.WriteLine("{0} Changing camera to driver: {1}; camera: {2}", data.Telemetry.SessionTimeSpan, car.UserName, camera.CameraName);
            cameraControl.CameraOnDriver(car.CarNumberRaw, camera.CameraNumber);
        }

        public void Direct(DataSample data)
        {
            directionAction();
        }

        public void Redirect(DataSample data)
        {
            if (car == null)
                return;

            TraceInfo.WriteLine("{0} Changing camera back to driver: {1}; camera: {2}; within {3}", data.Telemetry.SessionTimeSpan, car.UserName, camera.CameraName, battleGap);
            cameraControl.CameraOnDriver(car.CarNumberRaw, camera.CameraNumber);
        }

        void SwitchToBattle(DataSample data, CarDetails newFollower, bool establishingShot = false)
        {
            if (establishingShot)
                TraceInfo.WriteLine("{0} New Battle started - Searching for a LookingAtCar camera", data.Telemetry.SessionTimeSpan);

            var cameraTypes = establishingShot ? new[] { CameraAngle.LookingAtCar } : new[] { CameraAngle.LookingInfrontOfCar, CameraAngle.LookingBehindCar, CameraAngle.LookingAtCar };

            var newFollowerPosition = newFollower.Car(data).Position;
            battleLeader = data.Telemetry.Cars.First(c => c.Position == newFollowerPosition - 1).Details;
            battleFollower = newFollower;

            car = newFollower;
            camera = cameraControl.FindACamera(cameraTypes, adjustedCamera, 2);
            if (establishingShot)
                adjustedCamera = camera;
            else
                adjustedCamera = null;

            if (camera.CameraAngle == CameraAngle.LookingBehindCar)
            {
                TraceInfo.WriteLine("{0} Changing to forward car, with reverse camera", data.Telemetry.SessionTimeSpan);
                car = battleLeader;
            }

            TraceInfo.WriteLine("{0} Changing camera to driver: {1}; camera: {2}; within {3}",
                data.Telemetry.SessionTimeSpan,
                car.UserName,
                camera.CameraName,
                battleGap);

            cameraControl.CameraOnDriver(car.CarNumberRaw, camera.CameraNumber);
        }

        void UpdateCameraIfOvertake(DataSample data)
        {
            if (BattlersHaveSwappedPositions(data))
            {
                battleEndTime = data.Telemetry.SessionTimeSpan + this.battleStickyPeriod;
                TraceInfo.WriteLine("{0} {1} has overtaken {2}", data.Telemetry.SessionTimeSpan, battleFollower.UserName, battleLeader.UserName);
                SwitchToBattle(data, battleLeader);
                editMarker.WithOvertake();
            }
        }

        bool BattlersHaveSwappedPositions(DataSample data)
        {
            var followersLatestPosition = data.Telemetry.Cars[battleFollower.CarIdx].Position;
            var leadersLatestPosition = data.Telemetry.Cars[battleLeader.CarIdx].Position;

            return followersLatestPosition < leadersLatestPosition;
        }

        static CarDetails ChangeCarForCamera(DataSample data, CarDetails driver)
        {
            if (driver == null)
                return null;

            var car = driver.Car(data);

            var otherCar = data.Telemetry.Cars.FirstOrDefault(c => c.Position == car.Position - 1);
            if (otherCar == null)
                return null;

            return otherCar.Details;
        }

        BattleState GetBattlePosition(DataSample data)
        {
            if (isInBattle)
            {
                if (data.Telemetry.UnderPaceCar)
                    return new BattleState(BattlePosition.Finished);

                if (!HasBattleTimeout(data))
                    return new BattleState(BattlePosition.Inside);

                return SearchForNextBattle(data, notFound: () => new BattleState(BattlePosition.Finished));
            }

            if (data.Telemetry.UnderPaceCar)
                return new BattleState(BattlePosition.Outside);

            return SearchForNextBattle(data, notFound: () => new BattleState(BattlePosition.Outside));
        }

        BattleState SearchForNextBattle(DataSample data, Func<BattleState> notFound)
        {
            var battleDriver = Battle.Find(data, battleGap, battleFactor, Settings.Default.PreferredDrivers);
            if (battleDriver == null)
            {
                isInBattle = false;
                return notFound();
            }

            isInBattle = true;
            battleEndTime = data.Telemetry.SessionTimeSpan + this.battleStickyPeriod;
            cameraChangeTime = data.Telemetry.SessionTimeSpan + this.battleCameraChangePeriod;
            return new BattleState(BattlePosition.Started, battleDriver.Details);
        }

        bool HasBattleTimeout(DataSample data)
        {
            if (data.Telemetry.SessionTimeSpan > cameraChangeTime && !Battle.IsInBattle(data, battleGap, battleFollower, battleLeader))
            {
                TraceInfo.WriteLine("{0} Battle has stopped.", data.Telemetry.SessionTimeSpan);
                return true;
            }

            if (data.Telemetry.UnderPaceCar)
            {
                TraceInfo.WriteLine("{0} Battle has stopped, due to double yellows.", data.Telemetry.SessionTimeSpan);
                return true;
            }

            return data.Telemetry.SessionTimeSpan > battleEndTime;
        }

        public string Name
        {
            get { return GetType().Name; }
        }
    }
}
