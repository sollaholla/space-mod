﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using GTS.Events;
using GTS.Library;
using GTS.Scenes;
using GTS.Shuttle;
using GTS.Utility;
using GTSCommon;
using NativeUI;
using Control = GTA.Control;

namespace GTS
{
    internal class Core : Script
    {
        private const string LsReturnScene = "Earth";
        private readonly TimecycleModChanger _tcChanger;
        private bool _didAbort;
        private bool _initializedGts;
        private bool _initializedScripts;
        private UIMenu _mainMenu;
        private MapLoader _mapLoader;
        private bool _menuEnabled = true;
        private MenuPool _menuPool;
        private int _missionStatus;
        private Keys _optionsMenuKey = Keys.NumPad9;
        private bool _resetWantedLevel = true;
        private ShuttleManager _shuttleManager;
        private SandersBriefing _sandersBriefing;

        public Core()
        {
            Instance = this;
            Tick += OnTick;
            KeyUp += OnKeyUp;
            Aborted += OnAborted;

            ReadSettings();
            SaveSettings();
            CreateMainMenu();

            _tcChanger = new TimecycleModChanger();

            Debug.Log("Initialized!");
        }

        public static Core Instance { get; private set; }

        public static Ped PlayerPed => Game.Player.Character;

        public static Scene CurrentScene { get; private set; }

        public static Vector3 PlayerPosition {
            get => PlayerPed.IsInVehicle() ? PlayerPed.CurrentVehicle.Position : PlayerPed.Position;
            set {
                if (PlayerPed.IsInVehicle()) PlayerPed.CurrentVehicle.Position = value;
                else PlayerPed.Position = value;
            }
        }

        public static HeliTransport HeliTransport { get; private set; }

        protected override void Dispose(bool dispose)
        {
            Debug.ClearLog();
            Debug.Log("Thread disposed...");
            if (!_didAbort) OnAborted(null, new EventArgs());
            base.Dispose(dispose);

        }

        private void OnTick(object sender, EventArgs eventArgs)
        {
            try
            {
                LoadGts();
                ProcessMenus();
                CheckSceneStatus();
                DisableWantedStars();
                _sandersBriefing?.Update();
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message + Environment.NewLine + ex.StackTrace, DebugMessageType.Error);
                throw new Exception("Core script threw an exception...");
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.L)
            {
                World.GetActiveBlips().ToList().ForEach(x =>
                {
                    if (x.Color == BlipColor.Red)
                        x.Remove();
                });
            }

            if (_menuPool?.IsAnyMenuOpen() ?? false)
                return;

            if (e.KeyCode != _optionsMenuKey)
                return;

            _mainMenu.Visible = !_mainMenu.Visible;
        }

        internal void OnAborted(object sender, EventArgs eventArgs)
        {
            Reset();
            HeliTransport?.Delete();
            _shuttleManager?.Abort();
            _tcChanger?.Stop();
            _mapLoader?.RemoveMaps();
            _didAbort = true;
            _sandersBriefing.OnAborted();
        }

        private static void Reset()
        {
            if (!PlayerPed.IsDead && (Game.IsScreenFadedOut || Game.IsScreenFadingOut))
                Game.FadeScreenIn(0);
            PlayerPed.Task.ClearAll();
            PlayerPed.HasGravity = true;
            PlayerPed.FreezePosition = false;
            PlayerPed.CanRagdoll = true;
            PlayerPed.IsInvincible = false;
            Game.TimeScale = 1.0f;
            World.RenderingCamera = null;
            GtsLibNet.SetGravityLevel(9.81f);
            GtsLib.EndCredits();
            GtsLib.DisableLoadingScreenHandler(false);
            Function.Call(Hash.CLEAR_TIMECYCLE_MODIFIER);
            Effects.Stop();
            CurrentScene?.Delete(true);
            if (CurrentScene != null)
            {
                GiveSpawnControlToGame();
                if (!PlayerPed.IsDead)
                    PlayerPosition = Database.TrevorAirport;
                ResetWeather();
            }
            else
            {
                GtsLibNet.ToggleAllIpls(false);
            }
            CurrentScene = null;
        }

        private void ProcessMenus()
        {
            if (_menuPool == null) return;
            _menuPool.ProcessMenus();
            if (!_menuPool.IsAnyMenuOpen()) return;
            UI.HideHudComponentThisFrame(HudComponent.HelpText);
        }

        private void ReadSettings()
        {
            _optionsMenuKey = Settings.GetValue("core", "options_menu_key", _optionsMenuKey);
            _menuEnabled = Settings.GetValue("core", "menu_enabled", _menuEnabled);
            _missionStatus = Settings.GetValue("core", "mission_status", _missionStatus);
            GtsSettings.EnterOrbitHeight =
                Settings.GetValue("core", "enter_orbit_height", GtsSettings.EnterOrbitHeight);
            GtsSettings.DefaultOrbitScene =
                Settings.GetValue("core", "default_orbit_scene", GtsSettings.DefaultOrbitScene);
            GtsSettings.DefaultOrbitOffset = VectorParse.Read(Settings.GetValue("core", "default_orbit_offset"),
                GtsSettings.DefaultOrbitOffset);
            GtsSettings.DefaultOrbitRotation = VectorParse.Read(
                Settings.GetValue("core", "default_orbit_rotation"),
                GtsSettings.DefaultOrbitRotation);
            GtsSettings.UseSpaceWalk = Settings.GetValue("core", "use_spacewalk", GtsSettings.UseSpaceWalk);
            GtsSettings.ShowCustomGui =
                Settings.GetValue("core", "show_custom_Gui", GtsSettings.ShowCustomGui);
            GtsSettings.UseScenarios = Settings.GetValue("core", "use_scenarios", GtsSettings.UseScenarios);
            GtsSettings.MoonJump = Settings.GetValue("core", "low_gravity_jumping", GtsSettings.MoonJump);
            GtsSettings.MouseControlFlySensitivity = Settings.GetValue("core", "mouse_control_fly_sensitivity",
                GtsSettings.MouseControlFlySensitivity);
            GtsSettings.VehicleFlySpeed =
                Settings.GetValue("core", "vehicle_fly_speed", GtsSettings.VehicleFlySpeed);
            GtsSettings.VehicleReentrySpeed =
                Settings.GetValue("core", "vehicle_reentry_speed", GtsSettings.VehicleReentrySpeed);
            GtsSettings.EarthAtmosphereEnterPosition = VectorParse.Read(
                Settings.GetValue("core", "enter_atmos_pos"),
                GtsSettings.EarthAtmosphereEnterPosition);
            GtsSettings.EarthAtmosphereEnterRotation = VectorParse.Read(
                Settings.GetValue("core", "earth_atmos_rot"),
                GtsSettings.EarthAtmosphereEnterRotation);
            GtsSettings.AlwaysUseSound =
                Settings.GetValue("core", "always_use_sound", GtsSettings.AlwaysUseSound);
            GtsSettings.DisableWantedLevel =
                Settings.GetValue("core", "disable_wanted_level", GtsSettings.DisableWantedLevel);
            GtsSettings.ShutStage1Height =
                Settings.GetValue("shuttle", "shut_stage1_height", GtsSettings.ShutStage1Height);
            GtsSettings.ShutStage2Height =
                Settings.GetValue("shuttle", "shut_stage2_height", GtsSettings.ShutStage2Height);
            GtsSettings.ShuttleNewtonsOfForce =
                Settings.GetValue("shuttle", "shut_newtons_of_force", GtsSettings.ShuttleNewtonsOfForce);
            GtsSettings.ShuttleThrustInterpolation = Settings.GetValue("shuttle", "shut_thrust_interpolation",
                GtsSettings.ShuttleThrustInterpolation);
            GtsSettings.ShuttleGimbalFront =
                Settings.GetValue("shuttle", "shut_front_gimbal", GtsSettings.ShuttleGimbalFront);
            GtsSettings.ScenesFolder = Settings.GetValue("paths", "scenes_folder", GtsSettings.ScenesFolder);
            GtsSettings.InteriorsFolder =
                Settings.GetValue("paths", "interiors_folder", GtsSettings.InteriorsFolder);
            GtsSettings.ScenariosFolder =
                Settings.GetValue("paths", "scenarios_folder", GtsSettings.ScenariosFolder);
            GtsSettings.AudioFolder = Settings.GetValue("paths", "audio_folder", GtsSettings.AudioFolder);
            GtsSettings.LogPath = Settings.GetValue("paths", "log_file_path", GtsSettings.LogPath);
            GtsSettings.SpaceVehiclesPath =
                Settings.GetValue("paths", "space_vehicles_path", GtsSettings.SpaceVehiclesPath);
            GtsSettings.TimecycleModifierPath = Settings.GetValue("paths", "timecycle_modifier_list_path",
                GtsSettings.TimecycleModifierPath);
        }

        private void SaveSettings()
        {
            Settings.SetValue("core", "options_menu_key", _optionsMenuKey);
            Settings.SetValue("core", "menu_enabled", _menuEnabled);
            Settings.SetValue("core", "mission_status", _missionStatus);
            Settings.SetValue("core", "enter_orbit_height", GtsSettings.EnterOrbitHeight);
            Settings.SetValue("core", "default_orbit_scene", GtsSettings.DefaultOrbitScene);
            Settings.SetValue("core", "default_orbit_offset", GtsSettings.DefaultOrbitOffset);
            Settings.SetValue("core", "default_orbit_rotation", GtsSettings.DefaultOrbitRotation);
            Settings.SetValue("core", "use_spacewalk", GtsSettings.UseSpaceWalk);
            Settings.SetValue("core", "show_custom_Gui", GtsSettings.ShowCustomGui);
            Settings.SetValue("core", "use_scenarios", GtsSettings.UseScenarios);
            Settings.SetValue("core", "low_gravity_jumping", GtsSettings.MoonJump);
            Settings.SetValue("core", "mouse_control_fly_sensitivity", GtsSettings.MouseControlFlySensitivity);
            Settings.SetValue("core", "vehicle_fly_speed", GtsSettings.VehicleFlySpeed);
            Settings.SetValue("core", "vehicle_reentry_speed", GtsSettings.VehicleReentrySpeed);
            Settings.SetValue("core", "enter_atmos_pos", GtsSettings.EarthAtmosphereEnterPosition);
            Settings.SetValue("core", "earth_atmos_rot", GtsSettings.EarthAtmosphereEnterRotation);
            Settings.SetValue("core", "always_use_sound", GtsSettings.AlwaysUseSound);
            Settings.SetValue("core", "disable_wanted_level", GtsSettings.DisableWantedLevel);
            Settings.SetValue("shuttle", "shut_stage1_height", GtsSettings.ShutStage1Height);
            Settings.SetValue("shuttle", "shut_stage2_height", GtsSettings.ShutStage2Height);
            Settings.SetValue("shuttle", "shut_stage1_height", GtsSettings.ShutStage1Height);
            Settings.SetValue("shuttle", "shut_stage2_height", GtsSettings.ShutStage2Height);
            Settings.SetValue("shuttle", "shut_newtons_of_force", GtsSettings.ShuttleNewtonsOfForce);
            Settings.SetValue("shuttle", "shut_thrust_interpolation", GtsSettings.ShuttleThrustInterpolation);
            Settings.SetValue("shuttle", "shut_front_gimbal", GtsSettings.ShuttleGimbalFront);
            Settings.SetValue("paths", "scenes_folder", GtsSettings.ScenesFolder);
            Settings.SetValue("paths", "interiors_folder", GtsSettings.InteriorsFolder);
            Settings.SetValue("paths", "scenarios_folder", GtsSettings.ScenariosFolder);
            Settings.SetValue("paths", "audio_folder", GtsSettings.AudioFolder);
            Settings.SetValue("paths", "log_file_path", GtsSettings.LogPath);
            Settings.SetValue("paths", "space_vehicles_path", GtsSettings.SpaceVehiclesPath);
            Settings.SetValue("paths", "timecycle_modifier_list_path", GtsSettings.TimecycleModifierPath);
            Settings.Save();
        }

        private void CheckSceneStatus()
        {
            if (CurrentScene != null) SceneNotNull();
            else SceneNull();
        }

        private void SceneNull()
        {
            DoEarthUpdate();
            DoWorkingElevator();
        }

        private void SceneNotNull()
        {
            if (CurrentScene.Info != null)
                DoSceneUpdate();
        }

        private void CreateMainMenu()
        {
            if (!_menuEnabled)
                return;

            _menuPool = new MenuPool();
            _mainMenu = new UIMenu("GTS Options", "Core");

            #region Scenes

            var scenesMenu = _menuPool.AddSubMenu(_mainMenu, "Scenes");
            var filePaths = Directory.GetFiles(GtsSettings.ScenesFolder).Where(file => file.EndsWith(".space"))
                .ToArray();
            foreach (var path in filePaths)
            {
                var fileName = Path.GetFileName(path);
                var menuItem = new UIMenuItem(fileName);
                menuItem.Activated += (sender, item) =>
                {
                    var newScene = DeserializeSceneInfoFile(fileName);
                    SetCurrentScene(newScene, fileName);
                    UI.Notify($"{Database.NotifyHeader}Loaded: {fileName}");
                    _menuPool.CloseAllMenus();
                };
                scenesMenu.AddItem(menuItem);
            }
            var earthItem = new UIMenuItem("Return To Earth");
            earthItem.SetLeftBadge(UIMenuItem.BadgeStyle.Heart);
            earthItem.Activated += (a1, a2) =>
            {
                if (CurrentScene != null)
                    Reset();
            };
            scenesMenu.AddItem(earthItem);

            #endregion

            #region Settings

            var settingsMenu = _menuPool.AddSubMenu(_mainMenu, "Settings");

            #region User Interface Settings

            var userInterfaceMenu = _menuPool.AddSubMenu(settingsMenu, "Interface");
            var showCustomUiCheckbox = new UIMenuCheckboxItem("Show Custom UI", GtsSettings.ShowCustomGui);
            showCustomUiCheckbox.CheckboxEvent += (sender, check) => { GtsSettings.ShowCustomGui = check; };
            userInterfaceMenu.AddItem(showCustomUiCheckbox);

            #endregion

            #region Vehicles Settings

            int flyIndex;
            List<dynamic> dynamicList;

            var vehicleSettingsMenu = _menuPool.AddSubMenu(settingsMenu, "Vehicles");
            var vehicleSpeedList = new UIMenuListItem("Vehicle Speed",
                dynamicList = Enumerable.Range(1, 20).Select(i => (dynamic)(i * 5)).ToList(),
                (flyIndex = dynamicList.IndexOf(GtsSettings.VehicleFlySpeed)) == -1 ? 0 : flyIndex);
            vehicleSpeedList.OnListChanged += (sender, index) =>
            {
                GtsSettings.VehicleFlySpeed = (int)sender.IndexToItem(index);
            };

            var flySensitivity = (int)GtsSettings.MouseControlFlySensitivity;
            var vehicleSensitivityList = new UIMenuListItem("Mouse Control Sensitivity",
                Enumerable.Range(0, flySensitivity > 15 ? flySensitivity + 5 : 15)
                    .Select(i => (dynamic)i).ToList(), flySensitivity);
            vehicleSensitivityList.OnListChanged += (sender, index) =>
            {
                GtsSettings.MouseControlFlySensitivity = (float)sender.IndexToItem(index);
            };

            vehicleSettingsMenu.AddItem(vehicleSpeedList);
            vehicleSettingsMenu.AddItem(vehicleSensitivityList);

            #endregion

            #region Scene Settings

            var sceneSettingsMenu = _menuPool.AddSubMenu(settingsMenu, "Scenes");
            var useScenariosCheckbox = new UIMenuCheckboxItem("Use Scenarios", GtsSettings.UseScenarios);
            useScenariosCheckbox.CheckboxEvent += (sender, check) => { GtsSettings.UseScenarios = check; };
            var debugTriggerCheckbox = new UIMenuCheckboxItem("Debug Triggers", GtsSettings.DebugTriggers);
            debugTriggerCheckbox.CheckboxEvent += (sender, check) => { GtsSettings.DebugTriggers = check; };
            var soundCheckbox = new UIMenuCheckboxItem("Always Use Sound", GtsSettings.AlwaysUseSound);
            soundCheckbox.CheckboxEvent += (sender, check) => { GtsSettings.AlwaysUseSound = check; };

            sceneSettingsMenu.AddItem(useScenariosCheckbox);
            sceneSettingsMenu.AddItem(debugTriggerCheckbox);
            sceneSettingsMenu.AddItem(soundCheckbox);

            #endregion

            #region Player Settings

            var playerSettingsMenu = _menuPool.AddSubMenu(settingsMenu, "Player");
            var useFloatingCheckbox = new UIMenuCheckboxItem("Use SpaceWalk", GtsSettings.UseSpaceWalk);
            useFloatingCheckbox.CheckboxEvent += (sender, check) => { GtsSettings.UseSpaceWalk = check; };

            playerSettingsMenu.AddItem(useFloatingCheckbox);

            #endregion

            var respawnShuttleItem = new UIMenuItem("Respawn Shuttle", "Respawns the nasa shuttle on the gantry.");
            respawnShuttleItem.Activated += (sender, item) => { _shuttleManager?.CreateShuttle(); };
            settingsMenu.AddItem(respawnShuttleItem);

            var saveSettingsItem = new UIMenuItem("Save Settings");
            saveSettingsItem.SetLeftBadge(UIMenuItem.BadgeStyle.Star);
            saveSettingsItem.Activated += (sender, item) =>
            {
                SaveSettings();
                UI.Notify(Database.NotifyHeader + "Settings ~h~saved~s~!", true);
            };
            settingsMenu.AddItem(saveSettingsItem);

            var disableWantedLevelCheckbox =
                new UIMenuCheckboxItem("Disable Wanted Level", GtsSettings.DisableWantedLevel);
            disableWantedLevelCheckbox.CheckboxEvent += (a, b) => { GtsSettings.DisableWantedLevel = b; };
            settingsMenu.AddItem(disableWantedLevelCheckbox);

            #endregion

            #region Debug

            var debugButton = new UIMenuItem("Debug Player", "Log the player's position rotation and heading.");
            debugButton.SetLeftBadge(UIMenuItem.BadgeStyle.Alert);
            debugButton.Activated += (sender, item) =>
            {
                Debug.LogEntityData(PlayerPed.IsInVehicle() ? PlayerPed.CurrentVehicle : (Entity)PlayerPed);
            };

            _mainMenu.AddItem(debugButton);

            #endregion

            _menuPool.Add(_mainMenu);
            _menuPool.RefreshIndex();
            _menuPool.SetBannerType(new Sprite("", "", new Point(), new Size(), 0,
                ColorTranslator.FromHtml("#8000ff")));
        }

        private void LoadGts()
        {
            while (Game.IsLoading || Game.IsScreenFadedOut || Game.IsScreenFadingIn)
                Yield();

            if (!_initializedGts)
            {
                _menuPool.CloseAllMenus();

                GtsLib.DisableLoadingScreenHandler(true);
                Function.Call(Hash._0x0888C3502DBBEEF5);
                GtsLib.DisableLoadingScreenHandler(false);

                var loadScaleform = LoadScaleformDrawer.Instance.Create("Loading GTS...");
                loadScaleform.Draw = true;

                if (_shuttleManager == null)
                {
                    _shuttleManager = new ShuttleManager();
                    _shuttleManager.CreateShuttle();
                }
                if (_mapLoader == null)
                {
                    _mapLoader = new MapLoader();
                    _mapLoader.LoadMaps();
                }
                if (HeliTransport == null)
                {
                    HeliTransport = new HeliTransport();
                    HeliTransport.Load();
                }

                _sandersBriefing = new SandersBriefing();
                if (!_sandersBriefing.IsScenarioComplete())
                {
                    _sandersBriefing.Start();
                    _sandersBriefing.Completed += (scenario, success) =>
                    {
                        _sandersBriefing.EndScenario(true);
                    };
                }
                else _sandersBriefing = null;

                LoadScaleformDrawer.Instance.RemoveLoadScaleform(loadScaleform);
                _initializedGts = true;
            }

            _shuttleManager.Update();
        }

        private void DoEarthUpdate()
        {
            StartScripts();
            HeliTransport?.Update();

            var height = PlayerPed.HeightAboveGround;
            if (!(height > GtsSettings.EnterOrbitHeight)) return;

            var scene = XmlSerializer.Deserialize<SceneInfo>(Path.Combine(GtsSettings.ScenesFolder,
                GtsSettings.DefaultOrbitScene));
            SetCurrentScene(scene, GtsSettings.DefaultOrbitScene,
                scene.GalaxyCenter + GtsSettings.DefaultOrbitOffset,
                GtsSettings.DefaultOrbitRotation);
        }

        private void DoSceneUpdate()
        {
            if (PlayerPed.CurrentVehicle != null)
                PlayerPed.CurrentVehicle.HasGravity = CurrentScene.Info.UseGravity;
            else PlayerPed.HasGravity = CurrentScene.Info.UseGravity;
            CurrentScene.Update();
            StopScripts();
        }

        private void DisableWantedStars()
        {
            if (!GtsSettings.DisableWantedLevel)
            {
                StartWantedLevelScripts();
                return;
            }
            StopWantedLevelScripts();
        }

        private static void ResetWeather()
        {
            // Basically happens when we abort.
            Function.Call(Hash.CLEAR_WEATHER_TYPE_PERSIST);
            Function.Call(Hash.CLEAR_OVERRIDE_WEATHER);
            World.Weather = Weather.Clear;
            World.CurrentDayTime = new TimeSpan(World.CurrentDayTime.Days, 12, 0, 0);
        }

        private static SceneInfo DeserializeSceneInfoFile(string fileName)
        {
            if (fileName == LsReturnScene) return null;
            var newScene = XmlSerializer.Deserialize<SceneInfo>(GtsSettings.ScenesFolder + "\\" + fileName);
            if (newScene != null) return newScene;
            UI.Notify(Database.NotifyHeader + "Scene file " + fileName + " couldn't be read, or doesn't exist.");
            return null;
        }

        private void SetCurrentScene(SceneInfo sceneInfo, string fileName = "", Vector3 position = default(Vector3),
            Vector3 rotation = default(Vector3))
        {
            PlayerPed.IsInvincible = true;
            Game.FadeScreenOut(100);
            Wait(100);
            CreateScene(sceneInfo, fileName);
            if (position != default(Vector3)) PlayerPosition = position;
            if (rotation != default(Vector3) && PlayerPed.IsInVehicle()) PlayerPed.CurrentVehicle.Rotation = rotation;
            Game.FadeScreenIn(100);
            PlayerPed.IsInvincible = false;
        }

        private void CreateScene(SceneInfo scene, string fileName = "")
        {
            CurrentScene?.Delete();
            ClearAllEntities(PlayerPosition);
            if (PlayerPed.IsInVehicle()) PlayerPed.CurrentVehicle.Rotation = Vector3.Zero;
            else PlayerPed.Rotation = Vector3.Zero;
            CurrentScene = new Scene(scene) { FileName = fileName };
            CurrentScene.Start();
            CurrentScene.Exited += CurrentSceneOnExited;
        }

        private void CurrentSceneOnExited(object sender, SceneExitEventArgs sceneExitEventArgs)
        {
            var nextScene = sceneExitEventArgs.NewSceneFile;

            if (nextScene == LsReturnScene)
            {
                Scene.ResetLastSceneInfo();
                EnterAtmosphere();
                _shuttleManager?.CreateShuttle();
                return;
            }

            var sceneInfo = DeserializeSceneInfoFile(nextScene);
            var lastSceneInfo = Scene.GetLastSceneInfo();
            var offset = Vector3.Zero;
            var heading = 0f;
            if (!sceneInfo.SurfaceScene)
                if (lastSceneInfo != null)
                {
                    var orbital = sceneInfo.Orbitals.Find(x => x.Name == lastSceneInfo.OrbitalName);
                    if (orbital != null && nextScene == lastSceneInfo.Scene)
                    {
                        var dir = lastSceneInfo.DirToPlayer * lastSceneInfo.ModelDimensions;
                        offset = orbital.Position + dir;
                        foreach (var sceneInfoOrbital in sceneInfo.Orbitals)
                            sceneInfoOrbital.Position -= offset;
                        foreach (var sceneInfoBillboard in sceneInfo.Billboards)
                            sceneInfoBillboard.Position -= offset;
                        foreach (var sceneInfoSceneLink in sceneInfo.SceneLinks)
                            sceneInfoSceneLink.Position -= offset;
                        if (PlayerPed.IsInVehicle())
                            heading = lastSceneInfo.DirToPlayer.ToHeading();
                    }
                    Scene.ResetLastSceneInfo();
                }

            SetCurrentScene(sceneInfo, nextScene);
            CurrentScene.SimulatedPosition = offset;
            if (PlayerPed.IsInVehicle() && Math.Abs(heading) > 0.00001)
                PlayerPed.CurrentVehicle.Heading = heading;
        }

        private static void EnterAtmosphere()
        {
            Game.FadeScreenOut(100);
            Wait(100);
            CurrentScene?.Delete();
            CurrentScene = null;
            ResetWeather();
            if (PlayerPed.IsInVehicle())
            {
                var playerPedCurrentVehicle = PlayerPed.CurrentVehicle;
                playerPedCurrentVehicle.HasGravity = true;
                playerPedCurrentVehicle.Position = GtsSettings.EarthAtmosphereEnterPosition;
                playerPedCurrentVehicle.Rotation = GtsSettings.EarthAtmosphereEnterRotation;
                playerPedCurrentVehicle.Speed = GtsSettings.VehicleReentrySpeed;
            }
            else
            {
                PlayerPed.Position = GtsSettings.EarthAtmosphereEnterPosition;
            }
            Game.FadeScreenIn(100);
        }

        private static void ClearAllEntities(Vector3 position, float radius = 10000)
        {
            Function.Call(Hash.CLEAR_AREA, position.X, position.Y, position.Z, radius, false, false, false, false);
        }

        private static void GiveSpawnControlToGame()
        {
            if (Game.Globals[4].GetInt() == 0) return;
            Game.Globals[4].SetInt(0);
            Function.Call(Hash._DISABLE_AUTOMATIC_RESPAWN, false);
            Function.Call(Hash.SET_FADE_IN_AFTER_DEATH_ARREST, true);
            Function.Call(Hash.SET_FADE_OUT_AFTER_ARREST, true);
            Function.Call(Hash.SET_FADE_OUT_AFTER_DEATH, true);
            Function.Call(Hash.IGNORE_NEXT_RESTART, false);
        }

        private void StopScripts()
        {
            if (!_initializedScripts) return;
            GtsLibNet.TerminateScript("mission_triggerer_a");
            GtsLibNet.TerminateScript("mission_triggerer_b");
            GtsLibNet.TerminateScript("mission_triggerer_c");
            GtsLibNet.TerminateScript("mission_triggerer_d");
            GtsLibNet.TerminateScript("blip_controller");
            _initializedScripts = false;
        }

        private void StartScripts()
        {
            if (_initializedScripts) return;
            GtsLibNet.StartScript("mission_triggerer_a", GtsLib.GetScriptStackSize("mission_triggerer_a"));
            GtsLibNet.StartScript("mission_triggerer_b", GtsLib.GetScriptStackSize("mission_triggerer_b"));
            GtsLibNet.StartScript("mission_triggerer_c", GtsLib.GetScriptStackSize("mission_triggerer_c"));
            GtsLibNet.StartScript("mission_triggerer_d", GtsLib.GetScriptStackSize("mission_triggerer_d"));
            GtsLibNet.StartScript("blip_controller", GtsLib.GetScriptStackSize("blip_controller"));
            _initializedScripts = true;
        }

        private void StartWantedLevelScripts()
        {
            if (_resetWantedLevel) return;
            GtsLibNet.StartScript("restrictedareas", GtsLib.GetScriptStackSize("restrictedareas"));
            GtsLibNet.StartScript("re_lossantosintl", GtsLib.GetScriptStackSize("re_lossantosintl"));
            GtsLibNet.StartScript("re_prison", GtsLib.GetScriptStackSize("re_prison"));
            GtsLibNet.StartScript("re_prisonvanbreak", GtsLib.GetScriptStackSize("re_prisonvanbreak"));
            GtsLibNet.StartScript("re_armybase", GtsLib.GetScriptStackSize("re_armybase"));
            GtsLibNet.StartScript("am_armybase", GtsLib.GetScriptStackSize("am_armybase"));
            GtsLibNet.StartScript("building_controller", GtsLib.GetScriptStackSize("building_controller"));
            _resetWantedLevel = true;
        }

        private void StopWantedLevelScripts()
        {
            if (!_resetWantedLevel) return;
            GtsLibNet.TerminateScript("restrictedareas");
            GtsLibNet.TerminateScript("re_lossantosintl");
            GtsLibNet.TerminateScript("re_prison");
            GtsLibNet.TerminateScript("re_prisonvanbreak");
            GtsLibNet.TerminateScript("re_armybase");
            GtsLibNet.TerminateScript("am_armybase");
            GtsLibNet.TerminateScript("building_controller");
            Game.Player.WantedLevel = 0;
            Game.MaxWantedLevel = 0;
            _resetWantedLevel = false;
        }

        private static void DoWorkingElevator()
        {
            var start = new Vector3(-6542.53f, -1394.774f, 87.49376f);
            var end = new Vector3(-6542.53f, -1394.774f, 27.40076f);
            const float startHeading = 105.8469f;
            const float endHeading = 105.8555f;

            var distStart = PlayerPed.Position.DistanceToSquared(start);
            var distEnd = PlayerPed.Position.DistanceToSquared(end);
            const float triggerDist = 0.4f;

            World.DrawMarker(MarkerType.VerticalCylinder, start, Vector3.RelativeFront, Vector3.Zero,
                new Vector3(triggerDist, triggerDist, triggerDist), Color.Gold);
            World.DrawMarker(MarkerType.VerticalCylinder, end, Vector3.RelativeFront, Vector3.Zero,
                new Vector3(triggerDist, triggerDist, triggerDist), Color.Gold);

            Camera cam;

            if (distStart < 1.3f)
            {
                GtsLibNet.DisplayHelpTextWithGxt(GtsLabels.INPUT_CONTEXT_GENERIC);
                if (!Game.IsControlJustPressed(2, Control.Context)) return;
                PlayerPed.Task.StandStill(-1);
                PlayerPed.Task.AchieveHeading(startHeading, -1);
                cam = World.CreateCamera(PlayerPosition + Vector3.RelativeLeft + PlayerPed.UpVector, Vector3.Zero, 60);
                cam.PointAt(PlayerPed, PlayerPed.GetBoneIndex(Bone.SKEL_Head));
                World.RenderingCamera = cam;
                Wait(2000);
                cam.Destroy();
                World.RenderingCamera = null;
                Game.FadeScreenOut(1);
                PlayerPed.Position = end;
                PlayerPed.Heading = endHeading;
                Game.FadeScreenIn(1000);
            }
            else if (distEnd < 1.3f)
            {
                GtsLibNet.DisplayHelpTextWithGxt(GtsLabels.INPUT_CONTEXT_GENERIC);
                if (!Game.IsControlJustPressed(2, Control.Context)) return;
                PlayerPed.Task.StandStill(-1);
                PlayerPed.Task.AchieveHeading(endHeading, -1);
                cam = World.CreateCamera(PlayerPosition + Vector3.RelativeLeft + PlayerPed.UpVector, Vector3.Zero, 60);
                cam.PointAt(PlayerPed, PlayerPed.GetBoneIndex(Bone.SKEL_Head));
                World.RenderingCamera = cam;
                Wait(2000);
                cam.Destroy();
                World.RenderingCamera = null;
                Game.FadeScreenOut(1);
                PlayerPed.Position = start;
                PlayerPed.Heading = startHeading;
                Game.FadeScreenIn(1000);
            }
        }
    }
}