using System;
using System.Collections.Generic;
using System.Drawing;
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

            PlayerPosition = _surface.Position + PlayerPed.UpVector;
            PlayerPed.HasGravity = true;

            

            // Set the weather to make an atmosphere.
            //ModController.Instance.SetWeatherAndTime(Weather.Foggy, new TimeSpan(0, 0, 0, 0));

            // Start dust particles.
            var named = "core";
            var fxName = "env_wind_sand_dune";
            var scale = 20.0f;
            Function.Call(Hash.REQUEST_NAMED_PTFX_ASSET, named);
            Function.Call(Hash._SET_PTFX_ASSET_NEXT_CALL, named);
            var p = PlayerPosition;
            var handle = Function.Call<int>(Hash.START_PARTICLE_FX_LOOPED_ON_ENTITY, fxName, p.X, p.Y, p.Z, 0.0, 0.0, 0.0, scale, false, false, false, 0);
            Function.Call(Hash.SET_PARTICLE_FX_LOOPED_ALPHA, handle, 230f);
            var color = Color.BlanchedAlmond;
            Function.Call(Hash.SET_PARTICLE_FX_LOOPED_COLOUR, handle, color.R, color.G, color.B, false);

            // Configure the vehicle.
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
            ModController.Instance.ResetWeatherAndTime();
            Function.Call(Hash.REMOVE_PARTICLE_FX_FROM_ENTITY, PlayerPed.Handle);
        }

        public override void CleanUp()
        {
            _planetSystem?.Abort();
            _surface?.Delete();
            PlayerPed.HasGravity = false;
            Function.Call(Hash.SET_GRAVITY_LEVEL, 3);
            ModController.Instance.ResetWeatherAndTime();
            Function.Call(Hash.REMOVE_PARTICLE_FX_FROM_ENTITY, PlayerPed.Handle);
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
