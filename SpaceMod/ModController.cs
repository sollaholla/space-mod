using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;
using SpaceMod.DataClasses;
using SpaceMod.Static;

namespace SpaceMod
{
    public class ModController : Script
    {
        private readonly float _enterOrbitHeight = 5000;
        private readonly Keys _optionsMenuKey = Keys.NumPad9;


        private readonly MenuPool _menuPool;
        private readonly UIMenu _optionsMenu;

        private bool _useScenario = true;

        private readonly object _tickLock;
        
        private CustomScene _currentScene;

        public ModController()
        {
            _menuPool = new MenuPool();
            _menuPool = new MenuPool();
            _optionsMenu = new UIMenu("Grand Theft Space", "OPTIONS");
            _tickLock = new object();

            PlayerPrefs = new PlayerPrefs();
            Scenarios = new List<CustomScenario>();

            Instance = this;
            KeyUp += OnKeyUp;
            Tick += OnTick;
            Aborted += OnAborted;

            _enterOrbitHeight = Settings.GetValue("mod", "enter_orbit_height", _enterOrbitHeight);
            _optionsMenuKey = Settings.GetValue("mod", "options_menu_key", _optionsMenuKey);
            StaticSettings.MouseControlFlySensitivity = Settings.GetValue("vehicle_settings",
                "mouse_control_fly_sensitivity", StaticSettings.MouseControlFlySensitivity);
            StaticSettings.VehicleSurfaceSpawn = Settings.GetValue("vehicle_settings", "vehicle_surface_spawn",
                StaticSettings.VehicleSurfaceSpawn);
            Settings.SetValue("mod", "enter_orbit_height", _enterOrbitHeight);
            Settings.SetValue("mod", "options_menu_key", _optionsMenuKey);
            Settings.SetValue("vehicle_settings",
                "mouse_control_fly_sensitivity", StaticSettings.MouseControlFlySensitivity);
            Settings.SetValue("vehicle_settings", "vehicle_surface_spawn", StaticSettings.VehicleSurfaceSpawn);
            Settings.Save();

            var showUIItem = new UIMenuCheckboxItem("Show Custom UI", true);
            var useScenarioItem = new UIMenuCheckboxItem("Use Scenarios", true);
            var debugItem = new UIMenuItem("Log Player Data", "Log the player ped data to file.");
            var subMenu = _menuPool.AddSubMenu(_optionsMenu, "Scenes");
            var files = Directory.GetFiles(Database.PathToScenes);
            for (var i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var customXmlScene = MyXmlSerializer.Deserialize<CustomXmlScene>(file);
                if (customXmlScene == default(CustomXmlScene))
                {
                    UI.Notify($"Failed to load {file}");
                    continue;
                }
                var fileName = Path.GetFileNameWithoutExtension(file);
                var menuItem = new UIMenuItem(fileName, $"Load {fileName} as a scene.");
                menuItem.Activated += (sender, item) => SetCurrentScene(customXmlScene);
                subMenu.AddItem(menuItem);
            }

            subMenu.RefreshIndex();

            showUIItem.CheckboxEvent += (sender, @checked) =>
            {
                OrbitalSystem.ShowUIPositions = @checked;
            };

            useScenarioItem.CheckboxEvent += (sender, @checked) =>
            {
                _useScenario = @checked;
            };

            debugItem.Activated += (sender, item) =>
            {
                DebugLogger.LogEntityData(PlayerPed);
            };

            _menuPool.Add(_optionsMenu);
            _optionsMenu.AddItem(showUIItem);
            _optionsMenu.AddItem(useScenarioItem);
            _optionsMenu.AddItem(debugItem);
            _optionsMenu.RefreshIndex();
        }

        public Ped PlayerPed => Game.Player.Character;

        public Vector3 PlayerPosition {
            get { return PlayerPed.IsInVehicle() ? PlayerPed.CurrentVehicle.Position : PlayerPed.Position; }
            set
            {
                if (PlayerPed.IsInVehicle())
                {
                    PlayerPed.CurrentVehicle.Position = value;
                }
                else
                {
                    PlayerPed.Position = value;
                }
            }
        }

        public PlayerPrefs PlayerPrefs { get; }

        public List<CustomScenario> Scenarios { get; private set; }

        internal static ModController Instance { get; private set; }

        internal CustomScene GetCurrentScene()
        {
            return _currentScene;
        }
        
        private void OnAborted(object sender, EventArgs eventArgs)
        {
            Utilities.SetGravityLevel(0);

            if (!PlayerPed.IsDead)
                Game.FadeScreenIn(0);

            PlayerPed.HasGravity = true;

            Game.TimeScale = 1.0f;
            World.RenderingCamera = null;
            _currentScene?.Delete();

            if (Scenarios != null)
            {
                while (Scenarios.Count > 0)
                {
                    var scen = Scenarios[0];
                    scen.OnAborted();
                    Scenarios.RemoveAt(0);
                }
            }

            if (_currentScene != null && _currentScene != default(CustomScene))
                PlayerPosition = Database.TrevorAirport;

            _currentScene = null;
        }

        private void OnKeyUp(object sender, KeyEventArgs keyEventArgs)
        {
            if (_menuPool.IsAnyMenuOpen()) return;
            if (keyEventArgs.KeyCode == _optionsMenuKey)
                _optionsMenu.Visible = true;
        }

        private void OnTick(object sender, EventArgs eventArgs)
        {
            if (!Monitor.TryEnter(_tickLock)) return;

            try
            {
                _menuPool.ProcessMenus();

                if (PlayerPed.IsDead && _currentScene != null)
                {
                    CurrentSceneOnExited(null, "cmd_earth", Vector3.Zero);
                    return;
                }
                
                DisableWantedStars();

                if (_currentScene != null)
                {
                    Scenarios?.ForEach(scenario => scenario.Update());

                    Function.Call(Hash._CLEAR_CLOUD_HAT);

                    TimeType timeType = _currentScene.SceneData.CurrentIplData?.Time ?? _currentScene.SceneData.Time;

                    switch (timeType)
                    {
                        case TimeType.Night:
                            World.CurrentDayTime = new TimeSpan(World.CurrentDayTime.Days, 22, 0, 0);
                            break;
                        case TimeType.Day:
                            World.CurrentDayTime = new TimeSpan(World.CurrentDayTime.Days, 9, 0, 0);
                            break;
                        case TimeType.Evening:
                            World.CurrentDayTime = new TimeSpan(World.CurrentDayTime.Days, 16, 0, 0);
                            break;
                    }

                    World.Weather = Weather.ExtraSunny;

                    if (PlayerPed.CurrentVehicle != null)
                    {
                        PlayerPed.CurrentVehicle.HasGravity = _currentScene.SceneData.UseGravity;
                    }
                    else
                    {
                        PlayerPed.HasGravity = _currentScene.SceneData.UseGravity;
                    }

                    _currentScene.Update();
                }
                else
                {
                    float height = PlayerPed.HeightAboveGround;

                    if (height > _enterOrbitHeight)
                    {
                        CustomXmlScene scene =
                            MyXmlSerializer.Deserialize<CustomXmlScene>(Database.PathToScenes + "/" + "EarthOrbit.xml");

                        SetCurrentScene(scene);
                    }
                }
            }
            finally
            {
                Monitor.Exit(_tickLock);
            }
        }

        private static void DisableWantedStars()
        {
            Utilities.TerminateScriptByName("re_prison");
            Utilities.TerminateScriptByName("re_prisonlift");
            Utilities.TerminateScriptByName("am_prison");
            Utilities.TerminateScriptByName("re_lossantosintl");
            Utilities.TerminateScriptByName("re_armybase");
            Game.MaxWantedLevel = 0;
        }

        private void SetCurrentScene(CustomXmlScene customXmlScene)
        {
            Game.FadeScreenOut(1000);
            Wait(1000);
            CreateSceneFromCustomXmlScene(customXmlScene);
            Wait(1000);
            Game.FadeScreenIn(1000);
        }

        private void CreateSceneFromCustomXmlScene(CustomXmlScene customXmlScene)
        {
            lock (_tickLock)
            {
                EndActiveScenarios();

                if (_currentScene != null && _currentScene != default(CustomScene))
                {
                    _currentScene.Delete();
                }

                _currentScene = new CustomScene(customXmlScene);
                _currentScene.Start();
                _currentScene.Exited += CurrentSceneOnExited;

                if (PlayerPed.IsInVehicle())
                {
                    PlayerPed.CurrentVehicle.Rotation = Vector3.Zero;
                }
                else
                {
                    PlayerPed.Rotation = Vector3.Zero;
                }

                if (_useScenario)
                {
                    Scenarios = customXmlScene.CustomScenarios?.Select(x =>
                    {
                        var assembly = Assembly.LoadFrom(Database.PathToScenarios + "/" + x.Name);
                        if (assembly == null)
                        {
                            DebugLogger.Log("Failed to load assembly from: " + x.Name, MessageType.Error);
                            return null;
                        }
                        var name = x.PathToClass;
                        var type = assembly.GetType(name);
                        var scenario = (CustomScenario) Activator.CreateInstance(type);
                        return scenario.IsScenarioComplete() ? null : scenario;

                    }).Where(x => x != null).ToList();

                    Scenarios?.ForEach(scenario =>
                    {
                        scenario.Start();
                        scenario.Completed += ScenarioOnCompleted;
                    });
                }

                Utilities.SetGravityLevel(_currentScene.SceneData.GravityLevel);
            }
        }

        private void ScenarioOnCompleted(CustomScenario scenario, bool success)
        {
            lock (_tickLock)
            {
                Scenarios.Remove(scenario);
            }
        }

        private void CurrentSceneOnExited(CustomScene scene, string newSceneFile, Vector3 exitRotation)
        {
            lock (_tickLock)
            {
                Game.FadeScreenOut(1000);
                Wait(1000);

                _currentScene?.Delete();
                _currentScene = null;

                EndActiveScenarios();

                if (newSceneFile != "cmd_earth")
                {
                    var newScene =
                        MyXmlSerializer.Deserialize<CustomXmlScene>(Database.PathToScenes + "/" + newSceneFile);

                    if (newScene == default(CustomXmlScene))
                    {
                        UI.Notify(
                            "Your custom scene ~r~failed~s~ to load, because the file specified was either not written correctly or " +
                            "was invalid.");
                        throw new XmlException(newSceneFile);
                    }

                    CreateSceneFromCustomXmlScene(newScene);

                    if (PlayerPed.IsInVehicle())
                    {
                        PlayerPed.CurrentVehicle.Rotation = exitRotation;
                    }
                    else
                    {
                        PlayerPed.Rotation = exitRotation;
                    }
                }
                else
                {
                    if (PlayerPed.IsInVehicle())
                    {
                        var playerPedCurrentVehicle = PlayerPed.CurrentVehicle;
                        playerPedCurrentVehicle.Position = Database.EarthAtmosphereEnterPosition;
                        playerPedCurrentVehicle.Rotation = Vector3.Zero;
                        playerPedCurrentVehicle.Heading = 243;
                        playerPedCurrentVehicle.HasGravity = true;
                    }
                    else
                    {
                        PlayerPosition = Database.TrevorAirport;
                    }

                    PlayerPed.HasGravity = true;
                    Utilities.SetGravityLevel(0);
                }

                Wait(1000);
                Game.FadeScreenIn(1000);
            }
        }

        private void EndActiveScenarios()
        {
            if (Scenarios != null)
            {
                while (Scenarios.Count > 0)
                {
                    var scen = Scenarios[0];
                    scen.OnEnded(false);
                    Scenarios.RemoveAt(0);
                }
            }
        }
    }
}
