using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using GTA;
using GTA.Math;
using GTA.Native;
using GTS.DataClasses;
using GTS.Extensions;
using GTS.Library;
using GTS.OrbitalSystems;
using GTS.Scenes.Interiors;
using GTSCommon;
using NativeUI;
using Font = GTA.Font;

namespace GTS.Scenes
{
    /// <summary>
    ///     A player task type for zero G.
    /// </summary>
    public enum ZeroGTask
    {
        SpaceWalk,
        Mine,
        Repair
    }

    /// <summary>
    ///     Called when a <see cref="Scene" /> is exited.
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="newSceneFile"></param>
    /// <param name="exitRotation"></param>
    /// <param name="exitOffset"></param>
    public delegate void OnSceneExitEvent(Scene scene, string newSceneFile, Vector3 exitOffset, Vector3 exitRotation);

    /// <summary>
    ///     A Scene controls in-game logic for player movement, and referential game variables pertaining to
    ///     physics. It also adds props to the game based on it's <see cref="SceneInfo" /> data.
    /// </summary>
    public sealed class Scene
    {
        /// <summary>
        ///     The blip color of the mini map marker for planets.
        /// </summary>
        public const BlipColor MarkerBlipColor = (BlipColor) 58;

        /// <summary>
        ///     The texture dictionary used for the reticle.
        /// </summary>
        public const string ReticleTextureDict = "helicopterhud";

        /// <summary>
        ///     The texture used for the reticle.
        /// </summary>
        public const string ReticleTexture = "hud_lock";

        public const float MinWarpDetectDist = 10000;
        private readonly List<WarpEffect> _warpEffects = new List<WarpEffect>();
        private Orbital _currentWarpOrbital;

        private bool _didDeleteScene;
        private bool _didJump;
        private bool _didRaiseGears;
        private bool _didSetAreaTimecycle;
        private bool _didSetSpaceAudio;
        private bool _didSetTimecycle;
        private bool _didSpaceWalkTut;
        private bool _didWarpRotation;
        private bool _didWarpSfx;
        private bool _enteringVehicle;
        private bool _isSpaceVehicleInOrbit;

        private Vector3 _lastMinePos;
        private Prop _minableObject;
        private DateTime _mineTimeout;
        private float _pitchSpeed;

        private Vector3 _playerSimulatedOffset;
        private float _rollSpeed;
        private SpaceVehicleInfo _spaceVehicles;
        private Vehicle _spaceWalkObj;
        private Rope _spaceWalkRope;
        private bool _startedMining;
        private bool _stopUpdate;
        private Vector3 _vehicleLeavePos;
        private Vector3 _vehicleRepairNormal;
        private Vector3 _vehicleRepairPos;
        private DateTime _vehicleRepairTimeout;
        private float _verticalSpeed;

        private Camera _warpCamera;
        private bool _warpFlag;
        private MenuPool _warpMenuPool;
        private UIMenu _warpSelectMenu;
        private Prop _weldingProp;
        private LoopedPtfx _weldPtfx;
        private float _yawSpeed;

        public Scene(SceneInfo sceneData)
        {
            Info = sceneData;

            RegisteredVehicles = new List<Vehicle>();
            MiningProps = new List<Prop>();
            Surfaces = new List<Surface>();
            Scenarios = new List<Scenario>();
            Interiors = new List<Interior>();
            Blips = new List<Blip>();
            _spaceVehicles = new SpaceVehicleInfo();
        }

        public SceneInfo Info { get; }

        public Skybox Skybox { get; private set; }

        public List<Surface> Surfaces { get; private set; }

        public List<Orbital> WormHoles { get; private set; }

        public List<Billboardable> Billboards { get; private set; }

        public List<AttachedOrbital> AttachedOrbitals { get; private set; }

        public List<Orbital> Orbitals { get; private set; }

        public List<Vehicle> RegisteredVehicles { get; }

        public List<Scenario> Scenarios { get; }

        public List<Interior> Interiors { get; }

        public List<Blip> Blips { get; }

        public List<Prop> MiningProps { get; }

        public SpaceVehicle VehicleData
        {
            get
            {
                return _spaceVehicles?.VehicleData?.Find(
                    x => Game.GenerateHash(x.Model) == (PlayerVehicle?.Model.Hash ?? 0));
            }
        }

        public ZeroGTask PlayerTask { get; private set; }

        public string FileName { get; internal set; }

        public bool DebugWarp { get; set; }

        public Vector3 SimulatedPosition => PlayerPosition - Info.GalaxyCenter + _playerSimulatedOffset;

        private Vehicle PlayerVehicle { get; set; }

        private static Ped PlayerPed => Core.PlayerPed;

        private static Vector3 PlayerPosition
        {
            get => Core.PlayerPosition;
            set => Core.PlayerPosition = value;
        }

        public event OnSceneExitEvent Exited;

        private AttachedOrbital CreateAttachedOrbital(AttachedOrbitalInfo data)
        {
            if (string.IsNullOrEmpty(data.Model))
            {
                Debug.Log("Entity model was not set in the xml.", DebugMessageType.Error);

                return default(AttachedOrbital);
            }

            var model = RequestModel(data.Model);

            if (!model.IsLoaded)
            {
                Debug.Log($"Failed to load model: {data.Model}", DebugMessageType.Error);
                return default(AttachedOrbital);
            }

            Debug.Log($"Successfully loaded model: {data.Model}");

            var prop = World.CreateProp(model, Info.GalaxyCenter + data.Position, data.Rotation, false, false);
            prop.FreezePosition = true;
            prop.LodDistance = data.LodDistance;

            var orbital = new AttachedOrbital(prop, data.Position, data.Rotation,
                data.FreezeXCoord, data.FreezeYCoord, data.FreezeZCoord,
                data.ShiftX, data.ShiftY, data.ShiftZ,
                data.ShiftAmount);
            model.MarkAsNoLongerNeeded();

            return orbital;
        }

        private Orbital CreateOrbital(OrbitalInfo data)
        {
            if (string.IsNullOrEmpty(data.Model))
            {
                Debug.Log("Entity model was not set in the xml.", DebugMessageType.Error);
                return default(Orbital);
            }

            var model = RequestModel(data.Model);

            if (!model.IsLoaded)
            {
                Debug.Log($"Failed to load model: {data.Model}", DebugMessageType.Error);
                return default(Orbital);
            }

            Debug.Log($"Successfully loaded model: {data.Model}");

            var prop = GtsLibNet.CreatePropNoOffset(model, Info.GalaxyCenter + data.Position, false);
            prop.Rotation = data.Rotation;
            prop.FreezePosition = true;
            prop.LodDistance = data.LodDistance;

            var orbital = new Orbital(prop, data.Name, data.RotationSpeed, data.WormHole, data.TriggerDistance,
                data.NextScene, data.NextScenePosition, data.NextSceneRotation);

            if (!string.IsNullOrEmpty(data.Name))
            {
                var blip = orbital.AddBlip();
                blip.Sprite = (BlipSprite) 288;
                blip.Color = MarkerBlipColor;
                blip.Name = orbital.Name;
            }

            model.MarkAsNoLongerNeeded();

            return orbital;
        }

        private Surface CreateSurface(SurfaceInfo data)
        {
            if (string.IsNullOrEmpty(data.Model))
            {
                Debug.Log("Entity model was not set in the xml.", DebugMessageType.Error);
                return default(Surface);
            }

            var model = RequestModel(data.Model);
            if (!model.IsLoaded)
            {
                Debug.Log($"Failed to load model: {data.Model}", DebugMessageType.Error);
                return default(Surface);
            }

            Debug.Log(
                $"Successfully loaded model: {data.Model}{Environment.NewLine}Bounds: {model.GetDimensions().Length() / 2}");
            var pos = Info.GalaxyCenter + data.Position;
            var prop = World.CreateProp(model, pos, Vector3.Zero, false, false) ?? new Prop(0);
            prop.PositionNoOffset = pos;

            prop.FreezePosition = true;
            prop.LodDistance = data.LodDistance;

            var surface = new Surface(prop, data.TileSize, data.Dimensions)
            {
                CanUpdate = data.Tile,
                Offset = data.Position
            };
            surface.GenerateNeighbors();
            model.MarkAsNoLongerNeeded();

            return surface;
        }

        private static Prop CreateProp(Vector3 position, string modelName)
        {
            if (string.IsNullOrEmpty(modelName))
                return new Prop(0);

            var model = RequestModel(modelName);
            if (!model.IsLoaded)
            {
                Debug.Log($"Failed to load model: {modelName}", DebugMessageType.Error);
                return new Prop(0);
            }

            Debug.Log($"Successfully loaded model: {modelName}");

            var prop = World.CreateProp(model, Vector3.Zero, Vector3.Zero, false, false);
            prop.Position = position;
            prop.FreezePosition = true;
            model.MarkAsNoLongerNeeded();

            return prop;
        }

        private static Model RequestModel(string modelName)
        {
            var model = new Model(modelName);
            model.Request();
            while (!model.IsLoaded)
                Script.Yield();
            return model;
        }

        public Interior GetInterior(string name)
        {
            return Interiors.FirstOrDefault(x => x.Name == name);
        }

        internal void Start()
        {
            if (_didDeleteScene) return;
            ConfigureRendering();
            GameplayCamera.RelativeHeading = 0;
            GtsLibNet.ToggleAllIplsUnchecked(true);
            GtsLibNet.SetGravityLevel(Info.UseGravity ? Info.GravityLevel : 0f);
            Function.Call(Hash.STOP_AUDIO_SCENES);
            Function.Call(Hash.START_AUDIO_SCENE, "CREATOR_SCENES_AMBIENCE");
            Function.Call(Hash.SET_CLOCK_TIME, Info.Time, Info.TimeMinutes, 0);
            Function.Call(Hash.PAUSE_CLOCK, true);
            Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, Info.WeatherName);
            Function.Call(Hash.SET_ENTITY_ALWAYS_PRERENDER, Skybox?.Handle ?? 0, true);
            GetSpaceVehicles();
            ConfigureVehicleForScene();
            ResetPlayerPosition();
            CreateSpace();
            CreateInteriors();
            CreateTeleports();
            CreateScenarios();
            Game.MissionFlag = true;
            _didSpaceWalkTut = Core.Instance.Settings.GetValue("tutorial_info", "did_float_info", _didSpaceWalkTut);
            Debug.Log("Scene initialized.");
        }

        internal void Update()
        {
            if (_didDeleteScene || _stopUpdate) return;
            var viewFinderPosition = Database.ViewFinderPosition();
            ConfigureRendering();
            ConfigureWeather();
            UpdateAudio();
            SettingsUpdate();
            UpdateOrbitals(viewFinderPosition);
            SpaceWalkTask();
            PilotVehicle();
            HandleDeath();
            HandlePlayerVehicle();
            UpdateInteriorTeleports();
            UpdateSurfaceTiles();
            UpdateBillboards(viewFinderPosition);
            UpdateWormHoles();
            LowerPedAndVehicleDensity();
            Scenarios?.ForEach(scenario => scenario.Tick());
            if (Entity.Exists(Skybox)) Skybox.Position = viewFinderPosition;
            KeepInSafeZone();
            UpdateLockedWarpSystem();
            TryToExitScene();
        }

        internal void Delete(bool aborted = false)
        {
            try
            {
                ResetWarpSystems();
                Skybox.Delete();
                RemovePreviousVehicles();
                ResetPlayerVehicle();
                ClearLists(aborted);
                ResetGameData();
                _spaceWalkRope?.Delete();
                _didDeleteScene = true;
            }
            catch (Exception e)
            {
                Debug.Log("Failed to abort: " + e.Message + Environment.NewLine + e.StackTrace, DebugMessageType.Error);
            }
        }

        private void CreateWarpMenu()
        {
            if (_warpSelectMenu != null) return;
            _warpMenuPool = new MenuPool();
            _warpSelectMenu = new UIMenu("Warp Select", "Select A Location");
            _warpMenuPool.Add(_warpSelectMenu);
            ResetWarpMenuItems();
        }

        private void ResetWarpMenuItems()
        {
            if (_warpSelectMenu == null)
                return;

            // Clear the current menu items..
            _warpSelectMenu.Clear();

            // Collect the amount of orbitals greater than the detect distance.
            var orbitalList = Orbitals.Where(x => PlayerPosition.DistanceTo(x.Position) >= MinWarpDetectDist).ToList();

            // Loop through each of the orbitals.
            // Create a menu item for each of them...
            foreach (var orbital in orbitalList)
            {
                if (string.IsNullOrEmpty(orbital.Name)) continue;
                var menuItem = new UIMenuItem(orbital.Name, "Select to warp to " + orbital.Name);
                var orbital1 = orbital;
                menuItem.Activated += (sender, item) => _currentWarpOrbital = orbital1;
                _warpSelectMenu.AddItem(menuItem);
            }

            // Refresh the menu index.
            _warpMenuPool.RefreshIndex();
        }

        private void UpdateLockedWarpSystem()
        {
            if (Info.SurfaceScene) return;
            if (PlayerVehicle == null) return;
            if (!(VehicleData?.CanWarp ?? false)) return;
            CreateWarpMenu();

            // Allow the player to select an orbital from the menu.
            if (_currentWarpOrbital == null)
            {
                _warpMenuPool.ProcessMenus();
                if (_warpFlag)
                {
                    ResetWarpMenuItems();
                    ResetWarpSystems();
                    _warpFlag = false;
                }
                if (Game.IsControlJustPressed(2, Control.ParachuteSmoke))
                    _warpSelectMenu.Visible = !_warpSelectMenu.Visible;
                return;
            }

            if (!_warpFlag)
            {
                PlayerVehicle.FreezePosition = true;
                Game.Player.CanControlCharacter = false;

                // We started the warp so the warp flag should be true..
                _warpFlag = true;
            }
            GameplayCamera.RelativeHeading = 0f;
            GameplayCamera.RelativePitch = 0f;

            var directionToTarget = _currentWarpOrbital.Position - PlayerVehicle.Position;
            if (directionToTarget.Length() > 1)
                directionToTarget.Normalize();

            // Once we get the lerped rotation finished, we're going to calculate the 
            // rotation again, and lock the player's rotation to it completely.
            var right = Vector3.Cross(directionToTarget, Vector3.WorldUp);

            // The up rotation needs to be the direction to target with a 90 deg tilt on the x axis.
            var newUpQ = Quaternion.FromToRotation(Vector3.WorldUp,
                GtsLibNet.AngleAxis(90, right) * directionToTarget);

            // We want the player to face the direction to the target.
            var facingQ = Quaternion.Euler(0, 0,
                Vector3.SignedAngle(Vector3.RelativeFront, directionToTarget, Vector3.WorldUp));

            // Here's where we'll start the warp logic.
            if (!_didWarpRotation)
            {
                var angleToTarget = Vector3.Angle(PlayerVehicle.ForwardVector, directionToTarget);
                const float nearAngle = 10f;

                if (angleToTarget > nearAngle)
                {
                    // Get the lerped rotation to target.
                    var lerpedRotation = Quaternion.Lerp(PlayerVehicle.Quaternion, newUpQ * facingQ,
                        Game.LastFrameTime * VehicleData?.RotationMultiplier ?? 1.5f);

                    // Rotate the player.
                    PlayerVehicle.Quaternion = lerpedRotation;
                }
                else
                {
                    // Rotate the player.
                    PlayerVehicle.Quaternion = newUpQ * facingQ;

                    _didWarpRotation = true;
                }

                return;
            }

            Function.Call(Hash.HIDE_HUD_AND_RADAR_THIS_FRAME);

            // Now that the rotation is complete, we should start the warp drive.
            if (!Camera.Exists(_warpCamera))
            {
                _warpCamera = World.CreateCamera(GameplayCamera.Position, GameplayCamera.Rotation,
                    GameplayCamera.FieldOfView);
                _warpCamera.AttachTo(PlayerVehicle, PlayerVehicle.GetOffsetFromWorldCoords(_warpCamera.Position));
                _warpCamera.Shake(CameraShake.SkyDiving, 1f);
                _warpCamera.MotionBlurStrength = 10000f;
                World.RenderingCamera = _warpCamera;
            }

            if (!_didWarpSfx)
            {
                GTA.Audio.PlaySoundFrontend("ScreenFlash", "MissionFailedSounds");
                Effects.Start(ScreenEffect.MinigameTransitionOut);
                _didWarpSfx = true;

                foreach (var vehicleDataWarpModel in VehicleData?.WarpModels ?? new List<WarpModelInfo>(0))
                {
                    var model = new Model(vehicleDataWarpModel.Model);
                    model.Request();
                    while (!model.IsLoaded)
                        Script.Yield();
                    var prop = World.CreateProp(model, PlayerVehicle.Position, PlayerVehicle.Rotation, false, false);
                    var propExt = World.CreateProp(model, prop.Position, prop.Rotation, false, false);
                    propExt.AttachTo(prop, 0, new Vector3(0, model.GetDimensions().Length(), 0), Vector3.Zero);
                    _warpEffects.Add(new WarpEffect(prop.Handle, 0, 0, vehicleDataWarpModel.MoveSpeed,
                            vehicleDataWarpModel.RotationSpeed)
                        {Extension = propExt});
                }
            }

            foreach (var warpEffect in _warpEffects)
            {
                warpEffect.AttachTo(PlayerVehicle, 0, new Vector3(0, -warpEffect.MovementOffset, 0),
                    new Vector3(0, warpEffect.RotationOffset, 0));
                warpEffect.MovementOffset += Game.LastFrameTime * warpEffect.MovementSpeed;
                warpEffect.RotationOffset += Game.LastFrameTime * warpEffect.RotationSpeed;
                if (warpEffect.MovementOffset > warpEffect.Model.GetDimensions().Length())
                    warpEffect.MovementOffset = 0f;
            }

            if (_warpCamera.FieldOfView < 160)
                _warpCamera.FieldOfView += Game.LastFrameTime * 2.5f;

            PlayerPosition += directionToTarget * Game.LastFrameTime * (VehicleData?.WarpSpeed ?? 10000f);
            var dist = PlayerPosition.DistanceTo(_currentWarpOrbital.Position);
            if (dist >= MinWarpDetectDist) return;
            GameplayCamera.Shake(CameraShake.SmallExplosion, 1f);
            World.AddExplosion(GameplayCamera.Position, ExplosionType.Grenade, 0f, 0f, true, true);
            ResetWarpSystems();
        }

        private void ResetWarpSystems()
        {
            foreach (var warpEffects in _warpEffects)
                warpEffects?.Delete();
            _warpCamera?.Destroy();
            _didWarpRotation = false;
            _didWarpSfx = false;
            _currentWarpOrbital = null;
            Game.Player.CanControlCharacter = true;
            _warpCamera?.Destroy();
            World.RenderingCamera = null;
            PlayerVehicle.FreezePosition = false;
            Effects.Stop();
        }

        public void SetPropCanBeMined(Prop prop)
        {
            if (MiningProps.Contains(prop))
                return;
            MiningProps.Add(prop);
        }

        public void SetPropCannotBeMined(Prop prop)
        {
            MiningProps.Remove(prop);
        }

        public void RefreshTimecycle()
        {
            if (!string.IsNullOrEmpty(Info.TimecycleModifier) && Info.TimecycleModifierStrength > 0)
                TimecycleModifier.Set(Info.TimecycleModifier, Info.TimecycleModifierStrength);
            else TimecycleModifier.Clear();
        }

        public void DrawMarkerAt(Vector3 position, string name, Color? col = null)
        {
            if (string.IsNullOrEmpty(name)) return;
            if (_warpFlag) return;

            const float scale = 64f;
            const float width = 1f / 1920 / (1f / scale);
            const float height = 1f / 1080 / (1f / scale);
            if (col == null) col = ColorTranslator.FromHtml("#8000FF");
            if (position.DistanceTo(Database.ViewFinderPosition()) > 7000)
            {
                var dir = position - Database.ViewFinderPosition();
                if (dir.Length() > 1) dir.Normalize();
                position = Database.ViewFinderPosition() + dir * 7000;
            }

            Function.Call(Hash.SET_DRAW_ORIGIN, position.X, position.Y, position.Z, 0);
            Function.Call(Hash.DRAW_SPRITE, ReticleTextureDict, ReticleTexture, 0, 0, width, height, 45f, col.Value.R,
                col.Value.G, col.Value.B, col.Value.A);

            Function.Call(Hash.SET_TEXT_FONT, (int) Font.ChaletComprimeCologne);
            Function.Call(Hash.SET_TEXT_SCALE, 0.3f, 0.3f);
            Function.Call(Hash.SET_TEXT_COLOUR, col.Value.R, col.Value.G, col.Value.B, col.Value.A);
            Function.Call(Hash.SET_TEXT_DROPSHADOW, 1, 1, 1, 1, 1);
            Function.Call(Hash.SET_TEXT_EDGE, 1, 1, 1, 1, 205);
            Function.Call(Hash.SET_TEXT_JUSTIFICATION, 0);
            Function.Call(Hash.SET_TEXT_WRAP, 0, width);
            Function.Call(Hash._SET_TEXT_ENTRY, "CELL_EMAIL_BCON");
            Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, name);
            Function.Call(Hash._DRAW_TEXT, 0f, -0.01f);

            Function.Call(Hash.CLEAR_DRAW_ORIGIN);
        }

        private void RemovePreviousVehicles()
        {
            foreach (var v in RegisteredVehicles)
                if (Entity.Exists(v) && PlayerVehicle != v)
                    v.Delete();
        }

        private void ClearLists(bool aborted)
        {
            foreach (var b in Blips)
                b?.Remove();
            foreach (var interior in Interiors)
                interior?.Remove();
            foreach (var s in Surfaces)
                s?.Delete();
            foreach (var billboard in Billboards)
                billboard?.Delete();
            foreach (var orbital in Orbitals)
                orbital?.Delete();
            foreach (var lOrbital in AttachedOrbitals)
                lOrbital?.Delete();

            foreach (var scenario in Scenarios)
            {
                if (aborted)
                {
                    scenario?.SendMessage("OnAborted");
                    continue;
                }
                scenario?.SendMessage("OnDisable", false);
            }

            Debug.Log("Deleted objects and cleared lists.");
        }

        private static void ResetGameData()
        {
            GameplayCamera.ShakeAmplitude = 0;
            Function.Call(Hash._0x5E5E99285AE812DB);
            Function.Call(Hash.SET_WIND_SPEED, 1.0f);
            Function.Call(Hash.STOP_AUDIO_SCENES);
            Function.Call(Hash.CLEAR_TIMECYCLE_MODIFIER);
            Function.Call(Hash.SET_STREAMED_TEXTURE_DICT_AS_NO_LONGER_NEEDED, ReticleTextureDict);
            Function.Call(Hash.DISABLE_VEHICLE_DISTANTLIGHTS, false);
            Function.Call(Hash.PAUSE_CLOCK, false);
            Function.Call(Hash._CLEAR_CLOUD_HAT);
            GtsLibNet.ToggleAllIpls(false);
            Game.MissionFlag = false;
            GtsLib.SetGravityLevel(9.8000002f);
        }

        private void GetSpaceVehicles()
        {
            const string path = ".\\scripts\\Space\\SpaceVehicles.xml";
            if (!File.Exists(path))
                return;
            _spaceVehicles = XmlSerializer.Deserialize<SpaceVehicleInfo>(path);
        }

        private void ResetPlayerPosition()
        {
            var position = Info.GalaxyCenter;
            if (Info.SurfaceScene)
                if (!Entity.Exists(PlayerPed.CurrentVehicle) || !CanDoOrbitLanding() || _isSpaceVehicleInOrbit)
                {
                    var newPosition = GtsLibNet.GetGroundHeightRay(position, PlayerPed);
                    var timer = DateTime.Now + new TimeSpan(0, 0, 5);

                    while (newPosition == Vector3.Zero && DateTime.Now < timer)
                    {
                        newPosition = GtsLibNet.GetGroundHeightRay(position, PlayerPed);
                        Script.Yield();
                    }

                    if (newPosition != Vector3.Zero)
                        position = newPosition;
                }
                else
                {
                    return;
                }
            PlayerPosition = position;
        }

        private void ResetPlayerVehicle()
        {
            if (PlayerVehicle == null) return;
            PlayerPed.SetIntoVehicle(PlayerVehicle, VehicleSeat.Driver);
            PlayerVehicle.LockStatus = VehicleLockStatus.None;

            var heading = PlayerVehicle.Heading;
            PlayerVehicle.Quaternion = Quaternion.Identity;
            PlayerVehicle.Heading = heading;
            PlayerVehicle.Velocity = Vector3.Zero;
            PlayerVehicle.IsInvincible = false;
            PlayerVehicle.EngineRunning = true;
            PlayerVehicle.FreezePosition = false;
            PlayerVehicle.IsPersistent = true;
            foreach (var vehicleDoor in PlayerVehicle.GetDoors())
                PlayerVehicle.CloseDoor(vehicleDoor, true);
        }

        private void ExitSceneFromSurface()
        {
            Exited?.Invoke(this, Info.NextScene, Info.NextScenePosition, Info.NextSceneRotation);
        }

        private bool CanDoOrbitLanding()
        {
            return Info.OrbitAllowLanding && !Scenarios.Any(x => x.BlockOrbitLanding);
        }

        private void CreateScenarios()
        {
            if (!Settings.UseScenarios) return;
            try
            {
                CreateScenariosForSceneInfo(Info);
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message + Environment.NewLine + ex.StackTrace, DebugMessageType.Error);
            }
        }

        private void CreateScenariosForSceneInfo(SceneInfo scene)
        {
            foreach (var scenarioInfo in scene.Scenarios)
            {
                var assembly = Assembly.LoadFrom(Path.Combine(Database.PathToScenarios, scenarioInfo.Dll));
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (type == null || type.BaseType != typeof(Scenario)) continue;
                    if (type.Name != scenarioInfo.TypeName)
                        continue;
                    var instance = (Scenario) Activator.CreateInstance(type);
                    instance.CurrentScene = this;
                    instance.SendMessage("Awake");
                    if (instance.IsScenarioComplete()) continue;
                    Debug.Log("Created Scenario: " + type.Name);
                    Scenarios.Add(instance);
                }
            }

            foreach (var scenario in Scenarios)
            {
                scenario.SendMessage("Start");
                scenario.Completed += OnScenarioComplete;
            }
        }

        private void UpdateBillboards(Vector3 viewFinderPosition)
        {
            if (_warpFlag)
            {
                foreach (var billboardable in Billboards)
                    billboardable.IsVisible = false;
                return;
            }

            foreach (var billboardable in Billboards)
            {
                billboardable.IsVisible = true;
                //var pos = Info.GalaxyCenter + billboardable.StartPosition;
                //var startDist = billboardable.ParallaxStartDistance;
                //var d = viewFinderPosition.DistanceTo(pos);
                //if (d < startDist && d > 1000)
                //{
                //    var m = (startDist - d) * billboardable.ParallaxAmount;
                //    billboardable.Position = pos - billboardable.ForwardVector * m;
                //}
                //else billboardable.Position = pos;

                var aDir = billboardable.ForwardVector;
                var bDir = billboardable.Position - viewFinderPosition;
                if (bDir.Length() > 0)
                    bDir.Normalize();
                billboardable.Quaternion = Quaternion.Lerp(billboardable.Quaternion,
                    Quaternion.FromToRotation(aDir, bDir) * billboardable.Quaternion, Game.LastFrameTime * 5);
            }
        }

        private void OnScenarioComplete(Scenario scenario, bool success)
        {
            Scenarios.Remove(scenario);
            _didSetSpaceAudio = false;
        }

        private void CreateLink(Link sceneLink)
        {
            if (string.IsNullOrEmpty(sceneLink.Name)) return;
            var blip = World.CreateBlip(Info.GalaxyCenter + sceneLink.Position);
            blip.Sprite = (BlipSprite) 178;
            blip.Color = MarkerBlipColor;
            blip.Name = sceneLink.Name;
            Blips.Add(blip);
        }

        private void UpdateSurfaceTiles()
        {
            if (!Info.SurfaceScene) return;
            var pos = Game.Player.Character.Position;
            foreach (var surface in Surfaces)
                surface.Update(pos);
        }

        private void CreateSpace()
        {
            var skybox = CreateProp(PlayerPed.Position, Info.SkyboxModel);
            Function.Call(Hash.SET_ENTITY_ALWAYS_PRERENDER, skybox, true);

            Orbitals = Info.Orbitals?.Select(CreateOrbital).Where(o => o != default(Orbital)).ToList();

            AttachedOrbitals = Info.AttachedOrbitals?.Select(CreateAttachedOrbital)
                .Where(o => o != default(AttachedOrbital)).ToList();

            Surfaces = Info.Surfaces?.Select(CreateSurface).Where(o => o != default(Surface)).ToList();

            Billboards = Info.Billboards
                ?.Select(
                    x => new Billboardable(CreateProp(Info.GalaxyCenter + x.Position, x.Model).Handle, x.Position)
                    {
                        ParallaxAmount = x.ParallaxAmount,
                        ParallaxStartDistance = x.ParallaxStartDistance
                    }).ToList();

            WormHoles = Orbitals?.Where(x => x.WormHole).ToList();

            Skybox = new Skybox(skybox ?? new Prop(0));

            Info.SceneLinks.ForEach(CreateLink);
        }

        private void CreateInteriors()
        {
            RequestInteriors();

            if (Info.Interiors.Any())
                Debug.Log($"Created {Interiors.Count}/{Info.Interiors.Count} interiors.");
        }

        private void RequestInteriors()
        {
            foreach (var interiorInfo in Info.Interiors)
            {
                var interior = new Interior(interiorInfo.Name, interiorInfo.Type);
                interior.Request();
                Interiors.Add(interior);
            }
        }

        private void CreateTeleports()
        {
            foreach (var point in Info.Teleports)
                if (point.CreateBlip)
                {
                    var blipStart = World.CreateBlip(point.Start);

                    blipStart.Sprite = BlipSprite.Garage2;

                    blipStart.Name = "Teleport";

                    Blips.Add(blipStart);
                }
        }

        private void SettingsUpdate()
        {
            if (Settings.MoonJump && Info.SurfaceScene)
            {
                if (!_didJump && PlayerPed.IsJumping)
                {
                    PlayerPed.Velocity += PlayerPed.UpVector * Info.JumpForceOverride;

                    _didJump = true;
                }
                else if (_didJump && !PlayerPed.IsJumping && !PlayerPed.IsInAir)
                {
                    _didJump = false;
                }
            }
            else
            {
                _didJump = false;
            }
        }

        private void ConfigureRendering()
        {
            UI.HideHudComponentThisFrame(HudComponent.AreaName);
            Function.Call(Hash.SET_RADAR_AS_INTERIOR_THIS_FRAME);

            if (Settings.ShowCustomGui && (VehicleData?.CanWarp ?? false) && !Info.SurfaceScene)
            {
                //if (!Function.Call<bool>(Hash.IS_HUD_HIDDEN) || _warpFlag || GtsLibNet.IsHelpMessageBeingDisplayed() && !Info.SurfaceScene)
                //{
                //    var items = new List<string>();
                //    items.Add(@"~Ω~~y~Celestials Detected~s~");
                //    if (Orbitals?.Any() ?? false)
                //        items.AddRange(from orbital in Orbitals where !string.IsNullOrEmpty(orbital.Name) && orbital.Position.DistanceTo(PlayerPosition) < 80000 select orbital.Name);
                //    var itemConcat = string.Join("\n", items);
                //    var resText = new UIResText(itemConcat, new Point(100, 50), 0.3f, Color.White, Font.Monospace, UIResText.Alignment.Centered);
                //    resText.Draw();
                //    resText.Shadow = true;
                //}
            }

            if (Camera.Exists(World.RenderingCamera) || FollowCam.ViewMode == FollowCamViewMode.FirstPerson)
            {
                _didSetTimecycle = false;
                _didSetAreaTimecycle = false;
                return;
            }

            if (!_didSetTimecycle)
            {
                RefreshTimecycle();
                _didSetTimecycle = true;
            }

            UpdateTimecycleAreas();
        }

        private void ConfigureWeather()
        {
            Function.Call(Hash.SET_WIND_SPEED, Info.WindSpeed);
            Function.Call(Hash._SET_RAIN_FX_INTENSITY, Info.PuddleIntensity);
            Function.Call(Hash._0xB96B00E976BE977F, Info.WaveStrength);

            if (!Info.CloudsEnabled)
            {
                Function.Call(Hash._CLEAR_CLOUD_HAT);
                return;
            }

            if (string.IsNullOrEmpty(Info.CloudType)) return;
            Function.Call(Hash._SET_CLOUD_HAT_TRANSITION, Info.CloudType, 0f);
        }

        private void ConfigureVehicleForScene()
        {
            var vehicle = PlayerPed.CurrentVehicle;

            if (!Entity.Exists(vehicle)) return;

            PlayerVehicle = vehicle;
            vehicle.IsInvincible = true;
            vehicle.IsPersistent = true;

            if (Info.SurfaceScene)
            {
                GtsLib.ResetVehicleGravity(vehicle);
                SpaceVehicle spaceVehicle;
                if ((spaceVehicle =
                        _spaceVehicles?.VehicleData.Find(
                            x => x.RemainInOrbit && Game.GenerateHash(x.Model ?? string.Empty) ==
                                 vehicle.Model.Hash)) == null)
                    if (CanDoOrbitLanding())
                    {
                        Debug.Log("Doing orbital Landing...");
                        vehicle.Rotation = Info.OrbitLandingRotation;
                        vehicle.Position = Info.GalaxyCenter + Info.OrbitLandingPosition;
                        Function.Call(Hash.SET_VEHICLE_FORWARD_SPEED, vehicle, Info.OrbitLandingSpeed);
                        return;
                    }

                PlayerPed.Task.ClearAllImmediately();
                vehicle.Quaternion = Quaternion.Identity;
                vehicle.Rotation = Vector3.Zero;
                vehicle.Velocity = Vector3.Zero;
                vehicle.Speed = 0;

                if (spaceVehicle == null)
                {
                    vehicle.PositionNoOffset = Info.VehicleSurfaceSpawn + Vector3.WorldUp;
                    var timeout = DateTime.Now + new TimeSpan(0, 0, 0, 2);
                    while (!Function.Call<bool>(Hash._0x49733E92263139D1, vehicle.Handle, 5.0f) &&
                           DateTime.Now < timeout)
                        Script.Yield();
                }
                else
                {
                    vehicle.PositionNoOffset = Info.OrbitalVehicleOffset + Info.GalaxyCenter;
                    vehicle.FreezePosition = true;
                    _isSpaceVehicleInOrbit = true;
                    var b = World.CreateBlip(Info.VehicleSurfaceSpawn);
                    b.Sprite = BlipSprite.Garage2;
                    b.Name = "Exit Surface";
                    _vehicleLeavePos = Info.VehicleSurfaceSpawn;
                    var p = GtsLibNet.GetGroundHeightRay(b.Position, PlayerPed);
                    if (p != Vector3.Zero)
                    {
                        _vehicleLeavePos = p;
                        b.Position = p;
                    }
                    Blips.Add(b);
                }
            }
            else
            {
                PlayerPed.CanRagdoll = false;
            }
        }

        private void UpdateOrbitals(Vector3 viewFinderPosition)
        {
            if (!Function.Call<bool>(Hash.HAS_STREAMED_TEXTURE_DICT_LOADED, ReticleTextureDict))
            {
                Function.Call(Hash.REQUEST_STREAMED_TEXTURE_DICT, ReticleTextureDict);
                while (!Function.Call<bool>(Hash.HAS_STREAMED_TEXTURE_DICT_LOADED, ReticleTextureDict))
                    Script.Yield();
            }

            foreach (var o in Orbitals)
            {
                if (Math.Abs(o.RotationSpeed) > 0.00001f)
                    o.Quaternion = Quaternion.Lerp(o.Quaternion,
                        Quaternion.FromToRotation(o.ForwardVector, o.RightVector) * o.Quaternion,
                        Game.LastFrameTime * o.RotationSpeed);

                if (!Settings.ShowCustomGui)
                    continue;

                DrawMarkerAt(o.Position, o.Name);
            }

            foreach (var a in AttachedOrbitals)
            {
                var pos = viewFinderPosition + a.AttachOffset;
                if (a.FreezeX)
                    pos.X = Info.GalaxyCenter.X + a.AttachOffset.X;
                if (a.FreezeY)
                    pos.Y = Info.GalaxyCenter.Y + a.AttachOffset.Y;
                if (a.FreezeZ)
                    pos.Z = Info.GalaxyCenter.Z + a.AttachOffset.Z;

                // Add slight shift to object.
                var offset = _playerSimulatedOffset * a.ShiftAmount;
                if (!a.ShiftX) offset.X = 0;
                if (!a.ShiftY) offset.Y = 0;
                if (!a.ShiftZ) offset.Z = 0;

                a.Position = pos - offset;
            }

            foreach (var l in Info.SceneLinks)
            {
                if (!Settings.ShowCustomGui)
                    continue;

                DrawMarkerAt(Info.GalaxyCenter + l.Position, l.Name);
            }
        }

        private void UpdateAudio()
        {
            if (Info.UseSound) return;
            if (FollowCam.ViewMode == FollowCamViewMode.FirstPerson || Settings.AlwaysUseSound)
            {
                if (!_didSetSpaceAudio) return;
                Function.Call(Hash.STOP_AUDIO_SCENES);
                Function.Call(Hash.START_AUDIO_SCENE, "CREATOR_SCENES_AMBIENCE");
                _didSetSpaceAudio = false;
            }
            else
            {
                if (_didSetSpaceAudio) return;
                Function.Call(Hash.STOP_AUDIO_SCENES);
                Function.Call(Hash.START_AUDIO_SCENE, "END_CREDITS_SCENE");
                _didSetSpaceAudio = true;
            }
        }

        private void UpdateInteriorTeleports()
        {
            var scale = new Vector3(1, 1, 1) * .3f;
            const float distance = 4;

            foreach (var t in Info.Teleports)
            {
                if (t.StartMarker)
                    World.DrawMarker(MarkerType.VerticalCylinder, t.Start - Vector3.WorldUp, Vector3.RelativeRight,
                        Vector3.Zero, scale, Color.Purple);

                if (t.EndMarker)
                    World.DrawMarker(MarkerType.VerticalCylinder, t.End - Vector3.WorldUp, Vector3.RelativeRight,
                        Vector3.Zero, scale, Color.Purple);

                //////////////////////////////////////////////////
                // NOTE: Using lengthSquared because it's faster.
                //////////////////////////////////////////////////
                var distanceStart = (t.Start - PlayerPosition).LengthSquared();

                if (distanceStart < distance)
                {
                    GtsLibNet.DisplayHelpTextWithGxt("PRESS_E");

                    if (Game.IsControlJustReleased(2, Control.Context))
                    {
                        Game.FadeScreenOut(750);
                        Script.Wait(750);

                        PlayerPosition = t.End - Vector3.WorldUp;
                        PlayerPed.Heading = t.EndHeading;
                        GameplayCamera.RelativeHeading = 0;

                        Script.Wait(1250);
                        Game.FadeScreenIn(750);
                    }
                }

                var distanceEnd = (t.End - PlayerPosition).LengthSquared();

                if (!(distanceEnd < distance)) continue;
                GtsLibNet.DisplayHelpTextWithGxt("PRESS_E");

                if (!Game.IsControlJustPressed(2, Control.Context)) continue;
                Game.FadeScreenOut(750);
                Script.Wait(750);

                PlayerPosition = t.Start - Vector3.WorldUp;
                PlayerPed.Heading = t.StartHeading;
                GameplayCamera.RelativeHeading = 0;

                Script.Wait(1250);
                Game.FadeScreenIn(750);
            }
        }

        private void HandlePlayerVehicle()
        {
            if (!Info.SurfaceScene)
            {
                if (Game.IsLoading || Game.IsScreenFadedOut) return;
                if (_didRaiseGears || !Entity.Exists(PlayerPed.CurrentVehicle)) return;
                PlayerPed.CurrentVehicle.LandingGear = VehicleLandingGear.Retracted;
                _didRaiseGears = true;
                return;
            }
            ReturnToOrbit();
        }

        private void ReturnToOrbit()
        {
            if (string.IsNullOrEmpty(Info.NextScene))
                return;

            const float distance = 15 * 2;

            if (PlayerPed.Position.Z - Info.GalaxyCenter.Z > Info.OrbitLeaveHeight)
            {
                ExitSceneFromSurface();
                return;
            }

            if (_isSpaceVehicleInOrbit)
            {
                World.DrawMarker(MarkerType.VerticalCylinder, _vehicleLeavePos, Vector3.RelativeRight, Vector3.Zero,
                    new Vector3(0.4f, 0.4f, 0.4f), Color.DarkRed);
                var dist = PlayerPed.Position.DistanceToSquared(_vehicleLeavePos);
                if (dist < 4)
                {
                    GtsLibNet.DisplayHelpTextWithGxt("RET_ORBIT");
                    Game.DisableControlThisFrame(2, Control.Enter);
                    if (Game.IsDisabledControlJustPressed(2, Control.Enter))
                        ExitSceneFromSurface();
                }
            }

            if (!Info.LeaveSurfacePrompt)
            {
                ChangePlayerVehicle();
                return;
            }

            if (PlayerVehicle != null && PlayerPosition.DistanceToSquared(PlayerVehicle.Position) < distance &&
                !PlayerPed.IsInVehicle())
            {
                GtsLibNet.DisplayHelpTextWithGxt("RET_ORBIT");
                Game.DisableControlThisFrame(2, Control.Enter);
                if (Game.IsDisabledControlJustPressed(2, Control.Enter))
                    PlayerPed.SetIntoVehicle(PlayerVehicle, VehicleSeat.Driver);
            }

            if (PlayerVehicle != null && PlayerPed.IsInVehicle(PlayerVehicle) && !CanDoOrbitLanding())
            {
                ExitSceneFromSurface();
            }
            else if (PlayerPed.IsInVehicle())
            {
                ChangePlayerVehicle();
                GtsLibNet.DisplayHelpTextWithGxt("RET_ORBIT2");
                Game.DisableControlThisFrame(2, Control.Context);
                if (!Game.IsDisabledControlJustPressed(2, Control.Context)) return;
                RegisteredVehicles.Add(PlayerVehicle);
                PlayerVehicle = PlayerPed.CurrentVehicle;
            }
        }

        private void HandleDeath()
        {
            if (PlayerPed.IsDead)
            {
                Game.TimeScale = 0.3f;
                Function.Call(Hash.START_AUDIO_SCENE, "DEATH_SCENE");
                ScaleFormMessages.Message.SHOW_MISSION_PASSED_MESSAGE(Game.GetGXTEntry("BM_LABEL_2"), 1000);
                Game.PlaySound("ScreenFlash", "WastedSounds");
                Script.Wait(1500);
                Game.FadeScreenOut(1000);
                Script.Wait(1000);
                var spawn = Info.GalaxyCenter;
                Function.Call(Hash.NETWORK_RESURRECT_LOCAL_PLAYER, spawn.X, spawn.Y, spawn.Z, 0, false, false);
                Function.Call(Hash._RESET_LOCALPLAYER_STATE);
                Function.Call(Hash.STOP_AUDIO_SCENE, "DEATH_SCENE");
                Exited?.Invoke(this, FileName, Vector3.Zero, Vector3.Zero);
                Script.Wait(500);
                Game.FadeScreenIn(1000);
                Game.TimeScale = 1.0f;
                return;
            }

            //var height = PlayerPed.Position.Z - Info.GalaxyCenter.Z;
            //if (height < Info.CrushMinDepth)
            //{
            //    Function.Call(Hash.APPLY_DAMAGE_TO_PED, PlayerPed, Info.CrushMinDepth / height * Info.CrushDamageMultiplier, true);
            //    if (height < Info.CrushMaxDepth)
            //        PlayerPed.Kill();
            //}

            GtsLibNet.TerminateScript("respawn_controller");
            Game.Globals[4].SetInt(1);
            Function.Call(Hash._DISABLE_AUTOMATIC_RESPAWN, true);
            Function.Call(Hash.SET_FADE_IN_AFTER_DEATH_ARREST, false);
            Function.Call(Hash.SET_FADE_OUT_AFTER_ARREST, false);
            Function.Call(Hash.SET_FADE_OUT_AFTER_DEATH, false);
            Function.Call(Hash.IGNORE_NEXT_RESTART, true);
        }

        private static void LowerPedAndVehicleDensity()
        {
            Function.Call(Hash.SET_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0.0f);
            Function.Call(Hash.SET_PED_DENSITY_MULTIPLIER_THIS_FRAME, 0.0f);
            Function.Call(Hash.SET_SCENARIO_PED_DENSITY_MULTIPLIER_THIS_FRAME, 0.0f);
            Function.Call(Hash.SET_RANDOM_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0.0f);
            Function.Call(Hash.SET_PARKED_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0.0f);
            Function.Call(Hash.DISABLE_VEHICLE_DISTANTLIGHTS, true);
        }

        private void TryToExitScene()
        {
            foreach (var orbital in Orbitals)
            {
                if (string.IsNullOrEmpty(orbital?.NextScene)) continue;
                var position = orbital.Position;
                var distance = Vector3.DistanceSquared(PlayerPosition, position);
                if (Settings.DebugTriggers)
                    World.DrawMarker(MarkerType.DebugSphere, position, Vector3.Zero, Vector3.Zero,
                        new Vector3(orbital.TriggerDistance, orbital.TriggerDistance, orbital.TriggerDistance),
                        Color.FromArgb(150, 255, 0, 0));
                if (!(distance <= orbital.TriggerDistance * orbital.TriggerDistance)) continue;
                Exited?.Invoke(this, orbital.NextScene, orbital.NextScenePosition, orbital.NextSceneRotation);
                _stopUpdate = true;
                break;
            }
            foreach (var link in Info.SceneLinks)
            {
                if (string.IsNullOrEmpty(link?.NextScene)) continue;
                var position = Info.GalaxyCenter + link.Position;
                var distance = Vector3.DistanceSquared(PlayerPosition, position);
                if (Settings.DebugTriggers)
                    World.DrawMarker(MarkerType.DebugSphere, position, Vector3.Zero, Vector3.Zero,
                        new Vector3(link.TriggerDistance, link.TriggerDistance, link.TriggerDistance),
                        Color.FromArgb(150, 255, 255, 0));
                if (!(distance <= link.TriggerDistance * link.TriggerDistance)) continue;
                Exited?.Invoke(this, link.NextScene, link.NextScenePosition, link.NextSceneRotation);
                _stopUpdate = true;
                break;
            }
        }

        private void UpdateTimecycleAreas()
        {
            var area = Info.TimecycleAreas.FirstOrDefault(
                x => Game.Player.Character.Position.DistanceTo(x.Location) <= x.TriggerDistance);
            if (area == null)
            {
                if (!_didSetAreaTimecycle) return;
                Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, Info.WeatherName);
                Function.Call(Hash.SET_CLOCK_TIME, Info.Time, Info.TimeMinutes);
                _didSetAreaTimecycle = _didSetTimecycle = false;
                // _didSetTimecycle will tell ConfigureRendering() to reset the timecycle modifier.
                return;
            }

            if (_didSetAreaTimecycle) return;
            TimecycleModifier.Set(area.TimeCycleModifier, area.TimeCycleModifierStrength);
            if (!string.IsNullOrEmpty(area.WeatherName))
                Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, area.WeatherName);
            Function.Call(Hash.SET_CLOCK_TIME, area.Time, area.TimeMinutes);
            _didSetAreaTimecycle = true;
        }

        private void PilotVehicle()
        {
            if (Info.SurfaceScene) return;
            if (!Entity.Exists(PlayerVehicle))
                return;

            if (!PlayerPed.IsInVehicle(PlayerVehicle))
                return;

            float speed = Settings.VehicleFlySpeed;
            var v = _spaceVehicles?.VehicleData.Find(
                x => Game.GenerateHash(x.Model) == PlayerVehicle.Model.Hash);

            if (v != null)
                speed = v.Speed;

            EntityFlightControl(PlayerVehicle, speed, Settings.MouseControlFlySensitivity,
                !PlayerVehicle.IsOnAllWheels, v?.RotationMultiplier ?? 1, VehicleData?.NewtonianPhysics ?? false);

            SetVehicleDrag();
        }

        private void SpaceWalkTask()
        {
            if (Info.SurfaceScene) return;
            if (PlayerPed.IsInVehicle())
            {
                StopSpaceWalking();
                Game.DisableControlThisFrame(2, Control.VehicleExit);
                if (!Game.IsDisabledControlJustPressed(2, Control.VehicleExit)) return;
                Game.FadeScreenOut(100);
                Script.Wait(100);
                PlayerVehicle.FreezePosition = true;
                PlayerVehicle.IsPersistent = true;
                PlayerPed.Task.WarpOutOfVehicle(PlayerVehicle);
                while (PlayerPed.IsInVehicle(PlayerVehicle))
                    Script.Yield();
                SpaceWalk_Toggle(); // Call this here so before the screen fades back in the player's already space walking.
                Game.FadeScreenIn(100);
            }
            // here's where we're in space without a vehicle.
            else if (!PlayerPed.IsRagdoll && !PlayerPed.IsJumpingOutOfVehicle)
            {
                switch (PlayerTask)
                {
                    // this let's us float
                    case ZeroGTask.SpaceWalk:
                        SpaceWalk();
                        break;
                    // this let's us mine asteroids.
                    case ZeroGTask.Mine:
                    {
                        if (_minableObject == null || !Entity.Exists(_spaceWalkObj) || _lastMinePos == Vector3.Zero)
                        {
                            if (Entity.Exists(_spaceWalkObj))
                                _spaceWalkObj.Detach();

                            PlayerTask = ZeroGTask.SpaceWalk;
                            return;
                        }

                        // attach the player to the mineable object.
                        if (!_startedMining)
                        {
                            var dir = _lastMinePos - _spaceWalkObj.Position;
                            dir.Normalize();
                            _spaceWalkObj.Quaternion = Quaternion.FromToRotation(_spaceWalkObj.ForwardVector, dir) *
                                                       _spaceWalkObj.Quaternion;
                            _mineTimeout = DateTime.UtcNow + new TimeSpan(0, 0, 0, 5);
                            _spaceWalkObj.Position = _lastMinePos - dir;
                            _startedMining = true;
                        }
                        else
                        {
                            if (!PlayerPed.IsPlayingAnim("amb@world_human_welding@male@base", "base"))
                            {
                                PlayerPed.Task.PlayAnimation("amb@world_human_welding@male@base", "base", 4.0f,
                                    -4.0f, -1, (AnimationFlags) 49, 0.0f);
                                SpaceWalk_CreateWeldingProp(PlayerPed);
                                return;
                            }

                            if (DateTime.UtcNow > _mineTimeout)
                            {
                                PlayerPed.Task.ClearAnimation("amb@world_human_welding@male@base", "base");
                                SpaceWalk_RemoveWeldingProp();
                                _spaceWalkObj.Detach();
                                _spaceWalkObj.HasCollision = false;
                                _spaceWalkObj.IsVisible = false;
                                _spaceWalkObj.HasGravity = false;
                                PlayerPed.IsVisible = true;
                                Function.Call(Hash.SET_VEHICLE_GRAVITY, _spaceWalkObj, false);
                                GtsLibNet.NotifyWithGxt("GTS_LABEL_26");
                                _lastMinePos = Vector3.Zero;
                                _minableObject = null;
                                _startedMining = false;
                                PlayerTask = ZeroGTask.SpaceWalk;
                            }
                        }
                    }
                        break;
                    // this lets us repair stuff.
                    case ZeroGTask.Repair:
                    {
                        // the vehicle repair failed somehow and we need to fallback to the first switch case.
                        if (_vehicleRepairPos == Vector3.Zero || _vehicleRepairNormal == Vector3.Zero ||
                            _spaceWalkObj == null ||
                            !_spaceWalkObj.Exists())
                        {
                            PlayerTask = ZeroGTask.SpaceWalk;
                            return;
                        }

                        // If we decide to move in another direction, let's cancel.
                        if (Game.IsControlJustPressed(2, Control.VehicleAccelerate) ||
                            Game.IsControlJustPressed(2, Control.MoveLeft) ||
                            Game.IsControlJustPressed(2, Control.MoveRight) ||
                            Game.IsControlJustPressed(2, Control.VehicleBrake))
                        {
                            PlayerTask = ZeroGTask.SpaceWalk;
                            return;
                        }

                        // get some params for this sequence.
                        var distance = PlayerPosition.DistanceTo(_vehicleRepairPos);
                        GetDimensions(PlayerPed, out Vector3 _, out Vector3 _, out Vector3 _, out Vector3 _,
                            out float radius);

                        // make sure we're within distance of the vehicle.
                        if (distance > radius)
                        {
                            // make sure to rotate the fly helper towards the repair point.
                            var dir = _vehicleRepairPos + _vehicleRepairNormal * 0.5f - _spaceWalkObj.Position;
                            dir.Normalize();
                            var lookRotation = Quaternion.FromToRotation(_spaceWalkObj.ForwardVector, dir) *
                                               _spaceWalkObj.Quaternion;
                            _spaceWalkObj.Quaternion = Quaternion.Lerp(_spaceWalkObj.Quaternion, lookRotation,
                                Game.LastFrameTime * 5);

                            // now move the fly helper towards the direction of the repair point.
                            _spaceWalkObj.Velocity = dir * 1.5f;

                            // make sure that we update the timer so that if the time runs out, we will fallback to the floating case.
                            _vehicleRepairTimeout = DateTime.UtcNow + new TimeSpan(0, 0, 0, 5);
                        }
                        else
                        {
                            // since we're in tange of the vehicle we want to start the repair sequence.
                            // we're going to stop the movement of the player, and play the repairing animation.
                            var lookRotation =
                                Quaternion.FromToRotation(_spaceWalkObj.ForwardVector, -_vehicleRepairNormal) *
                                _spaceWalkObj.Quaternion;
                            _spaceWalkObj.Quaternion = Quaternion.Lerp(_spaceWalkObj.Quaternion, lookRotation,
                                Game.LastFrameTime * 15);
                            _spaceWalkObj.Velocity = Vector3.Zero;

                            // we're returning in this if, so that if we're for some reason not yet playing the animation, we
                            // want to wait for it to start.
                            if (!PlayerPed.IsPlayingAnim("amb@world_human_welding@male@base", "base"))
                            {
                                PlayerPed.Task.PlayAnimation("amb@world_human_welding@male@base", "base", 4.0f,
                                    -4.0f, -1, (AnimationFlags) 49, 0.0f);
                                SpaceWalk_CreateWeldingProp(PlayerPed);
                                return;
                            }

                            // if we've reached the end of the timer, then we're done repairing.
                            if (DateTime.UtcNow > _vehicleRepairTimeout)
                            {
                                // repair the vehicle.
                                PlayerVehicle.Repair();
                                PlayerVehicle.LockStatus = VehicleLockStatus.None;
                                if (VehicleData != null)
                                    foreach (var vehicleDoor in VehicleData.OpenDoorsSpaceWalk)
                                        PlayerVehicle.OpenDoor(vehicleDoor, false, true);

                                // let the player know what he/she's done.
                                //SpaceModLib.NotifyWithGXT("Vehicle ~b~repaired~s~.", true);
                                SpaceWalk_RemoveWeldingProp();
                                // clear the repairing animation.
                                PlayerPed.Task.ClearAnimation("amb@world_human_welding@male@base", "base");
                                // reset the player to the floating sate.
                                PlayerTask = ZeroGTask.SpaceWalk;
                            }
                        }
                    }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(PlayerTask),
                            "The player state specified is out of range, and does not exist.");
                }
            }
            else if (PlayerPed.IsRagdoll || PlayerPed.IsJumpingOutOfVehicle)
            {
                if (Entity.Exists(PlayerVehicle))
                    PlayerVehicle.FreezePosition = true;
            }
        }

        private void SpaceWalk()
        {
            if (Settings.UseSpaceWalk)
            {
                // make sure that we're floating first!
                if (!_enteringVehicle)
                    SpaceWalk_Toggle();

                // if the last vehicle is null, then there's nothing to do here.
                if (PlayerVehicle != null)
                {
                    // since we're floating already or, "not in a vehicle" technically, we want to stop our vehicle
                    // from moving and allow the payer to re-enter it.
                    PlayerVehicle.LockStatus = VehicleLockStatus.None;
                    PlayerVehicle.FreezePosition = true;
                    SpaceWalk_EnterVehicle(PlayerPed, PlayerVehicle);

                    // we also want to let the player mine stuff, repair stuff, etc.
                    if (!_enteringVehicle)
                        if (PlayerVehicle.IsDamaged || PlayerVehicle.EngineHealth < 1000)
                            SpaceWalk_RepairVehicle(PlayerPed, PlayerVehicle, 8f);
                }

                // we also want to allow the player to mine asteroids!
                SpaceWalk_MineAsteroids(PlayerPed, PlayerVehicle, 5f);
            }
            else
            {
                PlayerPed.Task.ClearAnimation("swimming@first_person", "idle");
            }
        }

        private void StopSpaceWalking()
        {
            DeleteSpaceWalkObject();
            ChangePlayerVehicle();

            PlayerPed.Task.ClearAnimation("swimming@first_person", "idle");
            _enteringVehicle = false;
        }

        private void ChangePlayerVehicle(bool checkModel = false)
        {
            if (Entity.Exists(PlayerPed.CurrentVehicle) && PlayerPed.CurrentVehicle != PlayerVehicle)
            {
                _spaceWalkRope?.Delete();
                RegisteredVehicles.Add(PlayerPed.LastVehicle);
                PlayerVehicle = PlayerPed.CurrentVehicle;
                if (!checkModel || !PlayerVehicle.Model.IsCar)
                    GtsLib.SetVehicleGravity(PlayerVehicle, Info.UseGravity ? Info.GravityLevel : 0f);
            }
            else if (PlayerVehicle != null)
            {
                PlayerVehicle.IsInvincible = false;
                PlayerVehicle.FreezePosition = false;
            }
        }

        private void SetVehicleDrag()
        {
            var v = _spaceVehicles?.VehicleData.Find(
                x => Game.GenerateHash(x.Model) == PlayerVehicle.Model.Hash);
            Game.Player.SetAirDragMultForVehicle(v?.Drag ?? 1.0f);
        }

        private void DeleteSpaceWalkObject()
        {
            _spaceWalkObj?.Delete();
            _spaceWalkObj = null;
        }

        private void SpaceWalk_CreateWeldingProp(Ped ped)
        {
            if (Entity.Exists(_weldingProp))
                return;

            _weldingProp = World.CreateProp("prop_weld_torch", ped.Position, false, false);
            _weldingProp.AttachTo(ped, ped.GetBoneIndex(Bone.SKEL_R_Hand), new Vector3(0.14f, 0.06f, 0f),
                new Vector3(28.0f, -170f, -5.0f));
            _weldPtfx = new LoopedPtfx("core", "ent_anim_welder");
            _weldPtfx.Start(_weldingProp, 1.0f, new Vector3(-0.2f, 0.15f, 0), Vector3.Zero, null);
        }

        private void SpaceWalk_RemoveWeldingProp()
        {
            if (!Entity.Exists(_weldingProp))
                return;

            _weldPtfx.Remove();
            _weldingProp.Delete();
        }

        private void SpaceWalk_MineAsteroids(Ped ped, Vehicle vehicle, float maxDistanceFromObject)
        {
            // make sure the ped isn't in a vehicle. which he shouldn't be, but just in case.
            if (Entity.Exists(vehicle) && ped.IsInVehicle(vehicle)) return;

            // let's start our raycast.
            var ray = World.Raycast(ped.Position, ped.ForwardVector, maxDistanceFromObject, IntersectOptions.Everything,
                ped);
            if (!ray.DitHitEntity) return;

            // now that we have the hit entity, lets check to see if it's a designated mineable object.
            var entHit = ray.HitEntity;

            // this is a registered mineable object.
            if (!MiningProps.Contains(entHit)) return;

            // let's start mining!
            GtsLibNet.DisplayHelpTextWithGxt("SW_MINE");
            Game.DisableControlThisFrame(2, Control.Context);
            if (Game.IsDisabledControlJustPressed(2, Control.Context))
            {
                _minableObject = entHit as Prop;
                _lastMinePos = ray.HitCoords;
                PlayerTask = ZeroGTask.Mine;
            }
        }

        private void SpaceWalk_RepairVehicle(Ped ped, Vehicle vehicle, float maxDistFromVehicle)
        {
            if (ped.IsInVehicle(vehicle)) return;

            var ray = World.Raycast(ped.Position, ped.ForwardVector, maxDistFromVehicle, IntersectOptions.Everything,
                ped);

            if (!ray.DitHitEntity) return;
            var entHit = ray.HitEntity;
            if (entHit.GetType() != typeof(Vehicle))
                return;

            var entVeh = (Vehicle) entHit;
            if (entVeh != vehicle) return;

            GtsLibNet.DisplayHelpTextWithGxt("SW_REPAIR");
            Game.DisableControlThisFrame(2, Control.Context);

            if (!Game.IsDisabledControlJustPressed(2, Control.Context)) return;

            _vehicleRepairTimeout = DateTime.UtcNow + new TimeSpan(0, 0, 0, 0, 5000);
            _vehicleRepairPos = ray.HitCoords;
            _vehicleRepairNormal = ray.SurfaceNormal;
            PlayerTask = ZeroGTask.Repair;
        }

        private void SpaceWalk_EnterVehicle(Ped ped, Vehicle vehicle)
        {
            if (ped.IsInVehicle(vehicle)) return;
            if (_spaceWalkObj == null)
            {
                if (_enteringVehicle) EnterVehicle_Reset(vehicle);
                return;
            }

            var doorPos = vehicle.HasBone("door_dside_f") ? vehicle.GetBoneCoord("door_dside_f") : vehicle.Position;

            var dist = ped.Position.DistanceTo(doorPos);

            var dir = doorPos - _spaceWalkObj.Position;

            if (!_enteringVehicle)
            {
                if (!(dist < 10f)) return;

                Game.DisableControlThisFrame(2, Control.Enter);
                if (Game.IsDisabledControlJustPressed(2, Control.Enter))
                    _enteringVehicle = true;
            }
            else
            {
                if (Game.IsControlJustPressed(2, Control.VehicleAccelerate) ||
                    Game.IsControlJustPressed(2, Control.MoveLeft) ||
                    Game.IsControlJustPressed(2, Control.MoveRight) ||
                    Game.IsControlJustPressed(2, Control.VehicleBrake))
                {
                    _enteringVehicle = false;
                    return;
                }

                var lookRotation = Quaternion.FromToRotation(_spaceWalkObj.ForwardVector, dir.Normalized) *
                                   _spaceWalkObj.Quaternion;
                _spaceWalkObj.Quaternion = Quaternion.Lerp(_spaceWalkObj.Quaternion, lookRotation,
                    Game.LastFrameTime * 15);
                _spaceWalkObj.Velocity = dir.Normalized * 1.5f;
                if (!(ped.Position.DistanceTo(doorPos) < 1.5f) && vehicle.HasBone("door_dside_f")) return;
                EnterVehicle_Reset(vehicle);
                Effects.Start(ScreenEffect.CamPushInNeutral);
            }
        }

        private void EnterVehicle_Reset(Vehicle vehicle)
        {
            PlayerPed.Detach();
            _spaceWalkObj?.Delete();
            _spaceWalkObj = null;
            _spaceWalkRope?.Delete();
            PlayerPed.Task.ClearAllImmediately();
            PlayerPed.SetIntoVehicle(vehicle, VehicleSeat.Driver);
            _enteringVehicle = false;
            if (VehicleData == null)
                return;
            foreach (var vehicleDoor in VehicleData.OpenDoorsSpaceWalk)
                PlayerVehicle.CloseDoor(vehicleDoor, true);
        }

        private void SpaceWalk_Toggle()
        {
            if (_spaceWalkObj == null || !PlayerPed.IsAttachedTo(_spaceWalkObj))
            {
                _spaceWalkObj = World.CreateVehicle(VehicleHash.Panto, PlayerPosition, PlayerPed.Heading);
                if (_spaceWalkObj == null) return;
                var lastPosition = PlayerPed.Position;
                _spaceWalkObj.HasCollision = false;
                _spaceWalkObj.IsVisible = false;
                _spaceWalkObj.HasGravity = false;
                Function.Call(Hash.SET_VEHICLE_GRAVITY, _spaceWalkObj, false);
                PlayerPed.Task.ClearAllImmediately();
                PlayerPed.AttachTo(_spaceWalkObj, 0);
                _spaceWalkObj.Position = lastPosition;
                _spaceWalkObj.Velocity = Vector3.Zero;

                if (Entity.Exists(PlayerVehicle) && VehicleData != null)
                {
                    _spaceWalkRope = World.AddRope(RopeType.Normal, PlayerVehicle.Position, Vector3.Zero,
                        VehicleData.RopeLength,
                        0f, false);
                    var attachmentOffset =
                        _spaceWalkObj.Position -
                        _spaceWalkObj.ForwardVector * 0.2f +
                        _spaceWalkObj.UpVector * 0.2f;
                    _spaceWalkRope.AttachEntities(_spaceWalkObj, attachmentOffset, PlayerVehicle,
                        PlayerVehicle.Position, VehicleData.RopeLength);
                    _spaceWalkRope.ResetLength(true);
                    _spaceWalkRope.ActivatePhysics();

                    foreach (var vehicleDoor in VehicleData.OpenDoorsSpaceWalk)
                        PlayerVehicle.OpenDoor(vehicleDoor, false, true);
                }
                Effects.Start(ScreenEffect.CamPushInNeutral);
            }
            else // and this is when we're floating
            {
                // we always want to be unarmed, because animations don't look right.
                if (PlayerPed.Weapons.Current.Hash != WeaponHash.Unarmed)
                    PlayerPed.Weapons.Select(WeaponHash.Unarmed);

                const string swimmingAnimDict = "swimming@first_person";
                const string swimmingAnimName = "idle";

                if (!PlayerPed.IsPlayingAnim(swimmingAnimDict, swimmingAnimName))
                {
                    PlayerPed.Task.ClearAllImmediately();
                    PlayerPed.Task.PlayAnimation(swimmingAnimDict, swimmingAnimName, 8.0f, -8.0f, -1,
                        (AnimationFlags) 15,
                        0.0f);
                    while (!PlayerPed.IsPlayingAnim(swimmingAnimDict, swimmingAnimName))
                        Script.Yield();
                    Function.Call(Hash.SET_ENTITY_ANIM_CURRENT_TIME, PlayerPed, swimmingAnimDict, swimmingAnimName,
                        0.4f);
                }

                PlayerPed.SetAnimSpeed(swimmingAnimDict, swimmingAnimName, 0.05f);

                if (!_didSpaceWalkTut)
                {
                    GtsLibNet.DisplayHelpTextWithGxt("SPACEWALK_INFO");
                    Core.Instance.Settings.SetValue("tutorial_info", "did_float_info", _didSpaceWalkTut = true);
                    Core.Instance.Settings.Save();
                }

                EntityFlightControl(_spaceWalkObj, 1f, 1f, !ArtificialCollision(PlayerPed, _spaceWalkObj), 0.5f, true);
            }
        }

        private static bool ArtificialCollision(Entity entity, Entity entityParent, float bounceDamp = 0.25f,
            bool debug = false)
        {
            GetDimensions(entity, out Vector3 min, out Vector3 max, out Vector3 minVector2, out Vector3 maxVector2,
                out float radius);

            var offset = new Vector3(0, 0, radius);
            offset = entity.Quaternion * offset;

            min = entity.GetOffsetInWorldCoords(min);
            max = entity.GetOffsetInWorldCoords(max);

            var bottom = min - minVector2 + offset;
            var middle = (min + max) / 2;
            var top = max - maxVector2 - offset;

            if (debug)
            {
                World.DrawMarker((MarkerType) 28, bottom, Vector3.RelativeFront, Vector3.Zero,
                    new Vector3(radius, radius, radius), Color.FromArgb(120, Color.Blue));
                World.DrawMarker((MarkerType) 28, middle, Vector3.RelativeFront, Vector3.Zero,
                    new Vector3(radius, radius, radius), Color.FromArgb(120, Color.Purple));
                World.DrawMarker((MarkerType) 28, top, Vector3.RelativeFront, Vector3.Zero,
                    new Vector3(radius, radius, radius), Color.FromArgb(120, Color.Orange));
            }

            if (entityParent == null)
                entityParent = entity;

            var ray = World.RaycastCapsule(bottom, (top - bottom).Normalized, (top - bottom).Length(), radius,
                IntersectOptions.Everything, entity);

            if (!ray.DitHitAnything) return false;
            var normal = ray.SurfaceNormal;

            entityParent.Velocity = (normal * entityParent.Velocity.Length() +
                                     (entity.Position - ray.HitCoords)) * bounceDamp;

            return true;
        }

        private static void GetDimensions(Entity entity, out Vector3 min, out Vector3 max, out Vector3 minVector2,
            out Vector3 maxVector2, out float radius)
        {
            entity.Model.GetDimensions(out min, out max);

            var minOffsetV2 = new Vector3(min.X, min.Y, 0);
            var maxOffsetV2 = new Vector3(max.X, max.Y, 0);

            minVector2 = entity.Quaternion * minOffsetV2;
            maxVector2 = entity.Quaternion * maxOffsetV2;
            radius = (minVector2 - maxVector2).Length() / 2.5f;
        }

        private void EntityFlightControl(Entity entity, float flySpeed, float sensitivity, bool canFly = true,
            float rotationMult = 1, bool forceMode = false)
        {
            UI.HideHudComponentThisFrame(HudComponent.WeaponWheel);
            Game.DisableControlThisFrame(2, Control.WeaponWheelLeftRight);
            Game.DisableControlThisFrame(2, Control.WeaponWheelNext);
            Game.DisableControlThisFrame(2, Control.WeaponWheelPrev);
            Game.DisableControlThisFrame(2, Control.WeaponWheelUpDown);
            Game.DisableControlThisFrame(2, Control.SelectWeapon);
            var leftRight = Game.CurrentInputMode == InputMode.MouseAndKeyboard
                ? Game.GetControlNormal(2, Control.MoveLeftRight)
                : Game.GetControlNormal(2, Control.VehicleFlyYawRight) -
                  Game.GetControlNormal(2, Control.VehicleFlyYawLeft);
            var upDown = Game.GetControlNormal(2, Control.VehicleFlyPitchUpDown);
            var roll = Game.GetControlNormal(2, Control.VehicleFlyRollLeftRight);
            var fly = Game.GetControlNormal(2, Control.VehicleFlyThrottleUp);
            var mouseControlNormal = Game.GetControlNormal(0, Control.VehicleFlyMouseControlOverride);

            if (mouseControlNormal > 0)
            {
                leftRight *= sensitivity;
                upDown *= sensitivity;
                roll *= sensitivity;
            }

            const float controlFlightSpeed = 2f;
            _yawSpeed = Mathf.Lerp(_yawSpeed, leftRight, Game.LastFrameTime * controlFlightSpeed);
            _pitchSpeed = Mathf.Lerp(_pitchSpeed, upDown, Game.LastFrameTime * controlFlightSpeed);
            _rollSpeed = Mathf.Lerp(_rollSpeed, roll, Game.LastFrameTime * controlFlightSpeed);
            _verticalSpeed = Mathf.Lerp(_verticalSpeed, fly, Game.LastFrameTime * controlFlightSpeed);

            var leftRightRotation =
                Quaternion.FromToRotation(entity.ForwardVector, entity.RightVector * _yawSpeed);
            var upDownRotation = Quaternion.FromToRotation(entity.ForwardVector,
                entity.UpVector * _pitchSpeed);
            var rollRotation = Quaternion.FromToRotation(entity.RightVector, -entity.UpVector * _rollSpeed);
            var rotation = leftRightRotation * upDownRotation * rollRotation * entity.Quaternion * rotationMult;
            entity.Quaternion = Quaternion.Lerp(entity.Quaternion, rotation, Game.LastFrameTime);

            if (!canFly)
                return;

            if (fly > 0)
            {
                var targetVelocity = entity.ForwardVector.Normalized * flySpeed * _verticalSpeed;
                if (!forceMode) entity.Velocity = Vector3.Lerp(entity.Velocity, targetVelocity, Game.LastFrameTime);
                else entity.ApplyForce(targetVelocity, Vector3.Zero, ForceType.MinForce);
            }
            //else if (reverse > 0)
            //{
            //    entity.Velocity = Vector3.Lerp(entity.Velocity, -entity.ForwardVector * flySpeed,
            //        Game.LastFrameTime);
            //}
        }

        private void UpdateWormHoles()
        {
            foreach (var wormhole in WormHoles)
                UpdateWormHole(wormhole);
        }

        private void UpdateWormHole(Orbital wormHole)
        {
            var data = Info.Orbitals.Find(o => o.Name == wormHole.Name);

            if (data == null)
                return;

            EnterWormHole(wormHole.Position, data);
        }

        private void EnterWormHole(Vector3 wormHolePosition, OrbitalInfo orbitalData)
        {
            var distanceToWormHole = PlayerPosition.DistanceTo(wormHolePosition);
            var escapeDistance = orbitalData.TriggerDistance * 20f;
            var gravitationalPullDistance = orbitalData.TriggerDistance * 15f;

            if (distanceToWormHole <= orbitalData.TriggerDistance)
            {
                Exited?.Invoke(this, orbitalData.NextScene, Vector3.Zero, orbitalData.NextSceneRotation);
            }
            else
            {
                if (!(distanceToWormHole <= escapeDistance)) return;
                if (!GameplayCamera.IsShaking)
                    GameplayCamera.Shake(CameraShake.SkyDiving, 0);
                else GameplayCamera.ShakeAmplitude = 1.5f;

                if (distanceToWormHole > gravitationalPullDistance)
                {
                    var targetDir = wormHolePosition - PlayerPosition;
                    var targetVelocity = targetDir * 50;

                    if (PlayerPed.IsInVehicle())
                        PlayerPed.CurrentVehicle.Velocity = targetVelocity;
                    else PlayerPed.Velocity = targetVelocity;
                }
                else
                {
                    var timeout = DateTime.UtcNow + new TimeSpan(0, 0, 0, 7);
                    while (DateTime.UtcNow < timeout)
                    {
                        var direction = PlayerPosition - wormHolePosition;
                        direction.Normalize();
                        var targetPos = GtsLibNet.RotatePointAroundPivot(PlayerPosition, wormHolePosition,
                            new Vector3(0, 0, 2000 * Game.LastFrameTime));
                        var playerPos = PlayerPed.IsInVehicle()
                            ? PlayerPed.CurrentVehicle.Position
                            : PlayerPosition;
                        var targetVelocity = targetPos - playerPos;
                        if (PlayerPed.IsInVehicle())
                            PlayerPed.CurrentVehicle.Velocity = targetVelocity;
                        else PlayerPed.Velocity = targetVelocity;
                        Script.Yield();
                    }
                    Exited?.Invoke(this, orbitalData.NextScene, orbitalData.NextScenePosition,
                        orbitalData.NextSceneRotation);
                }
            }
        }

        public void KeepInSafeZone()
        {
            if (Info.SurfaceScene) return;
            var dist = PlayerPosition.DistanceTo(Info.GalaxyCenter);
            if (dist < 2500) return;
            _playerSimulatedOffset += PlayerPosition - Info.GalaxyCenter;
            var offset = Info.GalaxyCenter - PlayerPosition;
            var speed = PlayerPed.IsInVehicle() ? PlayerPed.CurrentVehicle.Velocity : PlayerPed.Velocity;
            var camHeading = GameplayCamera.RelativeHeading;
            var camPitch = GameplayCamera.RelativeHeading;
            PlayerPosition = Info.GalaxyCenter;
            GameplayCamera.RelativePitch = camPitch;
            GameplayCamera.RelativeHeading = camHeading;
            foreach (var orbital in Orbitals)
                orbital.Position = orbital.Position + offset;
            foreach (var billboardable in Billboards)
            {
                billboardable.Position = billboardable.Position + offset;
                billboardable.StartPosition = billboardable.StartPosition + offset;
            }
            foreach (var registeredVehicle in RegisteredVehicles)
                if (Entity.Exists(registeredVehicle) && !PlayerPed.IsInVehicle(registeredVehicle))
                    registeredVehicle.Position += offset;
            foreach (var infoSceneLink in Info.SceneLinks)
                infoSceneLink.Position += offset;
            if (PlayerPed.IsInVehicle())
                PlayerPed.CurrentVehicle.Velocity = speed;
            else PlayerPed.Velocity = speed;
        }
    }
}