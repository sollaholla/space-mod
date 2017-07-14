﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using SpaceMod.Extensions;
using SpaceMod.Lib;
using SpaceMod.Scenarios;
using SpaceMod.Scenes;
using SpaceMod.Missions;
using SolomanMenu;
using Menu = SolomanMenu.Menu;

namespace SpaceMod
{
    internal class Core : Script
    {
        #region Variables
        internal static readonly string[] _weatherNames = {
            "EXTRASUNNY",
            "CLEAR",
            "CLOUDS",
            "SMOG",
            "FOGGY",
            "OVERCAST",
            "RAIN",
            "THUNDER",
            "CLEARING",
            "NEUTRAL",
            "SNOW",
            "BLIZZARD",
            "SNOWLIGHT",
            "XMAS"
        };

        #region Misc
        private Scene currentScene;
        private MenuConnector menuController;
        private Menu mainMenu;
        private readonly object _tickLock;
        #endregion

        #region Settings
        private string defaultSpaceScene = "EarthOrbit.space";
        private Vector3 defaultSpaceOffset = new Vector3(0, 2500, 0);
        private Vector3 defaultSpaceRotation = new Vector3(0, 0, 180);
        private bool menuEnabled = true;
        private bool preloadModels = false;
        private bool disableWantedStars = true;
        private bool resetWantedLevel = false;
        private float enterOrbitHeight = 5000;
        private Keys optionsMenuKey = Keys.NumPad9;
        private int missionStatus = 2;
        private bool didSetMissionFlag;
        private bool didRestartScripts;
        #endregion

        #region Internal Missions
        private IntroMission introMission = null;
        // private EndMission endMission = null;
        private bool endMissionCanStart;
        #endregion

        #endregion

        /// <summary>
        /// Our standard constructor.
        /// </summary>
        public Core()
        {
            _tickLock = new object();
            Scenarios = new List<Scenario>();

            Instance = this;
            KeyUp += OnKeyUp;
            Tick += OnTick;
            Aborted += OnAborted;

            ReadSettings();
            SaveSettings();
            CreateCustomMenu();
            RequestModels();

            Debug.Log("Initialized!", DebugMessageType.Debug);
        }

        #region Properties
        /// <summary>
        /// The instance of the Core.cs script.
        /// </summary>
        internal static Core Instance { get; private set; }

        /// <summary>
        /// The players character/ped.
        /// </summary>
        public Ped PlayerPed => Game.Player.Character;

        /// <summary>
        /// The position of the player in the game.
        /// </summary>
        public Vector3 PlayerPosition {
            get { return PlayerPed.IsInVehicle() ? PlayerPed.CurrentVehicle.Position : PlayerPed.Position; }
            set {
                if (PlayerPed.IsInVehicle()) PlayerPed.CurrentVehicle.Position = value;
                else PlayerPed.Position = value;
            }
        }

        /// <summary>
        /// The scenarios loaded for the current scene. These are internal so that the integrity of 
        /// another persons custom scenario cannot be redacted.
        /// </summary>
        public List<Scenario> Scenarios { get; private set; }
        #endregion

        #region Methods
        /// <summary>
        /// Get the currently active scene. Will return <see cref="default"/> if not assigned.
        /// </summary>
        /// <returns></returns>
        internal Scene GetCurrentScene()
        {
            return currentScene ?? default(Scene);
        }

        protected override void Dispose(bool dispose)
        {
            Debug.ClearLog();
            Debug.Log("Thread disposed...");
            base.Dispose(dispose);
        }

        #region Events
        private void OnAborted(object sender, EventArgs eventArgs)
        {
            if (!PlayerPed.IsDead && (Game.IsScreenFadedOut || Game.IsScreenFadingOut))
                Game.FadeScreenIn(0);

            PlayerPed.Task.ClearAll();
            PlayerPed.HasGravity = true;
            PlayerPed.FreezePosition = false;
            PlayerPed.FreezePosition = false;

            Game.TimeScale = 1.0f;
            World.RenderingCamera = null;
            SpaceModLib.SetGravityLevel(9.81f);
            SpaceModLib.RestartScript("blip_controller");
            SpaceModCppLib.CutCredits();
            Function.Call(Hash.CLEAR_TIMECYCLE_MODIFIER);
            Game.MissionFlag = !didSetMissionFlag;
            Effects.Stop();

            currentScene?.Delete();
            AbortActiveScenarios();
            if (currentScene != default(Scene))
            {
                GiveRespawnControlToGame();

                if (!PlayerPed.IsDead)
                {
                    PlayerPosition = Database.TrevorAirport;
                }

                ResetWeather();
            }
            currentScene = null;

            introMission?.OnAborted();
            // endMission?.OnAborted();
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (menuController?.AreMenusVisible() ?? false)
                return;

            if (e.KeyCode != optionsMenuKey)
                return;

            mainMenu.Draw = true;
        }

        private void OnTick(object sender, EventArgs eventArgs)
        {
            if (!Monitor.TryEnter(_tickLock)) return;
            try
            {
                try
                {
                    menuController?.UpdateMenus();

                    DisableWantedStars();

                    if (currentScene != null)
                    {
                        if (didRestartScripts)
                        {
                            SpaceModLib.TerminateScriptByName("blip_controller");
                            didRestartScripts = false;
                        }

                        Game.MissionFlag = didSetMissionFlag = true;
                        DoSceneUpdate();
                    }
                    else
                    {
                        if (!didRestartScripts)
                        {
                            SpaceModLib.RestartScript("blip_controller");
                            didRestartScripts = true;
                        }

                        Game.MissionFlag = didSetMissionFlag = false;
                        DoEarthUpdate();
                    }

                    RunInternalMissions();
                }
                // Locks our tick method so that if the last tick did 
                // not finish we will wait until it does to exit this method.
                finally
                {
                    Monitor.Exit(_tickLock);
                }
            }
            // Catch any exception in our mod, since this is our only script.
            catch (Exception ex)
            {
                Debug.Log(ex.Message + Environment.NewLine + ex.StackTrace, DebugMessageType.Error);
                throw;
            }
        }
        #endregion

        #region Initialize
        private void ReadSettings()
        {
            enterOrbitHeight = Settings.GetValue("mod", "enter_orbit_height", enterOrbitHeight);
            optionsMenuKey = Settings.GetValue("mod", "options_menu_key", optionsMenuKey);
            menuEnabled = Settings.GetValue("mod", "menu_enabled", menuEnabled);
            preloadModels = Settings.GetValue("mod", "pre_load_models", preloadModels);
            defaultSpaceScene = Settings.GetValue("mod", "default_orbit_scene", defaultSpaceScene);
            defaultSpaceOffset = ParseVector3.Read(Settings.GetValue("mod", "default_orbit_offset"), defaultSpaceOffset);
            defaultSpaceRotation = ParseVector3.Read(Settings.GetValue("mod", "default_orbit_rotation"), defaultSpaceRotation);
            missionStatus = Settings.GetValue("main_mission", "mission_status", missionStatus);
            SpaceMod.Settings.UseSpaceWalk = Settings.GetValue("settings", "use_spacewalk", SpaceMod.Settings.UseSpaceWalk);
            SpaceMod.Settings.ShowCustomUi = Settings.GetValue("settings", "show_custom_ui", SpaceMod.Settings.ShowCustomUi);
            SpaceMod.Settings.UseScenarios = Settings.GetValue("settings", "use_scenarios", SpaceMod.Settings.UseScenarios);
            SpaceMod.Settings.MoonJump = Settings.GetValue("settings", "low_gravity_jumping", SpaceMod.Settings.MoonJump);
            SpaceMod.Settings.MouseControlFlySensitivity = Settings.GetValue("vehicle_settings", "mouse_control_fly_sensitivity", SpaceMod.Settings.MouseControlFlySensitivity);
            SpaceMod.Settings.DefaultVehicleSpawn = Settings.GetValue("vehicle_settings", "vehicle_surface_spawn", SpaceMod.Settings.DefaultVehicleSpawn);
            SpaceMod.Settings.VehicleFlySpeed = Settings.GetValue("vehicle_settings", "vehicle_fly_speed", SpaceMod.Settings.VehicleFlySpeed);
            endMissionCanStart = CanStartEndMission();
        }

        private void SaveSettings()
        {
            Settings.SetValue("mod", "enter_orbit_height", enterOrbitHeight);
            Settings.SetValue("mod", "options_menu_key", optionsMenuKey);
            Settings.SetValue("mod", "menu_enabled", menuEnabled);
            Settings.SetValue("mod", "pre_load_models", preloadModels);
            Settings.SetValue("mod", "default_orbit_scene", defaultSpaceScene);
            Settings.SetValue("mod", "default_orbit_offset", defaultSpaceOffset);
            Settings.SetValue("mod", "default_orbit_rotation", defaultSpaceRotation);
            Settings.SetValue("main_mission", "mission_status", missionStatus);
            Settings.SetValue("settings", "use_spacewalk", SpaceMod.Settings.UseSpaceWalk);
            Settings.SetValue("settings", "show_custom_ui", SpaceMod.Settings.ShowCustomUi);
            Settings.SetValue("settings", "use_scenarios", SpaceMod.Settings.UseScenarios);
            Settings.SetValue("settings", "low_gravity_jumping", SpaceMod.Settings.MoonJump);
            Settings.SetValue("vehicle_settings", "mouse_control_fly_sensitivity", SpaceMod.Settings.MouseControlFlySensitivity);
            Settings.SetValue("vehicle_settings", "vehicle_surface_spawn", SpaceMod.Settings.DefaultVehicleSpawn);
            Settings.SetValue("vehicle_settings", "vehicle_fly_speed", SpaceMod.Settings.VehicleFlySpeed);
            Settings.Save();
        }

        private bool CanStartEndMission()
        {
            if (Settings.GetValue("main_mission", "mission_status", 0) != 1)
                return false;
            ScriptSettings scenarioSettings;

            scenarioSettings = ScriptSettings.Load(Database.PathToScenarios + "/MoonMission01.ini");
            if (!scenarioSettings.GetValue("scenario_config", "complete", false))
                return false;
            scenarioSettings = ScriptSettings.Load(Database.PathToScenarios + "/MarsMission01.ini");
            if (!scenarioSettings.GetValue("scenario_config", "complete", false))
                return false;
            scenarioSettings = ScriptSettings.Load(Database.PathToScenarios + "/MarsMission02.ini");
            if (!scenarioSettings.GetValue("scenario_config", "complete", false))
                return false;

            return true;
        }

        private void CreateCustomMenu()
        {
            if (!menuEnabled)
                return;

            menuController = new MenuConnector();
            mainMenu = new Menu("Space Mod", Color.FromArgb(125, Color.Black), Color.Black,
                Color.Purple)
            { MenuItemHeight = 26 };

            #region scenes

            var scenesMenu = mainMenu.AddParentMenu("Scenes", mainMenu);
            scenesMenu.Width = mainMenu.Width;

            var files = Directory.GetFiles(Database.PathToScenes).Where(file => file.EndsWith(".space")).ToArray();
            for (var i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var fileName = Path.GetFileName(file);
                var menuItem = new SolomanMenu.MenuItem(fileName);
                menuItem.ItemActivated += (sender, item) =>
                {
                    SceneInfo newScene = DeserializeFileAsScene(fileName);

                    SetCurrentScene(newScene, fileName);

                    UI.Notify($"{Database.NotifyHeader}Loaded: {fileName}");

                    menuController.Menus.ForEach(m => m.Draw = false);
                };
                scenesMenu.Add(menuItem);
            }

            #endregion

            #region settings

            var settingsMenu = mainMenu.AddParentMenu("Settings", mainMenu);

            #region ui settings

            var userInterfaceMenu = settingsMenu.AddParentMenu("Interface", mainMenu);

            var showCustomUiCheckbox = new CheckboxMenuItem("Show Custom UI", SpaceMod.Settings.ShowCustomUi);
            showCustomUiCheckbox.Checked += (sender, check) =>
            {
                SpaceMod.Settings.ShowCustomUi = check;
            };

            userInterfaceMenu.Add(showCustomUiCheckbox);

            #endregion

            #region vehicle settings

            var vehicleSettingsMenu = settingsMenu.AddParentMenu("Vehicles", mainMenu);
            vehicleSettingsMenu.Width = mainMenu.Width;

            List<dynamic> lst;
            int flyIndex;
            var vehicleSpeedList = new ListMenuItem("Vehicle Speed",
                lst = Enumerable.Range(1, 20).Select(i => (dynamic)(i * 5)).ToList(), (flyIndex = lst.IndexOf(SpaceMod.Settings.VehicleFlySpeed)) == -1 ? 0 : flyIndex);
            vehicleSpeedList.IndexChanged += (sender, index, item) =>
            {
                SpaceMod.Settings.VehicleFlySpeed = item;
            };

            var flySensitivity = (int)SpaceMod.Settings.MouseControlFlySensitivity;
            var vehicleSensitivityList = new ListMenuItem("Mouse Control Sensitivity",
                Enumerable.Range(0, flySensitivity > 15 ? flySensitivity + 5 : 15).Select(i => (dynamic)i).ToList(),
                flySensitivity);
            vehicleSensitivityList.IndexChanged += (sender, index, item) =>
            {
                SpaceMod.Settings.MouseControlFlySensitivity = item;
            };

            vehicleSettingsMenu.Add(vehicleSpeedList);
            vehicleSettingsMenu.Add(vehicleSensitivityList);

            #endregion

            #region scene settings

            var sceneSettingsMenu = settingsMenu.AddParentMenu("Scenes", mainMenu);

            var useScenariosCheckbox = new CheckboxMenuItem("Use Scenarios", SpaceMod.Settings.UseScenarios);
            useScenariosCheckbox.Checked += (sender, check) =>
            {
                SpaceMod.Settings.UseScenarios = check;
            };

            sceneSettingsMenu.Add(useScenariosCheckbox);

            #endregion

            #region player settings

            var playerSettingsMenu = settingsMenu.AddParentMenu("Player", mainMenu);

            var useFloatingCheckbox = new CheckboxMenuItem("Use SpaceWalk", SpaceMod.Settings.UseSpaceWalk);
            useFloatingCheckbox.Checked += (sender, check) =>
            {
                SpaceMod.Settings.UseSpaceWalk = check;
            };

            playerSettingsMenu.Add(useFloatingCheckbox);

            #endregion

            var saveSettingsItem = new SolomanMenu.MenuItem("Save Settings");
            saveSettingsItem.ItemActivated += (sender, item) =>
            {
                SaveSettings();
                UI.Notify(Database.NotifyHeader + "Settings ~b~saved~s~.", true);
            };

            settingsMenu.Add(saveSettingsItem);

            var disableWantedLevelCheckbox = new CheckboxMenuItem("Disable Wanted Level", disableWantedStars);
            disableWantedLevelCheckbox.Checked += (a, b) =>
            {
                disableWantedStars = b;
            };
            settingsMenu.Add(disableWantedLevelCheckbox);

            #endregion

            #region debug

            var debugButton = new SolomanMenu.MenuItem("Debug Player");
            debugButton.ItemActivated += (sender, item) =>
            {
                Debug.LogEntityData(PlayerPed);
            };

            mainMenu.Add(debugButton);

            #endregion

            menuController.Menus.Add(mainMenu);
            menuController.Menus.Add(settingsMenu);
            menuController.Menus.Add(userInterfaceMenu);
            menuController.Menus.Add(vehicleSettingsMenu);
            menuController.Menus.Add(sceneSettingsMenu);
            menuController.Menus.Add(playerSettingsMenu);
            menuController.Menus.Add(scenesMenu);
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
        #endregion

        #region Runtime Scene Updates
        private void DoEarthUpdate()
        {
            float height = PlayerPed.HeightAboveGround;

            if (height > enterOrbitHeight)
            {
                SceneInfo scene = XmlSerializer.Deserialize<SceneInfo>(Path.Combine(Database.PathToScenes, defaultSpaceScene));
                SetCurrentScene(scene, defaultSpaceScene);

                PlayerPosition += defaultSpaceOffset;
                if (PlayerPed.IsInVehicle()) PlayerPed.CurrentVehicle.Rotation = defaultSpaceRotation;
                else PlayerPed.Rotation = defaultSpaceRotation;
            }
        }

        private void DoSceneUpdate()
        {
            Scenarios?.ForEach(scenario => scenario.Update());
            Function.Call(Hash._CLEAR_CLOUD_HAT);
            if (PlayerPed.CurrentVehicle != null)
                PlayerPed.CurrentVehicle.HasGravity = currentScene.Info.UseGravity;
            else PlayerPed.HasGravity = currentScene.Info.UseGravity;

            currentScene.Update();
        }

        private void RunInternalMissions()
        {
            if (currentScene != null)
            {
                introMission?.OnAborted();
                // endMission?.OnAborted();
                return;
            }
            if (!endMissionCanStart && missionStatus == 0)
            {
                if (introMission != null)
                {
                    introMission.Update();
                }
                else
                {
                    introMission = new IntroMission();
                    introMission.OnStart();

                    introMission.completed += (scenario, success) =>
                    {
                        SetMissionStatus(1);
                        introMission = null;
                    };
                }
            }
            else if (endMissionCanStart && missionStatus == 1)
            {
                // TODO: Make end mission start.
            }
        }

        private void SetMissionStatus(int value)
        {
            missionStatus = value;
            Settings.SetValue("main_mission", "mission_status", missionStatus);
            Settings.Save();
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
        #endregion

        #region Scene Management
        private SceneInfo DeserializeFileAsScene(string fileName)
        {
            if (fileName == "cmd_earth")
                return null;

            SceneInfo newScene = XmlSerializer.Deserialize<SceneInfo>(Database.PathToScenes + "\\" + fileName);

            if (newScene == default(SceneInfo))
            {
                UI.Notify(Database.NotifyHeader + "Scene file " + fileName + " couldn't be read, or doesn't exist.");

                return null;
            }

            return newScene;
        }

        private void SetCurrentScene(SceneInfo scene, string fileName = default(string))
        {
            Game.FadeScreenOut(1000);
            Wait(1000);
            CreateScene(scene, fileName);
            Wait(1000);
            Game.FadeScreenIn(1000);
        }

        private void CreateScene(SceneInfo scene, string fileName = default(string))
        {
            lock (_tickLock)
            {
                EndActiveScenarios();
                ClearAllEntities();

                if (currentScene != null && currentScene != default(Scene))
                    currentScene.Delete();

                if (PlayerPed.IsInVehicle()) PlayerPed.CurrentVehicle.Rotation = Vector3.Zero;
                else PlayerPed.Rotation = Vector3.Zero;

                currentScene = new Scene(scene) { FileName = fileName };
                currentScene.Start();

                Function.Call(Hash.SET_CLOCK_TIME, currentScene.Info.Time);
                Function.Call(Hash.PAUSE_CLOCK, true);

                if (currentScene.Weather == (Weather)14) Function.Call(Hash.SET_WEATHER_TYPE_NOW, "HALLOWEEN");
                else World.Weather = currentScene.Weather;

                if ((int)World.Weather != -1)
                    Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, _weatherNames[(int)World.Weather]);

                if (SpaceMod.Settings.UseScenarios)
                {
                    try
                    {
                        foreach (ScenarioInfo scenarioInfo in scene.Scenarios)
                        {
                            Assembly assembly = Assembly.LoadFrom(Path.Combine(Database.PathToScenarios, scenarioInfo.Dll));

                            if (assembly == null)
                                continue;

                            Type type = assembly.GetType(scenarioInfo.Namespace);

                            if (type == null || type.BaseType != typeof(Scenario))
                                continue;

                            Debug.Log("Creating Scenario: " + type.Name);

                            Scenario instance = (Scenario)Activator.CreateInstance(type);

                            instance.OnAwake();

                            if (instance.IsScenarioComplete())
                                continue;

                            Scenarios.Add(instance);
                        }

                        foreach (Scenario scenario in Scenarios)
                        {
                            scenario.OnStart();

                            scenario.completed += ScenarioComplete;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Log(ex.Message + Environment.NewLine + ex.StackTrace, DebugMessageType.Error);
                    }
                }
                currentScene.Exited += CurrentSceneOnExited;
            }
        }

        private void ScenarioComplete(Scenario scenario, bool success)
        {
            lock (_tickLock)
            {
                Scenarios.Remove(scenario);
            }
        }

        private void ClearAllEntities(Vector3 pos = default(Vector3), float distance = int.MaxValue)
        {
            Entity[] entities = World.GetAllEntities();

            foreach (Entity e in entities)
            {
                if (!e.IsDead && (e.GetType() == typeof(Ped) || (e.GetType() == typeof(Vehicle) && PlayerPed.CurrentVehicle == ((Vehicle)e))))
                    continue;

                e?.Delete();
            }
        }

        private void CurrentSceneOnExited(Scene scene, string newSceneFile, Vector3 exitRotation, Vector3 exitOffset)
        {
            lock (_tickLock)
            {
                bool isActualScene = newSceneFile != "cmd_earth";

                SceneInfo newScene = DeserializeFileAsScene(newSceneFile);

                if (isActualScene && newScene == null)
                {
                    OnAborted(this, new EventArgs());
                    return;
                }

                if (isActualScene && newScene.SurfaceScene)

                    RaiseLandingGear();

                else if (!isActualScene)
                    // if this ISNT an actual scene check if we can start the end mission.
                    endMissionCanStart = CanStartEndMission();

                Game.FadeScreenOut(1000);
                Wait(1000);

                currentScene?.Delete();
                currentScene = null;

                ResetWeather();
                EndActiveScenarios();
                ClearAllEntities();

                if (newSceneFile != "cmd_earth")
                {
                    CreateScene(newScene, newSceneFile);

                    // AFTER creating the scene we set our offsets/rotations so that
                    // values set within the start of the scene are overriden.
                    if (PlayerPed.IsInVehicle())
                    {
                        PlayerPed.CurrentVehicle.Rotation = exitRotation;
                        PlayerPed.CurrentVehicle.Position += exitOffset;
                    }
                    else
                    {
                        PlayerPed.Rotation = exitRotation;
                        PlayerPed.Position += exitOffset;
                    }
                }
                else
                {
                    Function.Call(Hash.PAUSE_CLOCK, false);

                    GiveRespawnControlToGame();

                    if (PlayerPed.IsInVehicle())
                    {
                        Vehicle playerPedCurrentVehicle = PlayerPed.CurrentVehicle;
                        playerPedCurrentVehicle.Position = Database.EarthAtmosphereEnterPosition;
                        playerPedCurrentVehicle.Rotation = Vector3.Zero;
                        playerPedCurrentVehicle.Heading = 243;
                        playerPedCurrentVehicle.HasGravity = true;
                    }
                    else PlayerPosition = Database.TrevorAirport;

                    PlayerPed.HasGravity = true;

                    SpaceModLib.SetGravityLevel(9.81f);
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

        private void AbortActiveScenarios()
        {
            if (Scenarios != null)
            {
                while (Scenarios.Count > 0)
                {
                    var scenario = Scenarios[0];
                    scenario.OnAborted();
                    Scenarios.RemoveAt(0);
                }
            }
        }
        #endregion

        #region Utility
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

        private void GiveRespawnControlToGame()
        {
            Game.Globals[4].SetInt(0);
            Function.Call(Hash._DISABLE_AUTOMATIC_RESPAWN, false);
            Function.Call(Hash.SET_FADE_IN_AFTER_DEATH_ARREST, true);
            Function.Call(Hash.SET_FADE_OUT_AFTER_ARREST, true);
            Function.Call(Hash.SET_FADE_OUT_AFTER_DEATH, true);
            Function.Call(Hash.IGNORE_NEXT_RESTART, false);
        }
        #endregion
        #endregion
    }
}

// DEBUG
//////////////////////////////////////

//private void DoEndMission()
//{
//    if (!_missionsComplete || endMissionComplete || _currentScene != null)
//    {
//        if(introMissionComplete)
//        {
//            colonel?.Delete();
//            colonel = null;
//        }
//        return;
//    }

//    if (!Entity.Exists(colonel))
//    {
//        colonel = World.CreatePed(PedHash.Marine01SMM, new Vector3(-2356.895f, 3248.412f, 101.4508f), 313.5386f);
//        return;
//    }

//    SetColonelRelationship();
//    CreateColonelBlip();

//    float distance = Vector3.Distance(PlayerPosition, colonel.Position);
//    if (distance > 1.75f)
//        return;

//    World.DrawMarker(MarkerType.UpsideDownCone, colonel.Position + Vector3.WorldUp * 1.5f, Vector3.RelativeRight, Vector3.Zero, new Vector3(0.35f, 0.35f, 0.35f), Color.Red);
//    SpaceModLib.DisplayHelpTextWithGXT("END_LABEL_1");

//    if (Game.IsControlJustPressed(2, GTA.Control.Context))
//    {
//        Function.Call(Hash._PLAY_AMBIENT_SPEECH1, colonel.Handle, "Generic_Hi", "Speech_Params_Force");
//        PlayerPed.FreezePosition = true;
//        PlayerPed.Heading = (colonel.Position - PlayerPosition).ToHeading();
//        PlayerPed.Task.StandStill(-1);

//        while (colonel.Exists())
//        {
//            SpaceModLib.ShowSubtitleWithGXT("END_LABEL_2", 10000);
//            Wait(10000);
//            Function.Call(Hash._PLAY_AMBIENT_SPEECH1, PlayerPed.Handle, "Generic_Thanks", "Speech_Params_Force_Shouted_Critical");
//            SpaceModLib.ShowSubtitleWithGXT("END_LABEL_3");
//            Wait(5000);
//            Game.FadeScreenOut(1000);
//            Wait(1000);
//            colonel.Delete();
//            PlayerPed.Task.ClearAllImmediately();
//            PlayerPed.FreezePosition = false;
//            Wait(1000);
//            Game.FadeScreenIn(1000);
//            endMissionComplete = true;
//            Settings.SetValue("settings", "end_mission_complete", endMissionComplete);
//            Settings.Save();
//            GTSLib.RollCredits();
//            Wait(30000);
//            GTSLib.CutCredits();
//            Yield();
//        }
//    }
//}

