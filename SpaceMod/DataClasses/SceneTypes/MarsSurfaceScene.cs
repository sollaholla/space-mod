using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;
using GTA.Math;

namespace SpaceMod.DataClasses.SceneTypes
{
    public class MarsSurfaceScene : Scene
    {
        private OrbitalSystem _planetSystem;
        private Vehicle _playerVehicle;
        private Prop _surface;
        private Prop _marsBaseDoor;

        /// <summary>
        /// This is the vehicle position on the mars surface.
        /// </summary>
        private readonly Vector3 _vehiclePos = new Vector3(-10028.53f, -12189.84f, 2506.0208f);

        /// <summary>
        /// This is the player position on the mars surface.
        /// </summary>
        private readonly Vector3 _playerPos = new Vector3(-9994.448f, -12171.48f, 2504.697f);

        public override void Init()
        {
            
            // Set the player vehicle to be persistent.
            _playerVehicle = PlayerPed.CurrentVehicle;
            if (_playerVehicle != null) _playerVehicle.IsPersistent = true;

            // Tell the player how to leave.
            Utilities.DisplayHelpTextThisFrame("Go to your vehicle and press ~INPUT_CONTEXT~ to leave the surface.");

            // Create our props.
            _surface = World.CreateProp(Constants.MarsSurfaceModel, Vector3.Zero, false, false);
            var galaxy = World.CreateProp(Constants.SpaceDomeModel, Vector3.Zero, false, false);

            // Freeze the surface since it has collisions.
            _surface.FreezePosition = true;

            // Move the surface position.
            _surface.Position = Constants.PlanetSurfaceGalaxyCenter;

            // Create the planet sytem.
            _planetSystem = new OrbitalSystem(galaxy.Handle, new List<Orbital>(), new List<LockedOrbital>(), -0.3f);

            // Set the player position.
            PlayerPosition = _playerPos;
            PlayerPed.HasGravity = true;

            // Create the mars base enterence.
            _marsBaseDoor = World.CreateProp(Constants.MarsBaseDoor001Model, _surface.Position, false, false);
            _marsBaseDoor.Position += new Vector3(0, 10, 2.7f);
            _marsBaseDoor.FreezePosition = true;

            // Move and configure the player's vehicle.
            if (_playerVehicle == null) return;
            if (!_playerVehicle.Exists()) return;
            _playerVehicle.Speed = 0;
            _playerVehicle.IsInvincible = true;
            _playerVehicle.Heading = -90;
            _playerVehicle.Position = _vehiclePos - _playerVehicle.UpVector * 5;
            _playerVehicle.LandingGear = VehicleLandingGear.Deployed;

            // HACK: Apperantly in gta when you move an object while the screen is black it 
            // stays stuck in place so we are pushing it downward.
            _playerVehicle.ApplyForce(Vector3.WorldDown);
        }

        public override void Update()
        {
            // Set mars gravity
            Function.Call(Hash.SET_GRAVITY_LEVEL, 1);

            // Set mars time
            ModController.Instance.SetWeatherAndTime(Weather.Clear, new TimeSpan(0, 12, 0, 0, 0));

            // Process planets
            _planetSystem?.Process(Constants.GetValidGalaxyDomePosition(PlayerPed));

            // Try to leave with vehicle
            TryLeaveWithVehicle();
        }

        public override void Abort()
        {
            Reset();
        }

        private void Reset()
        {
            _planetSystem?.Abort();
            _surface?.Delete();
            _marsBaseDoor?.Delete();
            Function.Call(Hash.SET_GRAVITY_LEVEL, 3);
            ModController.Instance.ResetWeatherAndTime();
        }

        public override void CleanUp()
        {
            PlayerPed.HasGravity = false;

            Reset();

            if (_playerVehicle == null) return;
            if (!_playerVehicle.Exists()) return;
            _playerVehicle.FreezePosition = false;
            _playerVehicle.IsInvincible = false;
        }

        private void TryLeaveWithVehicle()
        {
            if (_playerVehicle == null) return;
            if (!_playerVehicle.Exists()) return;

            _playerVehicle.LockStatus = VehicleLockStatus.CannotBeTriedToEnter;
            var dist = _playerVehicle.Position.DistanceTo(PlayerPosition);
            if (dist > 20) return;
            Utilities.DisplayHelpTextThisFrame("Press ~INPUT_CONTEXT~ to leave.");
            Game.DisableControlThisFrame(2, Control.Context);
            if (!Game.IsDisabledControlJustPressed(2, Control.Context)) return;
            _playerVehicle.LockStatus = VehicleLockStatus.Unlocked;
            PlayerPed.Task.ClearAllImmediately();
            PlayerPed.Task.WarpIntoVehicle(_playerVehicle, VehicleSeat.Driver);
            End(new MarsOrbitScene(), SceneStartDirection.FromTarget);
        }
    }
}
