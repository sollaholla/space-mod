using System;
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
using GTS.Extensions;
using GTS.Library;
using GTS.Missions;
using GTS.Scenarios;
using GTS.Scenes;
using NativeUI;

namespace GTS
{
    /// <summary>
    ///     This is the controller of all script threads.
    /// </summary>
    internal class Core : Script
    {
        /// <summary>
        ///     Our standard constructor.
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

            Debug.Log("Initialized!");
        }

        #region Variables

        #region Misc

        private Scene _currentScene;
        private MenuPool _menuPool;
        private UIMenu _mainMenu;
        private bool _didAbort;
        private readonly object _tickLock;

        #endregion

        #region Settings

        private string _defaultSpaceScene = "EarthOrbit.space";
        private Vector3 _defaultSpaceOffset = new Vector3(0, 2500, 0);
        private Vector3 _defaultSpaceRotation = new Vector3(0, 0, 180);
        private bool _menuEnabled = true;
        private bool _loadModelsAtStart;
        private bool _disableWantedStars = true;
        private bool _resetWantedLevel;
        private float _enterOrbitHeight = 7500;
        private Keys _optionsMenuKey = Keys.NumPad9;
        private int _missionStatus = 2;
        private bool _didSetMissionFlag;
        private bool _didRestartEarthUpdate = true;

        #endregion

        #region Internal Missions

        private IntroMission _introMission;

        // private EndMission endMission = null;
        private bool _endMissionCanStart;

        #endregion

        #endregion

        #region Properties

        /// <summary>
        ///     The instance of the <see cref="Core" /> .cs script.
        /// </summary>
        internal static Core Instance { get; private set; }

        /// <summary>
        ///     The players character/ped.
        /// </summary>
        public Ped PlayerPed => Game.Player.Character;

        /// <summary>
        ///     The position of the player in the game.
        /// </summary>
        public Vector3 PlayerPosition
        {
            get => PlayerPed.IsInVehicle() ? PlayerPed.CurrentVehicle.Position : PlayerPed.Position;
            set
            {
                if (PlayerPed.IsInVehicle()) PlayerPed.CurrentVehicle.Position = value;
                else PlayerPed.Position = value;
            }
        }

        /// <summary>
        ///     The scenarios loaded for the current scene. These are
        ///     <see langword="internal" /> so that the integrity of another persons
        ///     custom scenario cannot be redacted.
        /// </summary>
        public List<Scenario> Scenarios { get; }

        #endregion

        #region Methods

        /// <summary>
        ///     Get the currently active scene..
        /// </summary>
        /// <returns>
        /// </returns>
        internal Scene GetCurrentScene()
        {
            return _currentScene;
        }

        protected override void Dispose(bool dispose)
        {
            Debug.ClearLog();
            Debug.Log("Thread disposed...");
            if (!_didAbort) OnAborted(null, new EventArgs());
            base.Dispose(dispose);
        }

        #region Events

        private void OnAborted(object sender, EventArgs eventArgs)
        {
            // This tells the dispose method not to abort if we already did.
            _didAbort = true;

            // If the game gets stuck loading then we need to get out of that.
            if (!PlayerPed.IsDead && (Game.IsScreenFadedOut || Game.IsScreenFadingOut))
                Game.FadeScreenIn(0);

            // Reset the player.
            PlayerPed.Task.ClearAll();
            PlayerPed.HasGravity = true;
            PlayerPed.FreezePosition = false;
            PlayerPed.CanRagdoll = false;

            // Reset the game.
            Game.TimeScale = 1.0f;
            World.RenderingCamera = null;
            Utils.SetGravityLevel(9.81f);
            GtsLib.CutCredits();
            GtsLib.RestoreWater();
            Function.Call(Hash.CLEAR_TIMECYCLE_MODIFIER);
            Game.MissionFlag = !_didSetMissionFlag;
            Effects.Stop();

            // Stop the current scene and reset the game
            // changes from that.
            AbortActiveScenarios();
            _currentScene?.Delete();
            if (_currentScene != default(Scene))
            {
                GiveSpawnControlToGame();
                if (!PlayerPed.IsDead)
                    PlayerPosition = Database.TrevorAirport;
                ResetWeather();
            }
            _currentScene = null;

            // Quit the internal missions.
            _introMission?.OnAborted();
            // endMission?.OnAborted();

            ///////////////////////////////////////////////////////////
            // NOTE: Putting this on the bottom since it uses some
            // Wait() statements, that will stop anything else from 
            // executing. Not sure how we can do this properly, because
            // if the script no longer exists it won't be restarted
            // when we abort. :/
            ///////////////////////////////////////////////////////////
            Utils.RestartScript("blip_controller");
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (_menuPool?.IsAnyMenuOpen() ?? false)
                return;

            if (e.KeyCode != _optionsMenuKey)
                return;

            _mainMenu.Visible = !_mainMenu.Visible;
        }

        private void OnTick(object sender, EventArgs eventArgs)
        {
            if (!Monitor.TryEnter(_tickLock)) return;
            try
            {
                try
                {
                    ProcessMenus();
                    SetVarsDependantOnSceneNull();
                    DisableWantedStars();
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
                Abort();
            }
        }

        #endregion

        private void ProcessMenus()
        {
            if (_menuPool == null) return;
            _menuPool.ProcessMenus();
            if (!_menuPool.IsAnyMenuOpen()) return;
            UI.HideHudComponentThisFrame(HudComponent.HelpText);
        }

        #region Initialize

        private void ReadSettings()
        {
            _enterOrbitHeight = Settings.GetValue("mod", "enter_orbit_height", _enterOrbitHeight);
            _optionsMenuKey = Settings.GetValue("mod", "options_menu_key", _optionsMenuKey);
            _menuEnabled = Settings.GetValue("mod", "menu_enabled", _menuEnabled);
            _loadModelsAtStart = Settings.GetValue("mod", "load_models_on_start", _loadModelsAtStart);
            _defaultSpaceScene = Settings.GetValue("mod", "default_orbit_scene", _defaultSpaceScene);
            _defaultSpaceOffset = ParseVector3.Read(Settings.GetValue("mod", "default_orbit_offset"),
                _defaultSpaceOffset);
            _defaultSpaceRotation = ParseVector3.Read(Settings.GetValue("mod", "default_orbit_rotation"),
                _defaultSpaceRotation);
            _missionStatus = Settings.GetValue("main_mission", "mission_status", _missionStatus);
            GTS.Settings.UseSpaceWalk = Settings.GetValue("settings", "use_spacewalk", GTS.Settings.UseSpaceWalk);
            GTS.Settings.ShowCustomGui = Settings.GetValue("settings", "show_custom_Gui", GTS.Settings.ShowCustomGui);
            GTS.Settings.UseScenarios = Settings.GetValue("settings", "use_scenarios", GTS.Settings.UseScenarios);
            GTS.Settings.MoonJump = Settings.GetValue("settings", "low_gravity_jumping", GTS.Settings.MoonJump);
            GTS.Settings.MouseControlFlySensitivity = Settings.GetValue("vehicle_settings",
                "mouse_control_fly_sensitivity", GTS.Settings.MouseControlFlySensitivity);
            GTS.Settings.DefaultVehicleSpawn = Settings.GetValue("vehicle_settings", "vehicle_surface_spawn",
                GTS.Settings.DefaultVehicleSpawn);
            GTS.Settings.VehicleFlySpeed =
                Settings.GetValue("vehicle_settings", "vehicle_fly_speed", GTS.Settings.VehicleFlySpeed);
            _endMissionCanStart = CanStartEndMission();
            GTS.Settings.LowConfigMode = Settings.GetValue("game", "low_config", false);
            GTS.Settings.EarthAtmosphereEnterPosition =
                ParseVector3.Read(Settings.GetValue("mod", "enter_atmos_pos"),
                    GTS.Settings.EarthAtmosphereEnterPosition);
        }

        private void SaveSettings()
        {
            Settings.SetValue("mod", "enter_orbit_height", _enterOrbitHeight);
            Settings.SetValue("mod", "options_menu_key", _optionsMenuKey);
            Settings.SetValue("mod", "menu_enabled", _menuEnabled);
            Settings.SetValue("mod", "load_models_on_start", _loadModelsAtStart);
            Settings.SetValue("mod", "default_orbit_scene", _defaultSpaceScene);
            Settings.SetValue("mod", "default_orbit_offset", _defaultSpaceOffset);
            Settings.SetValue("mod", "default_orbit_rotation", _defaultSpaceRotation);
            Settings.SetValue("main_mission", "mission_status", _missionStatus);
            Settings.SetValue("settings", "use_spacewalk", GTS.Settings.UseSpaceWalk);
            Settings.SetValue("settings", "show_custom_Gui", GTS.Settings.ShowCustomGui);
            Settings.SetValue("settings", "use_scenarios", GTS.Settings.UseScenarios);
            Settings.SetValue("settings", "low_gravity_jumping", GTS.Settings.MoonJump);
            Settings.SetValue("vehicle_settings", "mouse_control_fly_sensitivity",
                GTS.Settings.MouseControlFlySensitivity);
            Settings.SetValue("vehicle_settings", "vehicle_surface_spawn", GTS.Settings.DefaultVehicleSpawn);
            Settings.SetValue("vehicle_settings", "vehicle_fly_speed", GTS.Settings.VehicleFlySpeed);
            Settings.SetValue("game", "low_config", GTS.Settings.LowConfigMode);
            Settings.SetValue("mod", "enter_atmos_pos", GTS.Settings.EarthAtmosphereEnterPosition);
            Settings.Save();
        }

        private void SetVarsDependantOnSceneNull()
        {
            if (_currentScene != null)
                SceneNotNull();
            else
                SceneNull();
        }

        private void SceneNull()
        {
            Game.MissionFlag = _didSetMissionFlag = false;
            DoEarthUpdate();

            if (!_didRestartEarthUpdate)
            {
                _didRestartEarthUpdate = true;
                GtsLib.RestoreWater();
                Utils.RestartScript("blip_controller"); // Beware of this function, it may delay the mod.
            }
        }

        private void SceneNotNull()
        {
            Game.MissionFlag = _didSetMissionFlag = true;

            if (_currentScene.Info != null)
                DoSceneUpdate();

            if (_didRestartEarthUpdate)
            {
                _didRestartEarthUpdate = false;
                GtsLib.RemoveWater();
                Utils.TerminateScriptByName("blip_controller");
            }
        }

        private bool CanStartEndMission()
        {
            if (Settings.GetValue("main_mission", "mission_status", 0) != 1)
                return false;

            // TODO FIXME: Need to do something better than this.
            var scenarioSettings = ScriptSettings.Load(Database.PathToScenarios + "/MoonMission01.ini");
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
            if (!_menuEnabled)
                return;

            _menuPool = new MenuPool();
            _mainMenu = new UIMenu("GTS Options", "Core");

            #region Scenes

            var scenesMenu = _menuPool.AddSubMenu(_mainMenu, "Scenes");
            var filePaths = Directory.GetFiles(Database.PathToScenes).Where(file => file.EndsWith(".space")).ToArray();
            foreach (var path in filePaths)
            {
                var fileName = Path.GetFileName(path);
                var menuItem = new UIMenuItem(fileName);
                menuItem.Activated += (sender, item) =>
                {
                    var newScene = DeserializeFileAsScene(fileName);
                    SetCurrentScene(newScene, fileName);
                    UI.Notify($"{Database.NotifyHeader}Loaded: {fileName}");
                    _menuPool.CloseAllMenus();
                };
                scenesMenu.AddItem(menuItem);
            }

            #endregion

            #region Settings

            var settingsMenu = _menuPool.AddSubMenu(_mainMenu, "Settings");

            #region User Interface Settings

            var userInterfaceMenu = _menuPool.AddSubMenu(settingsMenu, "Interface");
            var showCustomUiCheckbox = new UIMenuCheckboxItem("Show Custom UI", GTS.Settings.ShowCustomGui);
            showCustomUiCheckbox.CheckboxEvent += (sender, check) => { GTS.Settings.ShowCustomGui = check; };
            userInterfaceMenu.AddItem(showCustomUiCheckbox);

            #endregion

            #region Vehicles Settings

            int flyIndex;
            List<dynamic> dynamicList;

            var vehicleSettingsMenu = _menuPool.AddSubMenu(settingsMenu, "Vehicles");
            var vehicleSpeedList = new UIMenuListItem("Vehicle Speed",
                dynamicList = Enumerable.Range(1, 20).Select(i => (dynamic) (i * 5)).ToList(),
                (flyIndex = dynamicList.IndexOf(GTS.Settings.VehicleFlySpeed)) == -1 ? 0 : flyIndex);
            vehicleSpeedList.OnListChanged += (sender, index) =>
            {
                GTS.Settings.VehicleFlySpeed = sender.IndexToItem(index);
            };

            var flySensitivity = (int) GTS.Settings.MouseControlFlySensitivity;
            var vehicleSensitivityList = new UIMenuListItem("Mouse Control Sensitivity",
                Enumerable.Range(0, flySensitivity > 15 ? flySensitivity + 5 : 15)
                    .Select(i => (dynamic) i).ToList(), flySensitivity);
            vehicleSensitivityList.OnListChanged += (sender, index) =>
            {
                GTS.Settings.MouseControlFlySensitivity = sender.IndexToItem(index);
            };

            vehicleSettingsMenu.AddItem(vehicleSpeedList);
            vehicleSettingsMenu.AddItem(vehicleSensitivityList);

            #endregion

            #region Scene Settings

            var sceneSettingsMenu = _menuPool.AddSubMenu(settingsMenu, "Scenes");
            var useScenariosCheckbox = new UIMenuCheckboxItem("Use Scenarios", GTS.Settings.UseScenarios);
            useScenariosCheckbox.CheckboxEvent += (sender, check) => { GTS.Settings.UseScenarios = check; };

            sceneSettingsMenu.AddItem(useScenariosCheckbox);

            #endregion

            #region Player Settings

            var playerSettingsMenu = _menuPool.AddSubMenu(settingsMenu, "Player");
            var useFloatingCheckbox = new UIMenuCheckboxItem("Use SpaceWalk", GTS.Settings.UseSpaceWalk);
            useFloatingCheckbox.CheckboxEvent += (sender, check) => { GTS.Settings.UseSpaceWalk = check; };

            playerSettingsMenu.AddItem(useFloatingCheckbox);

            #endregion

            var saveSettingsItem = new UIMenuItem("Save Settings");
            saveSettingsItem.SetLeftBadge(UIMenuItem.BadgeStyle.Star);
            saveSettingsItem.Activated += (sender, item) =>
            {
                SaveSettings();
                UI.Notify(Database.NotifyHeader + "Settings ~h~saved~s~!", true);
            };
            settingsMenu.AddItem(saveSettingsItem);

            var disableWantedLevelCheckbox = new UIMenuCheckboxItem("Disable Wanted Level", _disableWantedStars);
            disableWantedLevelCheckbox.CheckboxEvent += (a, b) => { _disableWantedStars = b; };
            settingsMenu.AddItem(disableWantedLevelCheckbox);

            #endregion

            #region Debug

            var debugButton = new UIMenuItem("Debug Player", "Log the player's position rotation and heading.");
            debugButton.SetLeftBadge(UIMenuItem.BadgeStyle.Alert);
            debugButton.Activated += (sender, item) => { Debug.LogEntityData(PlayerPed); };

            _mainMenu.AddItem(debugButton);

            #endregion

            _menuPool.Add(_mainMenu);
            _menuPool.RefreshIndex();
            _menuPool.SetBannerType(new Sprite("", "", new Point(), new Size(), 0,
                ColorTranslator.FromHtml("#8000ff")));
        }

        private void RequestModels()
        {
            // TODO FIXME: This seems to slow down the game, so it might
            // be a good idea to just ditch this feature.

            if (!_loadModelsAtStart)
                return;

            const string fileName = ".\\scripts\\Space\\RequestOnStart.txt";
            if (!File.Exists(fileName))
                return;

            using (var sr = File.OpenText(fileName))
            {
                string s;

                while ((s = sr.ReadLine()) != null)
                {
                    s = s.Trim();
                    var m = new Model(s);
                    if (!m.IsLoaded)
                        m.Request();
                }
            }
        }

        #endregion

        #region Runtime Scene Updates

        private void DoEarthUpdate()
        {
            // Let's us go to space from earth.

            var height = PlayerPed.HeightAboveGround;
            if (!(height > _enterOrbitHeight)) return;

            var scene = XmlSerializer.Deserialize<SceneInfo>(Path.Combine(Database.PathToScenes,
                _defaultSpaceScene));
            SetCurrentScene(scene, _defaultSpaceScene);

            PlayerPosition += _defaultSpaceOffset;
            if (PlayerPed.IsInVehicle()) PlayerPed.CurrentVehicle.Rotation = _defaultSpaceRotation;
            else PlayerPed.Rotation = _defaultSpaceRotation;
        }

        private void DoSceneUpdate()
        {
            // Updates the scene.
            Function.Call(Hash.SET_WIND_SPEED, 0.0f);
            Scenarios?.ForEach(scenario => scenario.Update());
            Function.Call(Hash._CLEAR_CLOUD_HAT);
            if (PlayerPed.CurrentVehicle != null)
                PlayerPed.CurrentVehicle.HasGravity = _currentScene.Info.UseGravity;
            else PlayerPed.HasGravity = _currentScene.Info.UseGravity;
            _currentScene.Update();
        }

        private void RunInternalMissions()
        {
            if (_currentScene != null)
            {
                _introMission?.OnAborted();
                // endMission?.OnAborted();
                return;
            }
            if (!_endMissionCanStart && _missionStatus == 0)
            {
                if (_introMission != null)
                {
                    _introMission.Update();
                }
                else
                {
                    _introMission = new IntroMission();
                    _introMission.OnStart();

                    _introMission.Completed += (scenario, success) =>
                    {
                        SetMissionStatus(1);
                        _introMission = null;
                    };
                }
            }
            else if (_endMissionCanStart && _missionStatus == 1)
            {
                // TODO: Make end mission start.
            }
        }

        private void SetMissionStatus(int value)
        {
            _missionStatus = value;
            Settings.SetValue("main_mission", "mission_status", _missionStatus);
            Settings.Save();
        }

        private void DisableWantedStars()
        {
            if (!_disableWantedStars)
            {
                if (_resetWantedLevel) return;
                Utils.RequestScript("re_prison");
                Utils.RequestScript("re_prisonlift");
                Utils.RequestScript("am_prison");
                Utils.RequestScript("re_lossantosintl");
                Utils.RequestScript("re_armybase");
                Utils.RequestScript("restrictedareas");
                Game.MaxWantedLevel = 5;
                _resetWantedLevel = true;
                return;
            }
            _resetWantedLevel = false;
            Utils.TerminateScriptByName("re_prison");
            Utils.TerminateScriptByName("re_prisonlift");
            Utils.TerminateScriptByName("am_prison");
            Utils.TerminateScriptByName("re_lossantosintl");
            Utils.TerminateScriptByName("re_armybase");
            Utils.TerminateScriptByName("restrictedareas");
            Game.Player.WantedLevel = 0;
            Game.MaxWantedLevel = 0;
        }

        private void ResetWeather()
        {
            // Basically happens when we abort.
            Function.Call(Hash.CLEAR_WEATHER_TYPE_PERSIST);
            Function.Call(Hash.CLEAR_OVERRIDE_WEATHER);
            World.Weather = Weather.Clear;
            World.CurrentDayTime = new TimeSpan(World.CurrentDayTime.Days, 12, 0, 0);
        }

        #endregion

        #region Scene Management

        private SceneInfo DeserializeFileAsScene(string fileName)
        {
            if (fileName == "cmd_earth")
                return null;

            var newScene = XmlSerializer.Deserialize<SceneInfo>(Database.PathToScenes + "\\" + fileName);

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

                if (_currentScene != null && _currentScene != default(Scene))
                    _currentScene.Delete();

                if (PlayerPed.IsInVehicle()) PlayerPed.CurrentVehicle.Rotation = Vector3.Zero;
                else PlayerPed.Rotation = Vector3.Zero;

                _currentScene = new Scene(scene) {FileName = fileName};
                _currentScene.Start();

                Function.Call(Hash.SET_CLOCK_TIME, _currentScene.Info.Time, _currentScene.Info.TimeMinutes, 0);
                Function.Call(Hash.PAUSE_CLOCK, true);

                Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, _currentScene.Info.WeatherName);

                if (GTS.Settings.UseScenarios)
                    try
                    {
                        CreateScenariosForSceneInfo(scene);
                    }
                    catch (Exception ex)
                    {
                        Debug.Log(ex.Message + Environment.NewLine + ex.StackTrace, DebugMessageType.Error);
                    }
                _currentScene.Exited += CurrentSceneOnExited;
            }
        }

        private void CreateScenariosForSceneInfo(SceneInfo scene)
        {
            foreach (var scenarioInfo in scene.Scenarios)
            {
                var assembly = Assembly.LoadFrom(Path.Combine(Database.PathToScenarios, scenarioInfo.Dll));

                if (assembly == null)
                    continue;

                var type = assembly.GetType(scenarioInfo.Namespace);

                if (type == null || type.BaseType != typeof(Scenario))
                    continue;

                Debug.Log("Creating Scenario: " + type.Name);

                var instance = (Scenario) Activator.CreateInstance(type);

                instance.OnAwake();

                if (instance.IsScenarioComplete())
                    continue;

                Scenarios.Add(instance);
            }

            foreach (var scenario in Scenarios)
            {
                scenario.OnStart();

                scenario.Completed += ScenarioComplete;
            }
        }

        private void ScenarioComplete(Scenario scenario, bool success)
        {
            lock (_tickLock)
            {
                Scenarios.Remove(scenario);
            }
        }

        private void ClearAllEntities()
        {
            var entities = World.GetAllEntities();

            foreach (var e in entities)
            {
                if (!e.IsDead && (e is Ped ||
                                  e is Vehicle && PlayerPed.CurrentVehicle == (Vehicle) e))
                    continue;

                e.Delete();
            }
        }

        private void CurrentSceneOnExited(Scene scene, string newSceneFile, Vector3 exitRotation, Vector3 exitOffset)
        {
            lock (_tickLock)
            {
                var isActualScene = newSceneFile != "cmd_earth";

                var newScene = DeserializeFileAsScene(newSceneFile);

                if (isActualScene && newScene == null)
                {
                    OnAborted(this, new EventArgs());
                    _didAbort = false;
                    return;
                }

                if (isActualScene && newScene.SurfaceScene)

                    RaiseLandingGear();

                else if (!isActualScene)
                    // if this ISNT an actual scene check if we can start the end mission.
                    _endMissionCanStart = CanStartEndMission();

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
                    if (exitOffset != Vector3.Zero)
                        PlayerPosition = newScene.GalaxyCenter + exitOffset;
                    if (PlayerPed.IsInVehicle())
                        PlayerPed.CurrentVehicle.Rotation = exitRotation;
                    else PlayerPed.Rotation = exitRotation;
                }
                else
                {
                    Function.Call(Hash.PAUSE_CLOCK, false);
                    GiveSpawnControlToGame();
                    EnterAtmosphere();
                    PlayerPed.HasGravity = true;
                    Utils.SetGravityLevel(9.81f);
                }

                Wait(1000);
                Game.FadeScreenIn(1000);
            }
        }

        private void EnterAtmosphere()
        {
            if (PlayerPed.IsInVehicle())
            {
                var playerPedCurrentVehicle = PlayerPed.CurrentVehicle;
                playerPedCurrentVehicle.Position = GTS.Settings.EarthAtmosphereEnterPosition;
                playerPedCurrentVehicle.Rotation = Vector3.Zero;
                playerPedCurrentVehicle.Heading = 243;
                playerPedCurrentVehicle.HasGravity = true;
                playerPedCurrentVehicle.Speed = 1000;
            }
            else
            {
                PlayerPosition = GTS.Settings.EarthAtmosphereEnterPosition;
            }
        }

        private void EndActiveScenarios()
        {
            if (Scenarios == null) return;
            while (Scenarios.Count > 0)
            {
                var scen = Scenarios[0];
                scen.OnEnded(false);
                Scenarios.RemoveAt(0);
            }
        }

        private void AbortActiveScenarios()
        {
            if (Scenarios == null) return;
            while (Scenarios.Count > 0)
            {
                var scenario = Scenarios[0];
                scenario.OnAborted();
                Scenarios.RemoveAt(0);
            }
        }

        #endregion

        #region Utility

        private void RaiseLandingGear()
        {
            var vehicle = PlayerPed.CurrentVehicle;
            if (PlayerPed.IsInVehicle() && Function.Call<bool>(Hash._0x4198AB0022B15F87, vehicle.Handle))
            {
                Function.Call(Hash._0xCFC8BE9A5E1FE575, vehicle.Handle, 0);
                var landingGrearTimeout = DateTime.UtcNow + new TimeSpan(0, 0, 5);
                while (Function.Call<int>(Hash._0x9B0F3DCA3DB0F4CD, vehicle.Handle) != 0)
                {
                    if (DateTime.UtcNow > landingGrearTimeout)
                        break;
                    Yield();
                }
            }
        }

        private void GiveSpawnControlToGame()
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

// TEMP
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