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
        private readonly Weather _defaultWeather = Weather.Clear;
        private readonly TimeSpan _defaultTime = new TimeSpan(0, 0, 0, 0);

        private Weather _currentWeather = Weather.Clear;
        private TimeSpan _currentTime = new TimeSpan(0, 0, 0, 0);
        
        // Responsible for asking the player whether he/she wants to leave orbit
        // stay on earth, or go to the issl.
        private readonly UIMenu _leavePrompt = new UIMenu("Travel", "SELECT AN OPTION"); 

        private readonly MenuPool _menuPool = new MenuPool();
        private readonly UIMenu _optionsMenu = new UIMenu("Grand Theft Space", "OPTIONS");

        public ModController()
        {
            Instance = this;
            KeyUp += OnKeyUp;
            Tick += OnTick;
            Aborted += OnAborted;

            _menuPool.Add(_optionsMenu);
            var showUIItem = new UIMenuCheckboxItem("Show Distance UI", true, "Show the distance to planets when in space.");
            showUIItem.CheckboxEvent += (sender, @checked) =>
            {
                OrbitalSystem.ShowUIPositions = @checked;
            };
            _optionsMenu.AddItem(showUIItem);

            SetupLeavePrompt();
        }

        public Ped PlayerPed => Game.Player.Character;
        public Vector3 PlayerPosition => PlayerPed.Position;
        public bool IsInMission => _currentMission != null;
        public static ModController Instance { get; private set; }

        private void SetupLeavePrompt()
        {
            var leaveItem = new UIMenuItem("Leave Earth", "Leave the Earth's orbit.");
            leaveItem.Activated += (sender, item) => LeaveEarth(new EarthOrbitScene(), SceneStartDirection.FromTarget);
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
            if (!PlayerPed.IsDead) Game.FadeScreenIn(0);
            _currentScene?.Abort();
            _currentMission?.Abort();

            if (_currentScene != null)
            {
                PlayerPed.Position = Constants.TrevorAirport;
                PlayerPed.LastVehicle?.Delete();
            }

            // TODO: Move to utilities.
            Function.Call(Hash.SET_GRAVITY_LEVEL, 0);
            PlayerPed.HasGravity = true;
            Game.TimeScale = 1.0f;
        }

        private void OnKeyUp(object sender, KeyEventArgs keyEventArgs)
        {
            if (keyEventArgs.KeyCode == Keys.H)
                _optionsMenu.Visible = true;

            if (keyEventArgs.KeyCode == Keys.O)
            {
                LeaveEarth(new MarsSurfaceScene());
                //SetCurrentMission(new TakeBackWhatsOurs());
            }
        }

        private void OnTick(object sender, EventArgs eventArgs)
        {
            PlayerPed.SetSuperJumpThisFrame(3, 3);

            _menuPool.ProcessMenus();

            if (PlayerPed.IsDead && Game.IsScreenFadedOut)
                OnAborted(null, null);

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
            _currentMission = null;

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
            if (PlayerPed.HeightAboveGround >= 5000)
            {
                if (_askedToLeave) return;
                Game.TimeScale = 0f;
                if (!_leavePrompt.Visible)
                    _leavePrompt.Visible = true;
                _menuPool.CloseAllMenus();
                _leavePrompt.ProcessControl();
                _leavePrompt.ProcessMouse();
                _leavePrompt.Draw();
            }
            else _askedToLeave = false;
        }

        private void LeaveEarth(Scene scene, SceneStartDirection dir = SceneStartDirection.None)
        {
            _leavePrompt.Visible = false;
            Game.TimeScale = 1.0f;

            Game.FadeScreenOut(2000);
            Wait(2000);

            _currentScene = scene;
            _currentScene.StartDirection = dir;
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
                currentVehicle.ApplyForceRelative(Vector3.RelativeFront * 0.1f);

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

        public void SetWeatherAndTime(Weather weather, TimeSpan time)
        {
            _currentWeather = weather;
            _currentTime = time;
        }

        public void ResetWeatherAndTime()
        {
            _currentWeather = _defaultWeather;
            _currentTime = _defaultTime;
        }

        private void SetTimeAndWeather()
        {
            World.CurrentDayTime = _currentTime;
            World.Weather = _currentWeather;
        }

        public void CloseAllMenus()
        {
            _menuPool?.CloseAllMenus();
        }
    }
}
