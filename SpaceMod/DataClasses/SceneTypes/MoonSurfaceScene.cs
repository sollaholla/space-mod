using GTA;
using GTA.Math;
using GTA.Native;
using System.Collections.Generic;

namespace SpaceMod.DataClasses.SceneTypes
{
    public class MoonSurfaceScene : Scene
    {
        /// <summary>
        /// This is the vehicle position on the moons surface.
        /// </summary>
        private readonly Vector3 _vehiclePos = new Vector3(-10028.53f, -12189.84f, 2506.0208f);

        // Fields.
        private OrbitalSystem _planetSystem;
        private Vehicle _playerVehicle;
        private PedGroup _playerPeds;
        private Prop _surface;
        
        public override void Init()
        {
            //Set the player ped group
            _playerPeds = Game.Player.Character.CurrentPedGroup;
            _playerVehicle = PlayerPed.CurrentVehicle;

            if (_playerVehicle != null) _playerVehicle.IsPersistent = true;

            // Give some info.
            Utilities.DisplayHelpTextThisFrame("Go to your vehicle and press ~INPUT_CONTEXT~ to leave the surface.");

            // Create props.
            _surface = World.CreateProp(Constants.MoonSurfaceModel, Vector3.Zero, false, false);
            _surface.FreezePosition = true;
            var earth = World.CreateProp(Constants.EarthSmallModel, Vector3.Zero, false, false);
            var galaxy = World.CreateProp(Constants.SpaceDomeModel, Vector3.Zero, false, false);
            
            // Create the planets list.
            var planets = new List<Orbital>
            {
                new Orbital(earth.Handle, "Earth", _surface, Vector3.Zero, 1, false) /*Earth*/
            };

            // Move the player to the galaxy position.
            MovePlayerToGalaxy(true);

            // Set our positions after setup.
            _surface.Position = Constants.PlanetSurfaceGalaxyCenter;
            earth.Position = _surface.Position + new Vector3(4000, 0, 4000);
            _planetSystem = new OrbitalSystem(galaxy.Handle, planets, new List<LockedOrbital>(), -0.3f);

            // Move the player and give him gravity.
            PlayerPed.HasGravity = true;
            PlayerPosition = _surface.Position + PlayerPed.UpVector;

            //Do above for all the peds in the vehicle.
            foreach (var ped in _playerPeds)
            {
                ped.Position = PlayerPosition.Around(5f);
                ped.HasGravity = true;
            }

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

        // NOTE: Keeping this just in case.
        //File.WriteAllText(@".\scripts\LOGS.txt", $"Vehicle Position = {_playerVehicle.Position}");

        public override void Update()
        {
            // Set the player to super jump mode.
            PlayerPed.SetSuperJumpThisFrame(2.35f, 3, false);

            // Set moon gravity.
            Function.Call(Hash.SET_GRAVITY_LEVEL, 1);

            // Process planets.
            _planetSystem?.Process(Constants.GetValidGalaxyDomePosition(PlayerPed));

            // Attempt to leave the moon.
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
            _playerVehicle.LandingGear = VehicleLandingGear.Retracted;
        }

        private void TryLeaveWithVehicle()
        {
            if (_playerVehicle == null) return;
            if (!_playerVehicle.Exists()) return;

            // Basically when we're in proper distance from the shuttle / vehicle, 
            // we tell the player he can press a button to leave.
            _playerVehicle.LockStatus = VehicleLockStatus.CannotBeTriedToEnter;
            var dist = _playerVehicle.Position.DistanceTo(PlayerPosition);
            if (dist > 20) return;
            Utilities.DisplayHelpTextThisFrame("Press ~INPUT_CONTEXT~ to leave.");
            Game.DisableControlThisFrame(2, Control.Context);
            if (!Game.IsDisabledControlJustPressed(2, Control.Context)) return;

            // Now that we've pressed the button we want to unlock the vehicle and warp the player inside.
            _playerVehicle.LockStatus = VehicleLockStatus.Unlocked;
            PlayerPed.Task.ClearAllImmediately();
            PlayerPed.Task.WarpIntoVehicle(_playerVehicle, VehicleSeat.Driver);

            // Now we load the moon orbit scene...
            End(new MoonOrbitScene(), SceneStartDirection.FromTarget /* ... facing away from the moon. */);
        }
    }
}
