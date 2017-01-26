using System;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;
using SpaceMod.DataClasses;
using SpaceMod.DataClasses.MissionTypes;
using SpaceMod.DataClasses.SceneTypes;
using Control = GTA.Control;

namespace SpaceMod
{
    public class ModController : Script
    {
        private Scene _currentScene;
        private Mission _currentMission;
        private bool _askedToLeave;

        // Responsible for asking the player whether he/she wants to leave orbit
        // stay on earth, or go to the issl.
        private readonly UIMenu _leavePrompt = new UIMenu("Travel", "SELECT AN OPTION"); 

        public ModController()
        {
            Instance = this;
            KeyUp += OnKeyUp;
            Tick += OnTick;
            Aborted += OnAborted;

            SetupLeavePrompt();
        }

        public Ped PlayerPed => Game.Player.Character;
        public Vector3 PlayerPosition => PlayerPed.Position;
        public bool IsInMission => _currentMission != null;
        public static ModController Instance { get; private set; }

        private void SetupLeavePrompt()
        {
            var leaveItem = new UIMenuItem("Leave Earth", "Leave the Earth's orbit.");
            leaveItem.Activated += (sender, item) => LeaveEarth(new EarthOrbitScene());
            _leavePrompt.AddItem(leaveItem);

            var isslItem = new UIMenuItem("Go To ISSL", "Go to the International Space Station of Los Santos.");
            isslItem.Activated += (sender, item) => LeaveEarth(new IsslScene());
            _leavePrompt.AddItem(isslItem);

            var stayItem = new UIMenuItem("Stay", "Stay on Earth.");
            stayItem.Activated += (sender, item) =>
            {
                _askedToLeave = true;
                _leavePrompt.Visible = false;
                Game.TimeScale = 1.0f;
            };
            _leavePrompt.AddItem(stayItem);
        }

        private void OnAborted(object sender, EventArgs eventArgs)
        {
            _currentScene?.Abort();
            _currentMission?.Abort();
            if (_currentScene != null) PlayerPed.Position = Constants.TrevorAirport;
            PlayerPed.HasGravity = true;
            PlayerPed.LastVehicle?.Delete();
            Function.Call(Hash.SET_GRAVITY_LEVEL, 0); // TODO: Move to utilities.
            Game.TimeScale = 1.0f;
        }

        private void OnKeyUp(object sender, KeyEventArgs keyEventArgs)
        {
            if (keyEventArgs.KeyCode != Keys.K) return;
            LeaveEarth(new MoonSurfaceScene());
            SetCurrentMission(new TakeBackWhatsOurs());
        }

        private void OnTick(object sender, EventArgs eventArgs)
        {
            EnterOrbit();
            UpdateScene();
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
            if (newScene == null) BackToEarth();
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
            if (PlayerPed.HeightAboveGround > 2000)
            {
                if (_askedToLeave) return;
                Game.TimeScale = 0f;
                if (!_leavePrompt.Visible)
                    _leavePrompt.Visible = true;
                _leavePrompt.ProcessControl();
                _leavePrompt.ProcessMouse();
                _leavePrompt.Draw();
            }
            else _askedToLeave = false;
        }

        private void LeaveEarth(Scene scene)
        {
            _leavePrompt.Visible = false;
            Game.TimeScale = 1.0f;

            Game.FadeScreenOut(2000);
            Wait(2000);

            _currentScene = scene;
            _currentScene.Init();
            _currentScene.SceneEnded += OnSceneEnded;

            var currentVehicle = PlayerPed.CurrentVehicle;
            if (currentVehicle != null)
            {
                currentVehicle.HasGravity = false;
                Function.Call(Hash.SET_VEHICLE_GRAVITY, currentVehicle.Handle, false); // TODO: Move to utils.
            }
            RemoveGravity();

            Wait(2000);
            Game.FadeScreenIn(2000);
        }

        private void BackToEarth()
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
            /*
                In the moon mission when you shoot you kinda get wantedstars
                so this thing prevents that :)
            */
            Game.Player.WantedLevel = 0; //My code, yay

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
