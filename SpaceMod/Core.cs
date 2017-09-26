using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using GTS.Extensions;
using GTS.Library;
using GTS.Scenes;
using GTS.Shuttle;
using GTSCommon;
using NativeUI;
using Control = GTA.Control;
//using GTS.Missions;

namespace GTS
{
    internal class Core : Script
    {
        private const string LSReturnScene = "Earth";
        private readonly TimecycleModChanger _tcChanger = new TimecycleModChanger();
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

        //private IntroMission _introMission;
        private ShuttleManager _shuttleManager;

        public Core()
        {
            Instance = this;
            KeyUp += OnKeyUp;
            Tick += OnTick;
            Aborted += OnAborted;

            ReadSettings();
            SaveSettings();
            CreateMainMenu();

            Debug.Log("Initialized!");
        }

        public static Core Instance { get; private set; }

        public static Ped PlayerPed => Game.Player.Character;

        public static Scene CurrentScene { get; private set; }

        public static Vector3 PlayerPosition
        {
            get => PlayerPed.IsInVehicle() ? PlayerPed.CurrentVehicle.Position : PlayerPed.Position;
            set
            {
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
                CreateMaps();
                ProcessMenus();
                CheckSceneStatus();
                RunInternalMissions();
                DisableWantedStars();
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message, DebugMessageType.Error);
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (_menuPool?.IsAnyMenuOpen() ?? false)
                return;

            if (e.KeyCode != _optionsMenuKey)
                return;

            _mainMenu.Visible = !_mainMenu.Visible;
        }

        internal void OnAborted(object sender, EventArgs eventArgs)
        {
            Reset();
            //_introMission?.OnAborted();
            _shuttleManager?.Abort();
            _tcChanger?.Stop();
            _mapLoader?.RemoveMaps();
            HeliTransport?.Delete();
            _didAbort = true;
        }

        private static void Reset()
        {
            if (!PlayerPed.IsDead && (Game.IsScreenFadedOut || Game.IsScreenFadingOut))
                Game.FadeScreenIn(0);
            PlayerPed.Task.ClearAll();
            PlayerPed.HasGravity = true;
            PlayerPed.FreezePosition = false;
            PlayerPed.CanRagdoll = true;
            Game.TimeScale = 1.0f;
            World.RenderingCamera = null;
            GtsLibNet.SetGravityLevel(9.81f);
            GtsLib.EndCredits();
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
                GtsLibNet.RemoveAllIplsRegardless(false);
            }
            CurrentScene = null;
            //Function.Call(Hash.DECOR_SET_INT, PlayerPed, "fileindex", 1);
            //Function.Call(Hash.DECOR_SET_BOOL, PlayerPed, "reload", true);
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
            GTS.Settings.EnterOrbitHeight =
                Settings.GetValue("core", "enter_orbit_height", GTS.Settings.EnterOrbitHeight);
            GTS.Settings.DefaultScene = Settings.GetValue("core", "default_orbit_scene", GTS.Settings.DefaultScene);
            GTS.Settings.DefaultScenePosition = ParseVector3.Read(Settings.GetValue("core", "default_orbit_offset"),
                GTS.Settings.DefaultScenePosition);
            GTS.Settings.DefaultSceneRotation = ParseVector3.Read(Settings.GetValue("core", "default_orbit_rotation"),
                GTS.Settings.DefaultSceneRotation);
            GTS.Settings.UseSpaceWalk = Settings.GetValue("core", "use_spacewalk", GTS.Settings.UseSpaceWalk);
            GTS.Settings.ShowCustomGui = Settings.GetValue("core", "show_custom_Gui", GTS.Settings.ShowCustomGui);
            GTS.Settings.UseScenarios = Settings.GetValue("core", "use_scenarios", GTS.Settings.UseScenarios);
            GTS.Settings.MoonJump = Settings.GetValue("core", "low_gravity_jumping", GTS.Settings.MoonJump);
            GTS.Settings.MouseControlFlySensitivity = Settings.GetValue("core", "mouse_control_fly_sensitivity",
                GTS.Settings.MouseControlFlySensitivity);
            GTS.Settings.VehicleFlySpeed = Settings.GetValue("core", "vehicle_fly_speed", GTS.Settings.VehicleFlySpeed);
            GTS.Settings.EarthAtmosphereEnterPosition = ParseVector3.Read(Settings.GetValue("core", "enter_atmos_pos"),
                GTS.Settings.EarthAtmosphereEnterPosition);
            GTS.Settings.EarthAtmosphereEnterRotation = ParseVector3.Read(Settings.GetValue("core", "earth_atmos_rot"),
                GTS.Settings.EarthAtmosphereEnterRotation);
            GTS.Settings.AlwaysUseSound = Settings.GetValue("core", "always_use_sound", GTS.Settings.AlwaysUseSound);
            GTS.Settings.DisableWantedLevel =
                Settings.GetValue("core", "disable_wanted_level", GTS.Settings.DisableWantedLevel);
        }

        private void SaveSettings()
        {
            Settings.SetValue("core", "options_menu_key", _optionsMenuKey);
            Settings.SetValue("core", "menu_enabled", _menuEnabled);
            Settings.SetValue("core", "mission_status", _missionStatus);
            Settings.SetValue("core", "enter_orbit_height", GTS.Settings.EnterOrbitHeight);
            Settings.SetValue("core", "default_orbit_scene", GTS.Settings.DefaultScene);
            Settings.SetValue("core", "default_orbit_offset", GTS.Settings.DefaultScenePosition);
            Settings.SetValue("core", "default_orbit_rotation", GTS.Settings.DefaultSceneRotation);
            Settings.SetValue("core", "use_spacewalk", GTS.Settings.UseSpaceWalk);
            Settings.SetValue("core", "show_custom_Gui", GTS.Settings.ShowCustomGui);
            Settings.SetValue("core", "use_scenarios", GTS.Settings.UseScenarios);
            Settings.SetValue("core", "low_gravity_jumping", GTS.Settings.MoonJump);
            Settings.SetValue("core", "mouse_control_fly_sensitivity", GTS.Settings.MouseControlFlySensitivity);
            Settings.SetValue("core", "vehicle_fly_speed", GTS.Settings.VehicleFlySpeed);
            Settings.SetValue("core", "enter_atmos_pos", GTS.Settings.EarthAtmosphereEnterPosition);
            Settings.SetValue("core", "earth_atmos_rot", GTS.Settings.EarthAtmosphereEnterRotation);
            Settings.SetValue("core", "always_use_sound", GTS.Settings.AlwaysUseSound);
            Settings.SetValue("core", "disable_wanted_level", GTS.Settings.DisableWantedLevel);
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
                GTS.Settings.VehicleFlySpeed = (int) sender.IndexToItem(index);
            };

            var flySensitivity = (int) GTS.Settings.MouseControlFlySensitivity;
            var vehicleSensitivityList = new UIMenuListItem("Mouse Control Sensitivity",
                Enumerable.Range(0, flySensitivity > 15 ? flySensitivity + 5 : 15)
                    .Select(i => (dynamic) i).ToList(), flySensitivity);
            vehicleSensitivityList.OnListChanged += (sender, index) =>
            {
                GTS.Settings.MouseControlFlySensitivity = (float) sender.IndexToItem(index);
            };

            vehicleSettingsMenu.AddItem(vehicleSpeedList);
            vehicleSettingsMenu.AddItem(vehicleSensitivityList);

            #endregion

            #region Scene Settings

            var sceneSettingsMenu = _menuPool.AddSubMenu(settingsMenu, "Scenes");
            var useScenariosCheckbox = new UIMenuCheckboxItem("Use Scenarios", GTS.Settings.UseScenarios);
            useScenariosCheckbox.CheckboxEvent += (sender, check) => { GTS.Settings.UseScenarios = check; };
            var debugTriggerCheckbox = new UIMenuCheckboxItem("Debug Triggers", GTS.Settings.DebugTriggers);
            debugTriggerCheckbox.CheckboxEvent += (sender, check) => { GTS.Settings.DebugTriggers = check; };
            var soundCheckbox = new UIMenuCheckboxItem("Always Use Sound", GTS.Settings.AlwaysUseSound);
            soundCheckbox.CheckboxEvent += (sender, check) => { GTS.Settings.AlwaysUseSound = check; };

            sceneSettingsMenu.AddItem(useScenariosCheckbox);
            sceneSettingsMenu.AddItem(debugTriggerCheckbox);
            sceneSettingsMenu.AddItem(soundCheckbox);

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

            var disableWantedLevelCheckbox =
                new UIMenuCheckboxItem("Disable Wanted Level", GTS.Settings.DisableWantedLevel);
            disableWantedLevelCheckbox.CheckboxEvent += (a, b) => { GTS.Settings.DisableWantedLevel = b; };
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

        private void CreateMaps()
        {
            while (Game.IsLoading || Game.IsScreenFadedOut || Game.IsScreenFadingIn)
                Yield();

            if (!_initializedGts)
            {
                _menuPool.CloseAllMenus();

                var loadScaleform = LoadScaleformDrawer.Instance.Create("Loading GTS...");
                loadScaleform.Draw = true;

                if (_shuttleManager == null)
                {
                    _shuttleManager = new ShuttleManager(GTS.Settings.EnterOrbitHeight);
                    //if (_missionStatus > 0)
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
                LoadScaleformDrawer.Instance.RemoveLoadScaleform(loadScaleform);
                _initializedGts = true;
            }

            _shuttleManager.Update();
        }

        private void DoEarthUpdate()
        {
            //if (_introMission == null || !_introMission.DidStart)
            //    StartScripts();
            //else 
            StopScripts();
            HeliTransport?.Update();

            // Let's us go to space from earth.
            var height = PlayerPed.HeightAboveGround;
            if (!(height > GTS.Settings.EnterOrbitHeight)) return;

            var scene = XmlSerializer.Deserialize<SceneInfo>(Path.Combine(Database.PathToScenes,
                GTS.Settings.DefaultScene));
            SetCurrentScene(scene, GTS.Settings.DefaultScene);

            PlayerPosition += GTS.Settings.DefaultScenePosition;
            if (PlayerPed.IsInVehicle()) PlayerPed.CurrentVehicle.Rotation = GTS.Settings.DefaultSceneRotation;
            else PlayerPed.Heading = GTS.Settings.DefaultSceneRotation.Z;

            if (PlayerPed.CurrentVehicle == _shuttleManager.Shuttle) return;
            _shuttleManager.Shuttle?.CleanUp();
            _shuttleManager.Shuttle?.Delete();
        }

        private void DoSceneUpdate()
        {
            if (PlayerPed.CurrentVehicle != null)
                PlayerPed.CurrentVehicle.HasGravity = CurrentScene.Info.UseGravity;
            else PlayerPed.HasGravity = CurrentScene.Info.UseGravity;
            CurrentScene.Update();
            StopScripts();
        }

        private void RunInternalMissions()
        {
            //if (CurrentScene != null)
            //{
            //    _introMission?.OnAborted();
            //    return;
            //}

            //if (_missionStatus != 0) return;
            //if (_introMission != null) _introMission.Tick();
            //else
            //{
            //    _introMission = new IntroMission();
            //    _introMission.Start();
            //    _introMission.Completed += (scenario, success) =>
            //    {
            //        SetMissionStatus(1);
            //        //_shuttleManager.CreateShuttle();
            //        _introMission = null;
            //    };
            //}
        }

        //private void SetMissionStatus(int value)
        //{
        //    _missionStatus = value;
        //    Settings.SetValue("core", "mission_status", _missionStatus);
        //    Settings.Save();
        //}

        private void DisableWantedStars()
        {
            if (!GTS.Settings.DisableWantedLevel)
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

        private static SceneInfo DeserializeFileAsScene(string fileName)
        {
            if (fileName == LSReturnScene) return null;
            var newScene = XmlSerializer.Deserialize<SceneInfo>(Database.PathToScenes + "\\" + fileName);
            if (newScene != null) return newScene;
            UI.Notify(Database.NotifyHeader + "Scene file " + fileName + " couldn't be read, or doesn't exist.");
            return null;
        }

        private void SetCurrentScene(SceneInfo scene, string fileName = "")
        {
            PlayerPed.IsInvincible = true;
            Game.FadeScreenOut(100);
            Wait(100);
            CreateScene(scene, fileName);
            Wait(100);
            Game.FadeScreenIn(100);
            PlayerPed.IsInvincible = false;
        }

        private void CreateScene(SceneInfo scene, string fileName = "")
        {
            ClearAllEntities(PlayerPosition);
            CurrentScene?.Delete();
            if (PlayerPed.IsInVehicle()) PlayerPed.CurrentVehicle.Rotation = Vector3.Zero;
            else PlayerPed.Rotation = Vector3.Zero;
            CurrentScene = new Scene(scene) {FileName = fileName};
            CurrentScene.Start();
            CurrentScene.Exited += CurrentSceneOnExited;
        }

        private void CurrentSceneOnExited(Scene scene, string nextScene, Vector3 offset, Vector3 rotation)
        {
            if (nextScene == LSReturnScene)
            {
                EnterAtmosphere();
                return;
            }
            var sceneInfo = DeserializeFileAsScene(nextScene);
            SetCurrentScene(sceneInfo, nextScene);
            if (sceneInfo.SurfaceScene) return;
            PlayerPosition = sceneInfo.GalaxyCenter + offset;
            PlayerPed.Rotation = rotation;
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
                playerPedCurrentVehicle.Position = GTS.Settings.EarthAtmosphereEnterPosition;
                playerPedCurrentVehicle.Rotation = GTS.Settings.EarthAtmosphereEnterRotation;
                playerPedCurrentVehicle.HasGravity = true;
                playerPedCurrentVehicle.Speed = 1000;
            }
            else PlayerPosition = GTS.Settings.EarthAtmosphereEnterPosition;
            Game.FadeScreenIn(100);
        }

        private static void ClearAllEntities(Vector3 position, float radius = 10000)
        {
            Function.Call(Hash.CLEAR_AREA, position.X, position.Y, position.Z, radius, false, false, false, false);
        }

        private static void RaiseLandingGear()
        {
            var vehicle = PlayerPed.CurrentVehicle;
            if (!PlayerPed.IsInVehicle() || !Function.Call<bool>(Hash._0x4198AB0022B15F87, vehicle.Handle)) return;
            Function.Call(Hash._0xCFC8BE9A5E1FE575, vehicle.Handle, 0);
            var landingGrearTimeout = DateTime.UtcNow + new TimeSpan(0, 0, 5);
            while (Function.Call<int>(Hash._0x9B0F3DCA3DB0F4CD, vehicle.Handle) != 0)
            {
                if (DateTime.UtcNow > landingGrearTimeout)
                    break;
                Yield();
            }
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

        //private void StartScripts()
        //{
        //    if (_initializedScripts) return;
        //    GtsLibNet.StartScript("mission_triggerer_a", GtsLib.GetScriptStackSize("mission_triggerer_a"));
        //    GtsLibNet.StartScript("mission_triggerer_b", GtsLib.GetScriptStackSize("mission_triggerer_b"));
        //    GtsLibNet.StartScript("mission_triggerer_c", GtsLib.GetScriptStackSize("mission_triggerer_c"));
        //    GtsLibNet.StartScript("mission_triggerer_d", GtsLib.GetScriptStackSize("mission_triggerer_d"));
        //    GtsLibNet.StartScript("blip_controller", GtsLib.GetScriptStackSize("blip_controller"));
        //    _initializedScripts = true;
        //}

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
                GtsLibNet.DisplayHelpTextWithGxt("PRESS_E");
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
                GtsLibNet.DisplayHelpTextWithGxt("PRESS_E");
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