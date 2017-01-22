using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;

namespace SpaceMod.DataClasses.SceneTypes
{
    public class MoonSurfaceScene : Scene
    {
        private PlanetSystem _planetSystem;
        private Vehicle _playerVehicle;
        private Prop _surface;

        public override void Init()
        {
            _playerVehicle = PlayerPed.CurrentVehicle;
            if (_playerVehicle != null) _playerVehicle.IsPersistent = true;

            Utilities.DisplayHelpTextThisFrame("Go to your vehicle and press ~INPUT_CONTEXT~ to leave the surface.");

            _surface = World.CreateProp(Constants.MoonSurfaceModel, Vector3.Zero, false, false);
            var earth = World.CreateProp(Constants.EarthSmallModel, Vector3.Zero, false, false);
            var galaxy = World.CreateProp(Constants.SpaceDomeModel, Vector3.Zero, false, false);
            _surface.FreezePosition = true;

            ResetPlayerOrigin();

            var planets = new List<Planet>
            {
                new Planet(earth.Handle, _surface, new Vector3(0.005f, 0, 0), 1)
            };

            TeleportPlayerToGalaxy();
            
            _surface.Position = Constants.GalaxyCenter;

            earth.Position = _surface.Position + new Vector3(4000, 0, 4000);
            _planetSystem = new PlanetSystem(galaxy.Handle, planets, new List<Star>(), -0.3f, RotationAxis.Y);

            Function.Call(Hash.SET_GRAVITY_LEVEL, 0);

            PlayerPed.Position = _surface.Position + PlayerPed.UpVector;
            PlayerPed.HasGravity = true;
            
            if (_playerVehicle == null) return;
            if (!_playerVehicle.Exists()) return;
            _playerVehicle.Position = PlayerPosition + PlayerPed.UpVector * 5;
            _playerVehicle.FreezePosition = true;
            _playerVehicle.Speed = 0;
            _playerVehicle.IsInvincible = true;
        }

        public override void Update()
        {
            _planetSystem?.Process(Constants.GetValidGalaxyDomePosition(PlayerPed));
            TryLeaveWithVehicle();
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
            End(new MoonOrbitScene());
        }

        public override void Abort()
        {
            _planetSystem?.Abort();
            _surface?.Delete();
        }

        public override void CleanUp()
        {
            _planetSystem?.Abort();
            _surface?.Delete();
            PlayerPed.HasGravity = false;
            Function.Call(Hash.SET_GRAVITY_LEVEL, 3);
            if (_playerVehicle == null) return;
            if (!_playerVehicle.Exists()) return;
            _playerVehicle.FreezePosition = false;
            _playerVehicle.IsInvincible = false;
        }
    }
}
