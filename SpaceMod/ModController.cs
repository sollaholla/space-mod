using System;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using SpaceMod.DataClasses;
using SpaceMod.DataClasses.SceneTypes;
using Control = GTA.Control;

namespace SpaceMod
{
    public class ModController : Script
    {
        private Scene _currentScene;
        private Mission _currentMission;
        private Prop _spaceCraft;

        public ModController()
        {
            Instance = this;
            KeyUp += OnKeyUp;
            Tick += OnTick;
            Aborted += OnAborted;
        }

        public Ped PlayerPed => Game.Player.Character;
        public Vector3 PlayerPosition => PlayerPed.Position;
        public bool IsInMission => _currentMission != null;
        public static ModController Instance { get; private set; }

        private void OnAborted(object sender, EventArgs eventArgs)
        {
            _currentScene?.Abort();
            _currentMission?.Abort();
            if (_currentScene != null) PlayerPed.Position = Constants.TrevorAirport;
            Function.Call(Hash.SET_GRAVITY_LEVEL, 0);
            PlayerPed.HasGravity = true;
            PlayerPed.LastVehicle?.Delete();
        }

        private void OnKeyUp(object sender, KeyEventArgs keyEventArgs)
        {
            if (keyEventArgs.KeyCode != Keys.K) return;
            _spaceCraft = World.CreateProp("ufo_zancudo", PlayerPosition + PlayerPed.ForwardVector * 100, true, false);
            _spaceCraft.FreezePosition = true;
            _spaceCraft.Health = _spaceCraft.MaxHealth = 5000;
        }

        private void OnTick(object sender, EventArgs eventArgs)
        {
            EnterOrbit();
            UpdateScene();

            if (_spaceCraft == null) return;
            UI.ShowSubtitle($"Health:{_spaceCraft.Health}\nDead: {_spaceCraft.IsDead}");
            if (_spaceCraft.IsDead)
            {
                var pos = _spaceCraft.Position;
                Function.Call(Hash.REQUEST_NAMED_PTFX_ASSET, "core");
                Function.Call(Hash._SET_PTFX_ASSET_NEXT_CALL, "core");
                Function.Call(Hash.START_PARTICLE_FX_NON_LOOPED_AT_COORD, "exp_grd_vehicle_lod", pos.X, pos.Y, pos.Z, 0, 0, 0, 20.0f, 0, 0, 0, 0);
                World.AddExplosion(pos, ExplosionType.Car, 25, 0.5f, true, true);
                _spaceCraft.FreezePosition = false;
                _spaceCraft = null;
            }
        }

        public void SetCurrentMission(Mission mission)
        {
            _currentMission = mission;
            _currentMission.MissionEnded += OnMissionEnded;
        }

        private void OnMissionEnded()
        {
            _currentMission?.CleanUp();
        }

        private void OnSceneEnded(Scene sender, Scene newScene)
        {
            Game.FadeScreenOut(2000);
            Wait(2000);
            sender?.CleanUp();
            if (newScene == null) EnterAtmosphere();
            _currentScene = newScene;
            if (_currentScene != null)
            {
                _currentScene.SceneEnded += OnSceneEnded;
                _currentScene.Init();
            }
            Wait(2000);
            Game.FadeScreenIn(2000);
        }

        private void EnterOrbit()
        {
            if (_currentScene != null) return;
            if (!PlayerPed.IsInVehicle()) return;
            if (PlayerPed.CurrentVehicle.ClassType != VehicleClass.Planes) return;
            if (PlayerPed.HeightAboveGround < 2000) return;
            Game.FadeScreenOut(2000);
            Wait(2000);

            _currentScene = new EarthOrbitScene();
            _currentScene.Init();
            _currentScene.SceneEnded += OnSceneEnded;

            var currentVehicle = PlayerPed.CurrentVehicle;
            currentVehicle.HasGravity = false;
            Function.Call(Hash.SET_VEHICLE_GRAVITY, currentVehicle.Handle, false);
            RemoveGravity();

            Wait(2000);
            Game.FadeScreenIn(2000);
        }

        private void EnterAtmosphere()
        {
            if (!PlayerPed.IsInVehicle())
            {
                PlayerPed.Position = Constants.EarthAtmosphereEnterPosition;
                PlayerPed.Heading = 244.7877f;
            }
            else
            {
                var currentVehicle = PlayerPed.CurrentVehicle;
                currentVehicle.Position = Constants.EarthAtmosphereEnterPosition;
                currentVehicle.Heading = 244.7877f;
                currentVehicle.Speed = 75;
                currentVehicle.HasGravity = true;
                Function.Call(Hash.SET_VEHICLE_GRAVITY, currentVehicle.Handle, true);
            }

            PlayerPed.HasGravity = true;
            Function.Call(Hash.SET_GRAVITY_LEVEL, 0);
        }

        private void UpdateScene()
        {
            if (_currentScene == null)
                return;

            _currentScene.Update();
            _currentMission?.Tick(PlayerPed, _currentScene);

            FlyVehicleInSpace();
            ClearClouds();
            SetTimeAndWeather();
        }

        private void FlyVehicleInSpace()
        {
            if (!PlayerPed.IsInVehicle()) return;

            var currentVehicle = PlayerPed.CurrentVehicle;
            currentVehicle.Rotation = new Vector3(0, 0, currentVehicle.Rotation.Z);
            currentVehicle.Velocity = new Vector3(currentVehicle.Velocity.X, currentVehicle.Velocity.Y, 0);

            // FWD
            if (Game.IsControlPressed(2, Control.VehicleAccelerate))
                currentVehicle.ApplyForceRelative(Vector3.RelativeFront * 0.05f);

            // BWD
            if (Game.IsControlPressed(2, Control.VehicleBrake))
                currentVehicle.ApplyForceRelative(-Vector3.RelativeFront * 0.05f);

            // STOP
            if (Game.IsControlPressed(2, Control.Jump))
                currentVehicle.Velocity = Vector3.Lerp(currentVehicle.Velocity, Vector3.Zero, Game.LastFrameTime * 5);

            // LFT
            if (Game.IsControlPressed(2, Control.VehicleFlyYawLeft))
            {
                var rotation = currentVehicle.Rotation;
                rotation.Z += Game.LastFrameTime * 150;
                currentVehicle.Rotation = Vector3.Lerp(currentVehicle.Rotation, rotation, Game.LastFrameTime * 10);
            }

            // RHT
            // ReSharper disable once InvertIf
            if (Game.IsControlPressed(2, Control.VehicleFlyYawRight))
            {
                var rotation = currentVehicle.Rotation;
                rotation.Z -= Game.LastFrameTime * 150;
                currentVehicle.Rotation = Vector3.Lerp(currentVehicle.Rotation, rotation, Game.LastFrameTime * 10);
            }
        }

        private static void ClearClouds()
        {
            Function.Call(Hash._CLEAR_CLOUD_HAT);
        }

        private static void RemoveGravity()
        {
            Function.Call(Hash.SET_GRAVITY_LEVEL, 3);
        }

        private static void SetTimeAndWeather()
        {
            World.CurrentDayTime = new TimeSpan(0, 0, 0, 0);
            World.Weather = Weather.ExtraSunny;
        }
    }
}
