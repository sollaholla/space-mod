using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public override void Init()
        {
            _playerVehicle = PlayerPed.CurrentVehicle;
            if (_playerVehicle != null) _playerVehicle.IsPersistent = true;

            Utilities.DisplayHelpTextThisFrame("Go to your vehicle and press ~INPUT_CONTEXT~ to leave the surface.");

            _surface = World.CreateProp(Constants.MarsSurfaceModel, Vector3.Zero, false, false);
            var galaxy = World.CreateProp(Constants.SpaceDomeModel, Vector3.Zero, false, false);

            _surface.FreezePosition = true;

            ResetPlayerOrigin();

            _surface.Position = Constants.PlanetSurfaceGalaxyCenter;

            _planetSystem = new OrbitalSystem(galaxy.Handle, new List<Orbital>(), new List<LockedOrbital>(), -0.3f);

            PlayerPosition = _surface.Position + PlayerPed.UpVector * 4;
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
            Function.Call(Hash.SET_GRAVITY_LEVEL, 1);
            _planetSystem?.Process(Constants.GetValidGalaxyDomePosition(PlayerPed));
            TryLeaveWithVehicle();
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
