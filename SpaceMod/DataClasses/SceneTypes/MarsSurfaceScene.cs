using System;
using System.Collections.Generic;
using System.Drawing;
using GTA;
using GTA.Native;
using GTA.Math;

namespace SpaceMod.DataClasses.SceneTypes
{
    public class MarsSurfaceScene : Scene
    {
        private const float EnteranceDistance = 1.3f;

        private OrbitalSystem _planetSystem;
        private Vehicle _playerVehicle;
        private Prop _surface;
        private Prop _marsBaseDoor;

        private Ipl _marsBaseInterior;

        /// <summary>
        /// This is the vehicle position on the mars surface.
        /// </summary>
        private readonly Vector3 _vehiclePos = new Vector3(-10028.53f, -12189.84f, 2506.0208f);

        /// <summary>
        /// This is the player position on the mars surface.
        /// </summary>
        private readonly Vector3 _playerPos = new Vector3(-9994.448f, -12171.48f, 2504.697f);

        /// <summary>
        /// This is the position of the enterance to the mars base.
        /// </summary>
        private readonly Vector3 _baseEnterancePos = new Vector3(-9994.323f, -12164.83f, 2505.336f);

        /// <summary>
        /// This the enterance of the interior.
        /// </summary>DrawMarker
        private readonly Vector3 _baseInteriorPos = new Vector3(-1965.833f, 3197.101f, 33.3099f);

        public bool IsIplLoaded => _marsBaseInterior != null && _marsBaseInterior.IsActive;

        public override void Init()
        {
            
            // Set the player vehicle to be persistent.
            _playerVehicle = PlayerPed.CurrentVehicle;
            if (_playerVehicle != null) _playerVehicle.IsPersistent = true;

            // Tell the player how to leave.
            Utilities.DisplayHelpTextThisFrame("Go to your vehicle and press ~INPUT_CONTEXT~ to leave the surface.");

            // Create our props.
            _surface = World.CreateProp(Database.MarsSurfaceModel, Vector3.Zero, false, false);
            var galaxy = World.CreateProp(Database.SpaceDomeModel, Vector3.Zero, false, false);

            // Freeze the surface since it has collisions.
            _surface.FreezePosition = true;

            // Move the surface position.
            _surface.Position = Database.PlanetSurfaceGalaxyCenter;

            // Create the planet sytem.
            _planetSystem = new OrbitalSystem(galaxy.Handle, new List<Orbital>(), new List<LockedOrbital>(), -0.3f);

            // Set the player position.
            PlayerPosition = _playerPos;
            PlayerPed.HasGravity = true;

            // Create the mars base enterence.
            _marsBaseDoor = World.CreateProp(Database.MarsBaseDoor001Model, _surface.Position, false, false);
            _marsBaseDoor.Position += new Vector3(0, 10, 2.7f);
            _marsBaseDoor.Position = new Vector3(-9994.448f, -12161.48f, 2505.8f);
            _marsBaseDoor.Rotation = new Vector3(0, 0, 90);
            _marsBaseDoor.FreezePosition = true;
            DebugLogger.LogEntityData(_marsBaseDoor);

            // Create the interior.
            _marsBaseInterior = new Ipl("mbi2", IplType.MapEditor);

            // Move and configure the player's vehicle.
            if (_playerVehicle == null) return;
            if (!_playerVehicle.Exists()) return;
            _playerVehicle.Speed = 0;
            _playerVehicle.IsInvincible = true;
            _playerVehicle.Heading = -90;
            _playerVehicle.Position = _vehiclePos - _playerVehicle.UpVector * 5;
            DebugLogger.LogEntityData(_playerVehicle);
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
            //ModController.Instance.SetWeatherAndTime(Weather.Clear, _marsBaseInterior.IsActive ? new TimeSpan(0, 0, 0, 0, 0) : new TimeSpan(0, 12, 0, 0, 0));

            // Process planets
            _planetSystem?.Process(Database.GetValidGalaxyDomePosition(PlayerPed));

            // Try to leave with vehicle
            TryLeaveWithVehicle();

            if (!Ipl.AllowTraversal) return;
            // Enter exit the base.
            DrawMarkers();
            TryEnterBase();
            TryLeaveBase();
        }

        public override void Abort()
        {
            Reset();
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

        private void DrawMarkers()
        {
            World.DrawMarker(MarkerType.UpsideDownCone, _baseEnterancePos, Vector3.RelativeRight, Vector3.Zero, new Vector3(0.5f, 0.5f, 0.5f), Color.Purple);
            World.DrawMarker(MarkerType.UpsideDownCone, _baseInteriorPos, Vector3.RelativeRight, Vector3.Zero, new Vector3(0.5f, 0.5f, 0.5f), Color.Purple);
        }

        private void TryEnterBase()
        {
            if (_marsBaseInterior.IsActive) return;
            if (PlayerPosition.DistanceTo(_baseEnterancePos) > EnteranceDistance) return;
            Utilities.DisplayHelpTextThisFrame("Press ~INPUT_CONTEXT~ to go underground.");
            Game.DisableControlThisFrame(2, Control.Talk);
            Game.DisableControlThisFrame(2, Control.Context);
            if (!Game.IsDisabledControlJustPressed(2, Control.Context)) return;
            LoadCurrentIpl(_baseInteriorPos, 90, false);
        }

        private void TryLeaveBase()
        {
            if (!_marsBaseInterior.IsActive) return;
            if (PlayerPosition.DistanceTo(_baseInteriorPos) > EnteranceDistance) return;
            Utilities.DisplayHelpTextThisFrame("Press ~INPUT_CONTEXT~ to leave the base.");
            Game.DisableControlThisFrame(2, Control.Talk);
            Game.DisableControlThisFrame(2, Control.Context);
            if (!Game.IsDisabledControlJustPressed(2, Control.Context)) return;
            LoadCurrentIpl(_baseEnterancePos, 180f, true);
        }

        private void Reset()
        {
            _planetSystem?.Abort();
            _surface?.Delete();
            _marsBaseDoor?.Delete();
            _marsBaseInterior?.Remove();
            Function.Call(Hash.SET_GRAVITY_LEVEL, 3);
            //ModController.Instance.ResetWeatherAndTime();
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

        public void LoadCurrentIpl(Vector3 setPosition, float setHeading, bool remove)
        {
            Game.FadeScreenOut(500);
            Script.Wait(500);

            if (!remove) _marsBaseInterior.Request();
            else _marsBaseInterior.Remove();

            if (setPosition != Vector3.Zero)
                PlayerPosition = setPosition;
            if (Math.Abs(setHeading) > 0.001f)
                PlayerPed.Heading = setHeading;
            GameplayCamera.RelativeHeading = 180;
            GameplayCamera.RelativePitch = 0;
            Script.Wait(500);
            Game.FadeScreenIn(500);
        }

        public void SetIpl(string ipl)
        {
            if (_marsBaseInterior.Name == ipl) return;
            _marsBaseInterior = new Ipl(ipl, IplType.MapEditor);
        }

        public Ipl GetIpl()
        {
            return _marsBaseInterior;
        }
    }
}
