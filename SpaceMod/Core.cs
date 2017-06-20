using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using GTA;
using GTA.Math;
using GTA.Native;
using SolomanMenu;
using SpaceMod.Extensions;
using SpaceMod.Lib;
using SpaceMod.Scenario;
using SpaceMod.Scenes;
using Menu = SolomanMenu.Menu;

namespace SpaceMod
{
    internal class Core : Script
    {
        private MenuConnector _menuConnector;
        private Menu _menu;

        private bool _missionsComplete;
        private bool preloadModels = true;
        private bool endMissionComplete;
        private bool disableWantedStars = true;
        private bool resetWantedLevel;
        private bool overrideTimecycleModifier;
        private bool resetTimecycle;
        private bool menuEnabled = true;
        private string spaceTimecycle;
        private float _enterOrbitHeight = 5000;
        private Keys _optionsMenuKey = Keys.NumPad9;
        private CustomScene _currentScene;
        private Ped colonel;

        private readonly object _tickLock;

        public Core()
        {
            _tickLock = new object();

            Scenarios = new List<CustomScenario>();

            Instance = this;
            KeyUp += OnKeyUp;
            Tick += OnTick;
            Aborted += OnAborted;

            ReadSettings();
            SaveSettings();
            CreateCustomMenu();

            RequestModels();
        }

        internal static Core Instance { get; private set; }
        public Ped PlayerPed => Game.Player.Character;
        public Vector3 PlayerPosition {
            get { return PlayerPed.IsInVehicle() ? PlayerPed.CurrentVehicle.Position : PlayerPed.Position; }
            set {
                if (PlayerPed.IsInVehicle()) PlayerPed.CurrentVehicle.Position = value;
                else PlayerPed.Position = value;
            }
        }
        public List<CustomScenario> Scenarios { get; private set; }

        internal CustomScene GetCurrentScene()
        {
            return _currentScene;
        }

        private void OnAborted(object sender, EventArgs eventArgs)
        {
            SpaceModLib.SetGravityLevel(0);

            if (!PlayerPed.IsDead)
                Game.FadeScreenIn(0);
            PlayerPed.HasGravity = true;
            PlayerPed.FreezePosition = false;
            PlayerPed.Task.ClearAll();
            PlayerPed.FreezePosition = false;
            GTSLib.CutCredits();

            Game.TimeScale = 1.0f;
            World.RenderingCamera = null;
            _currentScene?.Delete();
            colonel?.Delete();

            if (Scenarios != null)
            {
                while (Scenarios.Count > 0)
                {
                    var scenario = Scenarios[0];
                    scenario.OnAborted();
                    Scenarios.RemoveAt(0);
                }
            }

            if (_currentScene != default(CustomScene))
            {
                GiveRespawnControlToGame();
                if (!PlayerPed.IsDead)
                    PlayerPosition = SpaceModDatabase.TrevorAirport;
                ResetWeather();
            }
            _currentScene = null;

            Function.Call(Hash.CLEAR_TIMECYCLE_MODIFIER);
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (_menuConnector?.AreMenusVisible() ?? false) return;
            if (e.KeyCode != _optionsMenuKey) return;
            _menu.Draw = true;
        }

        private void OnTick(object sender, EventArgs eventArgs)
        {
            if (!Monitor.TryEnter(_tickLock)) return;
            try
            {
                _menuConnector?.UpdateMenus();

                DoEndMission();
                DisableWantedStars();

                if (_currentScene != null) DoSceneUpdate();
                else DoEarthUpdate();
            }
            finally
            {
                Monitor.Exit(_tickLock);
            }
        }

        private void RequestModels()
        {
            if (!preloadModels)
                return;

            const string fileName = "./scripts/SpaceMod/RequestOnStart.txt";
            if (!File.Exists(fileName))
                return;

            using (StreamReader sr = File.OpenText(fileName))
            {
                string s = String.Empty;

                while ((s = sr.ReadLine()) != null)
                {
                    s = s.Trim();
                    Model m = new Model(s);
                    if (!m.IsLoaded)
                        m.Request();
                }
            }
        }

        private void DoEndMission()
        {
            if (!_missionsComplete || endMissionComplete || _currentScene != null)
            {
                colonel?.Delete();
                colonel = null;
                return;
            }

            if (!Entity.Exists(colonel))
            {
                colonel = World.CreatePed(PedHash.Marine03SMY, new Vector3(-2356.895f, 3248.412f, 101.4508f), 313.5386f);
                return;
            }

            if (!colonel.CurrentBlip.Exists())
            {
                new Blip(colonel.AddBlip().Handle) {
                    Sprite = BlipSprite.GTAOMission,
                    Color = BlipColor.White,
                    Scale = 1.5f,
                    Name = "Colonel Larson"
                };
            }

            float distance = Vector3.Distance(PlayerPosition, colonel.Position);
            if (distance > 1.75f)
                return;

            World.DrawMarker(MarkerType.UpsideDownCone, colonel.Position + Vector3.WorldUp * 1.5f, Vector3.RelativeRight, Vector3.Zero, new Vector3(0.35f, 0.35f, 0.35f), Color.Red);
            SpaceModLib.DisplayHelpTextWithGXT("END_LABEL_1");

            if (Game.IsControlJustPressed(2, GTA.Control.Context))
            {
                Function.Call(Hash._PLAY_AMBIENT_SPEECH1, colonel.Handle, "Generic_Hi", "Speech_Params_Force");
                PlayerPed.FreezePosition = true;
                PlayerPed.Heading = (colonel.Position - PlayerPosition).ToHeading();
                PlayerPed.Task.StandStill(-1);

                while (colonel.Exists())
                {
                    SpaceModLib.ShowSubtitleWithGXT("END_LABEL_2", 10000);
                    Wait(10000);
                    Function.Call(Hash._PLAY_AMBIENT_SPEECH1, PlayerPed.Handle, "Generic_Thanks", "Speech_Params_Force_Shouted_Critical");
                    SpaceModLib.ShowSubtitleWithGXT("END_LABEL_3");
                    Wait(5000);
                    Game.FadeScreenOut(1000);
                    Wait(1000);
                    colonel.Delete();
                    PlayerPed.Task.ClearAllImmediately();
                    PlayerPed.FreezePosition = false;
                    Wait(1000);
                    Game.FadeScreenIn(1000);
                    endMissionComplete = true;
                    Settings.SetValue("settings", "end_mission_complete", endMissionComplete);
                    Settings.Save();
                    GTSLib.RollCredits();
                    Wait(30000);
                    GTSLib.CutCredits();
                    Yield();
                }
            }
        }

        private void DoEarthUpdate()
        {
            float height = PlayerPed.HeightAboveGround;

            if (height > _enterOrbitHeight)
            {
                CustomXmlScene scene =
                    MyXmlSerializer.Deserialize<CustomXmlScene>(SpaceModDatabase.PathToScenes + "/" + "EarthOrbit.space");
                SetCurrentScene(scene, "EarthOrbit.space");
            }
        }

        private void DoSceneUpdate()
        {
            if (overrideTimecycleModifier)
            {
                Function.Call(Hash.SET_TIMECYCLE_MODIFIER, spaceTimecycle);
                resetTimecycle = false;
            }
            else if (!resetTimecycle)
            {
                Function.Call(Hash.CLEAR_TIMECYCLE_MODIFIER);
                resetTimecycle = true;
            }

            Scenarios?.ForEach(scenario => scenario.Update());

            SetTime();
            SetWeather();

            if (PlayerPed.CurrentVehicle != null)
                PlayerPed.CurrentVehicle.HasGravity = _currentScene.SceneData.UseGravity;
            else PlayerPed.HasGravity = _currentScene.SceneData.UseGravity;

            _currentScene.Update();
        }

        private void SetWeather()
        {
            Function.Call(Hash._CLEAR_CLOUD_HAT);
            if (_currentScene.OverrideWeather == (Weather)14) Function.Call(Hash.SET_WEATHER_TYPE_NOW, "HALLOWEEN");
            else World.Weather = _currentScene.OverrideWeather;
        }

        private void SetTime()
        {
            TimeType timeType = _currentScene.SceneData.CurrentIplData?.Time ?? _currentScene.SceneData.Time;
            switch (timeType)
            {
                case TimeType.Night:
                    World.CurrentDayTime = new TimeSpan(World.CurrentDayTime.Days, 1, 30, 0);
                    break;
                case TimeType.Day:
                    World.CurrentDayTime = new TimeSpan(World.CurrentDayTime.Days, 9, 0, 0);
                    break;
                case TimeType.Evening:
                    World.CurrentDayTime = new TimeSpan(World.CurrentDayTime.Days, 18, 0, 0);
                    break;
            }
        }

        private void ReadSettings()
        {
            _enterOrbitHeight = Settings.GetValue("mod", "enter_orbit_height", _enterOrbitHeight);
            _optionsMenuKey = Settings.GetValue("mod", "options_menu_key", _optionsMenuKey);
            menuEnabled = Settings.GetValue("mod", "menu_enabled", menuEnabled);
            preloadModels = Settings.GetValue("mod", "pre_load_models", preloadModels);
            endMissionComplete = Settings.GetValue("settings", "end_mission_complete", endMissionComplete);
            StaticSettings.ShowCustomUi = Settings.GetValue("settings", "show_custom_ui", StaticSettings.ShowCustomUi);
            StaticSettings.UseScenarios = Settings.GetValue("settings", "use_scenarios", StaticSettings.UseScenarios);
            overrideTimecycleModifier = Settings.GetValue("settings", "override_timecycle", overrideTimecycleModifier);
            spaceTimecycle = Settings.GetValue("settings", "space_timecycle", spaceTimecycle);
            StaticSettings.MoonJump = Settings.GetValue("settings", "low_gravity_jumping", StaticSettings.MoonJump);
            StaticSettings.MouseControlFlySensitivity = Settings.GetValue("vehicle_settings", "mouse_control_fly_sensitivity", StaticSettings.MouseControlFlySensitivity);
            StaticSettings.DefaultVehicleSpawn = Settings.GetValue("vehicle_settings", "vehicle_surface_spawn", StaticSettings.DefaultVehicleSpawn);
            StaticSettings.VehicleFlySpeed = Settings.GetValue("vehicle_settings", "vehicle_fly_speed", StaticSettings.VehicleFlySpeed);
            CheckMissionsStatus();
        }

        private void SaveSettings()
        {
            Settings.SetValue("mod", "enter_orbit_height", _enterOrbitHeight);
            Settings.SetValue("mod", "options_menu_key", _optionsMenuKey);
            Settings.SetValue("mod", "menu_enabled", menuEnabled);
            Settings.SetValue("mod", "pre_load_models", preloadModels);
            Settings.SetValue("settings", "show_custom_ui", StaticSettings.ShowCustomUi);
            Settings.SetValue("settings", "use_scenarios", StaticSettings.UseScenarios);
            Settings.SetValue("settings", "override_timecycle", overrideTimecycleModifier);
            Settings.SetValue("settings", "space_timecycle", spaceTimecycle);
            Settings.SetValue("settings", "low_gravity_jumping", StaticSettings.MoonJump);
            Settings.SetValue("vehicle_settings", "mouse_control_fly_sensitivity", StaticSettings.MouseControlFlySensitivity);
            Settings.SetValue("vehicle_settings", "vehicle_surface_spawn", StaticSettings.DefaultVehicleSpawn);
            Settings.SetValue("vehicle_settings", "vehicle_fly_speed", StaticSettings.VehicleFlySpeed);
            Settings.Save();
        }

        private void CheckMissionsStatus()
        {
            string dm = "/DefaultMissions.";
            ScriptSettings mars1Settings = ScriptSettings.Load(SpaceModDatabase.PathToScenarios + dm + "MarsMission01.ini");
            ScriptSettings mars2Settings = ScriptSettings.Load(SpaceModDatabase.PathToScenarios + dm + "MarsMission02.ini");
            ScriptSettings moonSettings = ScriptSettings.Load(SpaceModDatabase.PathToScenarios + dm + "MoonMission01.ini");
            _missionsComplete =
                mars1Settings.GetValue("scenario_config", "complete", false) &&
                mars2Settings.GetValue("scenario_config", "complete", false) &&
                moonSettings.GetValue("scenario_config", "complete", false);
        }

        private void CreateCustomMenu()
        {
            if (!menuEnabled)
                return;

            _menuConnector = new MenuConnector();
            _menu = new Menu("Space Mod", Color.FromArgb(125, Color.Black), Color.Black,
                Color.Purple)
            { MenuItemHeight = 26 };

            #region scenes

            var scenesMenu = _menu.AddParentMenu("Scenes", _menu);
            scenesMenu.Width = _menu.Width;

            var files = Directory.GetFiles(SpaceModDatabase.PathToScenes).Where(file => file.EndsWith(".space")).ToArray();
            for (var i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var fileName = Path.GetFileName(file);
                var menuItem = new SolomanMenu.MenuItem(fileName);
                menuItem.ItemActivated += (sender, item) =>
                {
                    var customXmlScene = MyXmlSerializer.Deserialize<CustomXmlScene>(file);
                    if (customXmlScene == default(CustomXmlScene))
                    {
                        UI.Notify($"Failed to load {file}");
                        return;
                    }
                    _menuConnector.Menus?.ForEach(m => m.Draw = false);
                    SetCurrentScene(customXmlScene, fileName);
                };
                scenesMenu.Add(menuItem);
            }

            #endregion

            #region settings

            var settingsMenu = _menu.AddParentMenu("Settings", _menu);

            #region ui settings

            var userInterfaceMenu = settingsMenu.AddParentMenu("Interface", _menu);

            var showCustomUiCheckbox = new CheckboxMenuItem("Show Custom UI", StaticSettings.ShowCustomUi);
            showCustomUiCheckbox.Checked += (sender, check) => {
                StaticSettings.ShowCustomUi = check;
            };

            userInterfaceMenu.Add(showCustomUiCheckbox);

            #endregion

            #region vehicle settings

            var vehicleSettingsMenu = settingsMenu.AddParentMenu("Vehicles", _menu);
            vehicleSettingsMenu.Width = _menu.Width;

            List<dynamic> lst;
            int flyIndex;
            var vehicleSpeedList = new ListMenuItem("Vehicle Speed",
                lst = Enumerable.Range(1, 20).Select(i => (dynamic)(i * 5)).ToList(), (flyIndex = lst.IndexOf(StaticSettings.VehicleFlySpeed)) == -1 ? 0 : flyIndex);
            vehicleSpeedList.IndexChanged += (sender, index, item) => {
                StaticSettings.VehicleFlySpeed = item;
            };

            var flySensitivity = (int)StaticSettings.MouseControlFlySensitivity;
            var vehicleSensitivityList = new ListMenuItem("Mouse Control Sensitivity",
                Enumerable.Range(0, flySensitivity > 15 ? flySensitivity + 5 : 15).Select(i => (dynamic)i).ToList(),
                flySensitivity);
            vehicleSensitivityList.IndexChanged += (sender, index, item) =>
            {
                StaticSettings.MouseControlFlySensitivity = item;
            };

            vehicleSettingsMenu.Add(vehicleSpeedList);
            vehicleSettingsMenu.Add(vehicleSensitivityList);

            #endregion

            #region scene settings

            var sceneSettingsMenu = settingsMenu.AddParentMenu("Scenes", _menu);

            var useScenariosCheckbox = new CheckboxMenuItem("Use Scenarios", StaticSettings.UseScenarios);
            useScenariosCheckbox.Checked += (sender, check) =>
            {
                StaticSettings.UseScenarios = check;
            };

            sceneSettingsMenu.Add(useScenariosCheckbox);

            #endregion

            #region player settings

            var playerSettingsMenu = settingsMenu.AddParentMenu("Player", _menu);

            var useFloatingCheckbox = new CheckboxMenuItem("Use Floating", StaticSettings.UseFloating);
            useFloatingCheckbox.Checked += (sender, check) =>
            {
                StaticSettings.UseFloating = check;
            };

            playerSettingsMenu.Add(useFloatingCheckbox);

            #endregion

            var saveSettingsItem = new SolomanMenu.MenuItem("Save Settings");
            saveSettingsItem.ItemActivated += (sender, item) =>
            {
                SaveSettings();
                UI.Notify("Settings ~b~saved~s~.", true);
            };

            settingsMenu.Add(saveSettingsItem);

            var disableWantedLevelCheckbox = new CheckboxMenuItem("Disable Wanted Level", disableWantedStars);
            disableWantedLevelCheckbox.Checked += (a, b) => {
                disableWantedStars = b;
            };
            settingsMenu.Add(disableWantedLevelCheckbox);

            var overrideTimecycle = new CheckboxMenuItem("Override TimeCycleModifier", overrideTimecycleModifier);
            overrideTimecycle.Checked += (a, b) => {
                overrideTimecycleModifier = b;
            };
            settingsMenu.Add(overrideTimecycle);

            #endregion

            #region debug

            var debugButton = new SolomanMenu.MenuItem("Debug Player");
            debugButton.ItemActivated += (sender, item) =>
            {
                Debug.LogEntityData(PlayerPed);
            };

            _menu.Add(debugButton);

            #endregion

            _menuConnector.Menus.Add(_menu);
            _menuConnector.Menus.Add(settingsMenu);
            _menuConnector.Menus.Add(userInterfaceMenu);
            _menuConnector.Menus.Add(vehicleSettingsMenu);
            _menuConnector.Menus.Add(sceneSettingsMenu);
            _menuConnector.Menus.Add(playerSettingsMenu);
            _menuConnector.Menus.Add(scenesMenu);
        }
        
        private void DisableWantedStars()
        {
            if (!disableWantedStars)
            {
                if (!resetWantedLevel)
                {
                    SpaceModLib.RequestScript("re_prison");
                    SpaceModLib.RequestScript("re_prisonlift");
                    SpaceModLib.RequestScript("am_prison");
                    SpaceModLib.RequestScript("re_lossantosintl");
                    SpaceModLib.RequestScript("re_armybase");
                    SpaceModLib.RequestScript("restrictedareas");
                    Game.MaxWantedLevel = 5;
                    resetWantedLevel = true;
                }
                return;
            }
            resetWantedLevel = false;
            SpaceModLib.TerminateScriptByName("re_prison");
            SpaceModLib.TerminateScriptByName("re_prisonlift");
            SpaceModLib.TerminateScriptByName("am_prison");
            SpaceModLib.TerminateScriptByName("re_lossantosintl");
            SpaceModLib.TerminateScriptByName("re_armybase");
            SpaceModLib.TerminateScriptByName("restrictedareas");
            Game.MaxWantedLevel = 0;
        }

        private void ResetWeather()
        {
            World.Weather = Weather.ExtraSunny;
            World.CurrentDayTime = new TimeSpan(World.CurrentDayTime.Days, 12, 0, 0);
        }

        private void SetCurrentScene(CustomXmlScene customXmlScene, string fileName = default(string))
        {
            Game.FadeScreenOut(1000);
            Wait(1000);
            CreateScene(customXmlScene, fileName);
            Wait(1000);
            Game.FadeScreenIn(1000);
        }

        private void CreateScene(CustomXmlScene customXmlScene, string fileName = default(string))
        {
            lock (_tickLock)
            {
                EndActiveScenarios();

                if (_currentScene != null && _currentScene != default(CustomScene))
                    _currentScene.Delete();

                ClearAllEntities();

                if (PlayerPed.IsInVehicle()) PlayerPed.CurrentVehicle.Rotation = Vector3.Zero;
                else PlayerPed.Rotation = Vector3.Zero;

                _currentScene = new CustomScene(customXmlScene) { SceneFile = fileName };
                _currentScene.Start();
                _currentScene.Exited += CurrentSceneOnExited;

                if (StaticSettings.UseScenarios)
                {
                    Scenarios = customXmlScene.CustomScenarios?.Select(x =>
                    {
                        var assembly = Assembly.LoadFrom(SpaceModDatabase.PathToScenarios + "/" + x.Name);
                        if (assembly == null)
                        {
                            Debug.Log("Failed to load assembly from: " + x.Name, DebugMessageType.Error);
                            return null;
                        }
                        var name = x.PathToClass;
                        var type = assembly.GetType(name);
                        var scenario = (CustomScenario)Activator.CreateInstance(type);
                        return scenario.IsScenarioComplete() ? null : scenario;

                    }).Where(x => x != null).ToList();

                    Scenarios?.ForEach(scenario =>
                    {
                        scenario.Start();
                        scenario.Completed += ScenarioComplete;
                    });
                }

                SpaceModLib.SetGravityLevel(_currentScene.SceneData.GravityLevel);
            }
        }

        private CustomXmlScene ReadSceneFile(CustomScene scene, string newSceneFile)
        {
            if (newSceneFile == "cmd_earth")
                return null;
            var newScene = MyXmlSerializer.Deserialize<CustomXmlScene>(SpaceModDatabase.PathToScenes + "/" + newSceneFile);
            if (newScene == default(CustomXmlScene))
            {
                UI.Notify(
                    "Your custom scene ~r~failed~s~ to load, because the file specified was " +
                    "either was invalid.");
                throw new XmlException(newSceneFile);
            }
            newScene.LastSceneFile = scene.SceneFile;
            return newScene;
        }

        private void ScenarioComplete(CustomScenario scenario, bool success)
        {
            lock (_tickLock) {
                Scenarios.Remove(scenario);
                CheckMissionsStatus();
            }
        }

        private void ClearAllEntities(Vector3 pos = default(Vector3), float distance = int.MaxValue)
        {
            // Non persistent entities.
            Function.Call(Hash.SET_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0f);
            Function.Call(Hash.SET_RANDOM_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0f);
            Function.Call(Hash.SET_PARKED_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0f);
            Function.Call(Hash.SET_NUMBER_OF_PARKED_VEHICLES, 0f);
            Function.Call((Hash)0xF796359A959DF65D, false);
            Function.Call(Hash.SET_PED_DENSITY_MULTIPLIER_THIS_FRAME, 0f);
            Function.Call(Hash.SET_SCENARIO_PED_DENSITY_MULTIPLIER_THIS_FRAME, 0f, 0f);
            Function.Call((Hash)0x2F9A292AD0A3BD89);
            Function.Call((Hash)0x5F3B7749C112D552);
            Function.Call(Hash.DELETE_ALL_TRAINS);
            Function.Call(Hash.DESTROY_MOBILE_PHONE);
            Function.Call(Hash.SET_GARBAGE_TRUCKS, 0);
            Function.Call(Hash.SET_RANDOM_BOATS, 0);
            Function.Call(Hash.SET_RANDOM_TRAINS, 0);
            Function.Call(Hash.CLEAR_AREA_OF_PEDS, pos.X, pos.Y, pos.Z, distance, 0);
            Function.Call(Hash.CLEAR_AREA_OF_VEHICLES, pos.X, pos.Y, pos.Z, distance, 0);
            Function.Call(Hash.CLEAR_AREA_OF_COPS, pos.X, pos.Y, pos.Z, distance, 0);

            // All entities.
            Entity[] entities = World.GetAllEntities();
            foreach(Entity e in entities)
                if (e != PlayerPed.CurrentVehicle)
                    e?.Delete();
        }

        private void CurrentSceneOnExited(CustomScene scene, string newSceneFile, Vector3 exitRotation)
        {
            lock (_tickLock)
            {
                bool isActualScene = newSceneFile != "cmd_earth";
                CustomXmlScene newScene = ReadSceneFile(scene, newSceneFile);

                if (isActualScene && newScene.SurfaceFlag)
                    RaiseLandingGear();

                Game.FadeScreenOut(1000);
                Wait(1000);

                _currentScene?.Delete();
                _currentScene = null;

                ResetWeather();
                EndActiveScenarios();
                ClearAllEntities();

                if (newSceneFile != "cmd_earth")
                {
                    CreateScene(newScene, newSceneFile);
                    if (PlayerPed.IsInVehicle())
                        PlayerPed.CurrentVehicle.Rotation = exitRotation;
                    else PlayerPed.Rotation = exitRotation;
                }
                else
                {
                    GiveRespawnControlToGame();
                    if (PlayerPed.IsInVehicle())
                    {
                        Vehicle playerPedCurrentVehicle = PlayerPed.CurrentVehicle;
                        playerPedCurrentVehicle.Position = SpaceModDatabase.EarthAtmosphereEnterPosition;
                        playerPedCurrentVehicle.Rotation = Vector3.Zero;
                        playerPedCurrentVehicle.Heading = 243;
                        playerPedCurrentVehicle.HasGravity = true;
                    }
                    else PlayerPosition = SpaceModDatabase.TrevorAirport;
                    PlayerPed.HasGravity = true;
                    SpaceModLib.SetGravityLevel(0);
                }

                Wait(1000);
                Game.FadeScreenIn(1000);
            }
        }

        private void RaiseLandingGear()
        {
            Vehicle vehicle = PlayerPed.CurrentVehicle;
            if (PlayerPed.IsInVehicle() && Function.Call<bool>(Hash._0x4198AB0022B15F87, vehicle.Handle))
            {
                Function.Call(Hash._0xCFC8BE9A5E1FE575, vehicle.Handle, 0);
                DateTime landingGrearTimeout = DateTime.UtcNow + new TimeSpan(0, 0, 5);
                while (Function.Call<int>(Hash._0x9B0F3DCA3DB0F4CD, vehicle.Handle) != 0)
                {
                    if (DateTime.UtcNow > landingGrearTimeout)
                        break;
                    Yield();
                }
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

        private void GiveRespawnControlToGame()
        {
            Game.Globals[4].SetInt(0);
            Function.Call(Hash._DISABLE_AUTOMATIC_RESPAWN, false);
            Function.Call(Hash.SET_FADE_IN_AFTER_DEATH_ARREST, true);
            Function.Call(Hash.SET_FADE_OUT_AFTER_ARREST, true);
            Function.Call(Hash.SET_FADE_OUT_AFTER_DEATH, true);
            Function.Call(Hash.IGNORE_NEXT_RESTART, false);
        }
    }
}
