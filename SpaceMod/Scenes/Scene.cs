using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using GTA;
using GTA.Math;
using GTA.Native;
using GTS.Extensions;
using GTS.Library;
using GTS.OrbitalSystems;
using GTS.Scenarios;
using GTS.Scenes.Interiors;
using GTS.Vehicles;
using GTSCommon;
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
        public const BlipColor MarkerBlipColor = (BlipColor)58;

        /// <summary>
        ///     The texture dictionary used for the reticle.
        /// </summary>
        public const string ReticleTextureDict = "helicopterhud";

        /// <summary>
        ///     The texture used for the reticle.
        /// </summary>
        public const string ReticleTexture = "hud_lock";

        private readonly List<Blip> _blips;
        private readonly List<Interior> _interiors;
        private readonly List<Prop> _minableProps;

        private readonly object _startLock;
        private readonly object _updateLock;
        private readonly List<Vehicle> _vehicles;
        private List<AttachedOrbital> _attachedOrbitals;
        private bool _didDeleteScene;
        private bool _didJump;
        private bool _didRaiseGears;
        private bool _didSetAreaTimecycle;
        private bool _didSetSpaceAudio;
        private bool _didSetTimecycle;
        private bool _didSpaceWalkTut;
        private bool _enteringVehicle;
        private bool _isSpaceVehicleInOrbit;
        private Vector3 _lastMinePos;

        private Prop _minableObject;
        private DateTime _mineTimeout;
        private List<Orbital> _orbitals;
        private float _pitchSpeed;
        private ZeroGTask _playerTask;
        private float _rollSpeed;
        private SpaceVehicleInfo _spaceVehicles;
        private Vehicle _spaceWalkObj;

        private bool _startedMining;
        private Vector3 _vehicleLeavePos;
        private Vector3 _vehicleRepairNormal;
        private Vector3 _vehicleRepairPos;
        private DateTime _vehicleRepairTimeout;
        private float _verticalSpeed;
        private Prop _weldingProp;
        private LoopedPtfx _weldPtfx;

        private float _yawSpeed;

        /// <summary>
        ///     Our standard constructor.
        /// </summary>
        /// <param name="sceneData">The data <see langword="this" /> scene is based off of.</param>
        public Scene(SceneInfo sceneData)
        {
            Info = sceneData;

            _vehicles = new List<Vehicle>();
            _minableProps = new List<Prop>();
            Surfaces = new List<Surface>();
            Scenarios = new List<Scenario>();
            _interiors = new List<Interior>();
            _blips = new List<Blip>();
            _spaceVehicles = new SpaceVehicleInfo();

            _startLock = new object();
            _updateLock = new object();
        }

        /// <summary>
        ///     The <see cref="SceneInfo" /> file that was deserialized.
        /// </summary>
        public SceneInfo Info { get; }

        /// <summary>
        ///     The wormholes in this scene.
        /// </summary>
        public List<Orbital> WormHoles { get; private set; }

        /// <summary>
        ///     The surfaces in this scene.
        /// </summary>
        public List<Surface> Surfaces { get; private set; }

        /// <summary>
        ///     The orbital system of this scene.
        /// </summary>
        public Skybox Skybox { get; private set; }

        /// <summary>
        ///     These are objects that always rotate towards the camera.
        /// </summary>
        public List<Billboardable> Billboards { get; private set; }

        /// <summary>
        ///     The scenarios loaded for the current scene. These are
        ///     <see langword="internal" /> so that the integrity of another persons
        ///     custom scenario cannot be redacted.
        /// </summary>
        public List<Scenario> Scenarios { get; }

        /// <summary>
        ///     The filename of this scene.
        /// </summary>
        public string FileName { get; internal set; }

        internal Vehicle PlayerVehicle { get; private set; }

        internal static Ped PlayerPed => Game.Player.Character ?? new Ped(0);

        internal Vector3 PlayerPosition {
            get => PlayerPed.IsInVehicle() ? PlayerPed.CurrentVehicle.Position : PlayerPed.Position;
            set {
                if (PlayerPed.IsInVehicle())
                    PlayerPed.CurrentVehicle.Position = value;
                else PlayerPed.Position = value;
            }
        }

        public event OnSceneExitEvent Exited;

        internal void Start()
        {
            lock (_startLock)
            {
                if (_didDeleteScene)
                    return;
                Function.Call(Hash._LOWER_MAP_PROP_DENSITY, true);
                GtsLibNet.RemoveAllIpls(true);
                GetSpaceVehicles();
                CreateSpace();
                CreateInteriors();
                CreateTeleports();
                RefreshTimecycle();
                GtsLibNet.SetGravityLevel(Info.UseGravity ? Info.GravityLevel : 0f);
                CreateScenarios();
                ConfigureVehicleForScene();
                ResetPlayerPosition();
                _didSpaceWalkTut = Core.Instance.Settings.GetValue("tutorial_info", "did_float_info", _didSpaceWalkTut);
                Function.Call(Hash.STOP_AUDIO_SCENES);
                Function.Call(Hash.START_AUDIO_SCENE, "CREATOR_SCENES_AMBIENCE");
                GameplayCamera.RelativeHeading = 0;
                Function.Call(Hash.SET_CLOCK_TIME, Info.Time, Info.TimeMinutes, 0);
                Function.Call(Hash.PAUSE_CLOCK, true);
                Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, Info.WeatherName);
                Game.MissionFlag = true;
            }
        }

        internal void Update()
        {
            if (!Monitor.TryEnter(_updateLock)) return;
            try
            {
                if (_didDeleteScene)
                    return;
                var viewFinderPosition = Database.ViewFinderPosition();
                ConfigureRendering();
                ConfigureWeather();
                UpdateAudio();
                SettingsUpdate();
                if (Entity.Exists(Skybox))
                    Skybox.Position = viewFinderPosition;
                UpdateOrbitals(viewFinderPosition);
                SpaceWalkTask();
                PilotVehicle();
                HandleDeath();
                TryToExitScene();
                HandlePlayerVehicle();
                UpdateInteriorTeleports();
                UpdateSurfaceTiles();
                UpdateBillboards(viewFinderPosition);
                UpdateWormHoles();
                LowerPedAndVehicleDensity();
                Scenarios?.ForEach(scenario => scenario.Update());
            }
            finally
            {
                Monitor.Exit(_updateLock);
            }
        }

        internal void Delete(bool aborted = false)
        {
            lock (_updateLock)
            {
                try
                {
                    _didDeleteScene = true;
                    Skybox.Delete();
                    RemovePreviousVehicles();
                    ResetPlayerVehicle();
                    ClearLists(aborted);
                    ResetGameData();
                }
                catch (Exception e)
                {
                    Debug.Log("Failed to abort: " + e.Message + Environment.NewLine + e.StackTrace);
                }
            }
        }

        private void RemovePreviousVehicles()
        {
            foreach (var v in _vehicles)
                if (Entity.Exists(v) && PlayerVehicle != v)
                    v.Delete();
        }

        private static void ResetGameData()
        {
            GameplayCamera.ShakeAmplitude = 0;
            Function.Call(Hash._0x5E5E99285AE812DB);
            Function.Call(Hash.SET_WIND_SPEED, 1.0f);
            Function.Call(Hash.STOP_AUDIO_SCENES);
            Function.Call(Hash.CLEAR_TIMECYCLE_MODIFIER);
            Function.Call(Hash.SET_STREAMED_TEXTURE_DICT_AS_NO_LONGER_NEEDED, ReticleTextureDict);
            Function.Call(Hash.SET_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 1.0f);
            Function.Call(Hash.SET_PED_DENSITY_MULTIPLIER_THIS_FRAME, 1.0f);
            Function.Call(Hash.SET_SCENARIO_PED_DENSITY_MULTIPLIER_THIS_FRAME, 1.0f);
            Function.Call(Hash.SET_RANDOM_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 1.0f);
            Function.Call(Hash.SET_PARKED_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 1.0f);
            Function.Call(Hash.DISABLE_VEHICLE_DISTANTLIGHTS, false);
            Function.Call(Hash._CLEAR_CLOUD_HAT);
            Function.Call(Hash.PAUSE_CLOCK, false);
            Function.Call(Hash._LOWER_MAP_PROP_DENSITY, false);
            GtsLibNet.RemoveAllIpls(false);
            Game.MissionFlag = false;
        }

        private void ClearLists(bool aborted)
        {
            foreach (var scenario in Scenarios)
            {
                if (aborted)
                {
                    scenario?.OnAborted();
                    continue;
                }
                scenario?.OnEnded(false);
            }

            foreach (var b in _blips)
                b?.Remove();

            foreach (var interior in _interiors)
                interior?.Remove();

            foreach (var s in Surfaces)
                s?.Delete();

            foreach (var billboard in Billboards)
                billboard?.Delete();

            foreach (var orbital in _orbitals)
                orbital?.Delete();

            foreach (var lOrbital in _attachedOrbitals)
                lOrbital?.Delete();
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
        }

        /// <summary>
        ///     Give the <see cref="Prop" /> specified the ability to be mined by the player.
        /// </summary>
        /// <param name="prop"></param>
        public void AddMinableProp(Prop prop)
        {
            if (_minableProps.Contains(prop))
                return;
            _minableProps.Add(prop);
        }

        /// <summary>
        ///     Remove a <see cref="Prop" />'s ability to be mined by the player.
        /// </summary>
        /// <param name="prop"></param>
        public void RemoveMinableProp(Prop prop)
        {
            _minableProps.Remove(prop);
        }

        /// <summary>
        ///     Refresh the timecycle that <see langword="this" /> scene uses.
        /// </summary>
        public void RefreshTimecycle()
        {
            if (!string.IsNullOrEmpty(Info.TimecycleModifier) && Info.TimecycleModifierStrength > 0)
                TimecycleModifier.Set(Info.TimecycleModifier, Info.TimecycleModifierStrength);
            else TimecycleModifier.Clear();
        }

        /// <summary>
        ///     Get an interior from our <see langword="private" /> list by <paramref name="name" />.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Interior GetInterior(string name)
        {
            return _interiors.FirstOrDefault(x => x.Name == name);
        }

        /// <summary>
        ///     Draw a marker at the given <paramref name="position" /> with the name: name, and color: col.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="name"></param>
        /// <param name="col"></param>
        public void DrawMarkerAt(Vector3 position, string name, Color? col = null)
        {
            if (string.IsNullOrEmpty(name))
                return;

            const float scale = 64f;
            const float width = 1f / 1920 / (1f / scale);
            const float height = 1f / 1080 / (1f / scale);

            if (col == null)
                col = ColorTranslator.FromHtml("#8000FF");

            Function.Call(Hash.SET_DRAW_ORIGIN, position.X, position.Y, position.Z, 0);

            Function.Call(Hash.DRAW_SPRITE, ReticleTextureDict, ReticleTexture, 0, 0, width, height, 45f, col.Value.R,
                col.Value.G, col.Value.B, col.Value.A);

            Function.Call(Hash.SET_TEXT_FONT, (int)Font.ChaletComprimeCologne);
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

            var orbital = new AttachedOrbital(prop, data.Position, data.Rotation)
            {
                FreezeX = data.FreezeXCoord,
                FreezeY = data.FreezeYCoord,
                FreezeZ = data.FreezeZCoord
            };

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

            var orbital = new Orbital(prop, data.Name, data.RotationSpeed)
            {
                WormHole = data.WormHole
            };

            if (!string.IsNullOrEmpty(data.Name))
            {
                var blip = orbital.AddBlip();
                blip.Sprite = (BlipSprite)288;
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

                var type = assembly.GetType(scenarioInfo.Namespace);

                if (type == null || type.BaseType != typeof(Scenario))
                    continue;

                var instance = (Scenario)Activator.CreateInstance(type);

                instance.OnAwake();

                if (instance.IsScenarioComplete())
                    continue;

                Debug.Log("Creating Scenario: " + type.Name);

                Scenarios.Add(instance);
            }

            foreach (var scenario in Scenarios)
            {
                scenario.OnStart();

                scenario.Completed += OnScenarioComplete;
            }
        }

        private void CreateLink(Link sceneLink)
        {
            if (string.IsNullOrEmpty(sceneLink.Name)) return;
            var blip = World.CreateBlip(Info.GalaxyCenter + sceneLink.Position);
            blip.Sprite = (BlipSprite)178;
            blip.Color = MarkerBlipColor;
            blip.Name = sceneLink.Name;
            _blips.Add(blip);
        }

        private void OnScenarioComplete(Scenario scenario, bool success)
        {
            Scenarios.Remove(scenario);
            _didSetSpaceAudio = false;
        }

        private void UpdateBillboards(Vector3 viewFinderPosition)
        {
            foreach (var billboardable in Billboards)
            {
                var pos = Info.GalaxyCenter + billboardable.StartPosition;
                var startDist = billboardable.ParallaxStartDistance;
                var d = viewFinderPosition.DistanceTo(pos);
                if (d < startDist && d > 1000)
                {
                    var m = (startDist - d) * billboardable.ParallaxAmount;
                    billboardable.Position = pos - billboardable.ForwardVector * m;
                }
                else
                {
                    billboardable.Position = pos;
                }

                var aDir = billboardable.ForwardVector;
                var bDir = billboardable.Position - viewFinderPosition;
                if (bDir.Length() > 0)
                    bDir.Normalize();
                billboardable.Quaternion = Quaternion.Lerp(billboardable.Quaternion,
                    Quaternion.FromToRotation(aDir, bDir) * billboardable.Quaternion, Game.LastFrameTime * 5);
            }
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

            _orbitals = Info.Orbitals?.Select(CreateOrbital).Where(o => o != default(Orbital)).ToList();

            _attachedOrbitals = Info.AttachedOrbitals?.Select(CreateAttachedOrbital)
                .Where(o => o != default(AttachedOrbital)).ToList();

            Surfaces = Info.Surfaces?.Select(CreateSurface).Where(o => o != default(Surface)).ToList();

            Billboards = Info.Billboards
                ?.Select(
                    x => new Billboardable(CreateProp(Info.GalaxyCenter + x.Position, x.Model).Handle, x.Position)
                    {
                        ParallaxAmount = x.ParallaxAmount,
                        ParallaxStartDistance = x.ParallaxStartDistance
                    }).ToList();

            WormHoles = _orbitals?.Where(x => x.WormHole).ToList();

            Skybox = new Skybox(skybox ?? new Prop(0));

            Info.SceneLinks.ForEach(CreateLink);
        }

        private void CreateInteriors()
        {
            RequestInteriors();

            if (Info.Interiors.Any())
                Debug.Log($"Created {_interiors.Count}/{Info.Interiors.Count} interiors.");
        }

        private void RequestInteriors()
        {
            foreach (var interiorInfo in Info.Interiors)
            {
                var interior = new Interior(interiorInfo.Name, interiorInfo.Type);
                interior.Request();
                _interiors.Add(interior);
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

                    _blips.Add(blipStart);
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
                    while (!Function.Call<bool>(Hash._0x49733E92263139D1, vehicle.Handle, 5.0f) && DateTime.Now < timeout)
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
                    _blips.Add(b);
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

            foreach (var o in _orbitals)
            {
                if (Math.Abs(o.RotationSpeed) > 0.00001f)
                    o.Quaternion = Quaternion.Lerp(o.Quaternion,
                        Quaternion.FromToRotation(o.ForwardVector, o.RightVector) * o.Quaternion,
                        Game.LastFrameTime * o.RotationSpeed);

                if (!Settings.ShowCustomGui)
                    continue;

                DrawMarkerAt(o.Position, o.Name);
            }

            foreach (var a in _attachedOrbitals)
            {
                var pos = viewFinderPosition + a.AttachOffset;
                if (a.FreezeX)
                    pos.X = Info.GalaxyCenter.X + a.AttachOffset.X;
                if (a.FreezeY)
                    pos.Y = Info.GalaxyCenter.Y + a.AttachOffset.Y;
                if (a.FreezeZ)
                    pos.Z = Info.GalaxyCenter.Z + a.AttachOffset.Z;
                a.Position = pos;
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

            //if (PlayerPed.IsInVehicle() && CanDoOrbitLanding())
            //    if (PlayerVehicle != PlayerPed.CurrentVehicle)
            //    {
            //        PlayerVehicle = PlayerPed.CurrentVehicle;
            //        if (!PlayerPed.CurrentVehicle.Model.IsPlane)
            //            GtsLib.SetVehicleGravity(PlayerVehicle, Info.GravityLevel);
            //    }

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
                _vehicles.Add(PlayerVehicle);
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
            foreach (var orbital in Info.Orbitals)
            {
                if (string.IsNullOrEmpty(orbital?.NextScene))
                    continue;

                var position = Info.GalaxyCenter + orbital.Position;
                var distance = Vector3.DistanceSquared(PlayerPosition, position);

                if (Settings.DebugTriggers)
                    World.DrawMarker(MarkerType.DebugSphere, position, Vector3.Zero, Vector3.Zero,
                        new Vector3(orbital.TriggerDistance, orbital.TriggerDistance, orbital.TriggerDistance),
                        Color.FromArgb(150, 255, 0, 0));

                if (!(distance <= orbital.TriggerDistance * orbital.TriggerDistance)) continue;
                Exited?.Invoke(this, orbital.NextScene, orbital.NextScenePosition, orbital.NextSceneRotation);
                break;
            }

            foreach (var link in Info.SceneLinks)
            {
                if (string.IsNullOrEmpty(link?.NextScene))
                    continue;

                var position = Info.GalaxyCenter + link.Position;
                var distance = Vector3.DistanceSquared(PlayerPosition, position);

                if (Settings.DebugTriggers)
                    World.DrawMarker(MarkerType.DebugSphere, position, Vector3.Zero, Vector3.Zero,
                        new Vector3(link.TriggerDistance, link.TriggerDistance, link.TriggerDistance),
                        Color.FromArgb(150, 255, 255, 0));

                if (!(distance <= link.TriggerDistance * link.TriggerDistance)) continue;
                Exited?.Invoke(this, link.NextScene, link.NextScenePosition, link.NextSceneRotation);
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
                !PlayerVehicle.IsOnAllWheels, v?.RotationMultiplier ?? 1);
        }

        private void SpaceWalkTask()
        {
            if (Info.SurfaceScene) return;
            if (PlayerPed.IsInVehicle())
            {
                StopSpaceWalking();
                Game.DisableControlThisFrame(2, Control.VehicleExit);
                if (!Game.IsDisabledControlJustPressed(2, Control.VehicleExit)) return;
                PlayerVehicle.FreezePosition = true;
                PlayerVehicle.IsPersistent = true;
                PlayerPed.Task.WarpOutOfVehicle(PlayerVehicle);
            }
            // here's where we're in space without a vehicle.
            else if (!PlayerPed.IsRagdoll && !PlayerPed.IsJumpingOutOfVehicle)
            {
                switch (_playerTask)
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

                                _playerTask = ZeroGTask.SpaceWalk;
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
                                        -4.0f, -1, (AnimationFlags)49, 0.0f);
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
                                    _playerTask = ZeroGTask.SpaceWalk;
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
                                _playerTask = ZeroGTask.SpaceWalk;
                                return;
                            }

                            // If we decide to move in another direction, let's cancel.
                            if (Game.IsControlJustPressed(2, Control.VehicleAccelerate) ||
                                Game.IsControlJustPressed(2, Control.MoveLeft) ||
                                Game.IsControlJustPressed(2, Control.MoveRight) ||
                                Game.IsControlJustPressed(2, Control.VehicleBrake))
                            {
                                _playerTask = ZeroGTask.SpaceWalk;
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
                                        -4.0f, -1, (AnimationFlags)49, 0.0f);
                                    SpaceWalk_CreateWeldingProp(PlayerPed);
                                    return;
                                }

                                // if we've reached the end of the timer, then we're done repairing.
                                if (DateTime.UtcNow > _vehicleRepairTimeout)
                                {
                                    // repair the vehicle.
                                    PlayerVehicle.Repair();

                                    // let the player know what he/she's done.
                                    //SpaceModLib.NotifyWithGXT("Vehicle ~b~repaired~s~.", true);
                                    SpaceWalk_RemoveWeldingProp();

                                    // clear the repairing animation.
                                    PlayerPed.Task.ClearAnimation("amb@world_human_welding@male@base", "base");

                                    // reset the player to the floating sate.
                                    _playerTask = ZeroGTask.SpaceWalk;
                                }
                            }
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(_playerTask),
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

        private void ChangePlayerVehicle()
        {
            if (Entity.Exists(PlayerPed.CurrentVehicle) && PlayerPed.CurrentVehicle != PlayerVehicle)
            {
                PlayerVehicle = PlayerPed.CurrentVehicle;
                if (!PlayerPed.CurrentVehicle.Model.IsPlane)
                    GtsLib.SetVehicleGravity(PlayerVehicle, Info.UseGravity ? Info.GravityLevel : 0f);
            }
            else if (PlayerVehicle != null)
            {
                PlayerVehicle.IsInvincible = false;
                PlayerVehicle.FreezePosition = false;
            }
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
            if (!_minableProps.Contains(entHit)) return;

            // let's start mining!
            GtsLibNet.DisplayHelpTextWithGxt("SW_MINE");
            Game.DisableControlThisFrame(2, Control.Context);
            if (Game.IsDisabledControlJustPressed(2, Control.Context))
            {
                _minableObject = entHit as Prop;
                _lastMinePos = ray.HitCoords;
                _playerTask = ZeroGTask.Mine;
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

            var entVeh = (Vehicle)entHit;
            if (entVeh != vehicle) return;

            GtsLibNet.DisplayHelpTextWithGxt("SW_REPAIR");
            Game.DisableControlThisFrame(2, Control.Context);

            if (!Game.IsDisabledControlJustPressed(2, Control.Context)) return;

            _vehicleRepairTimeout = DateTime.UtcNow + new TimeSpan(0, 0, 0, 0, 5000);
            _vehicleRepairPos = ray.HitCoords;
            _vehicleRepairNormal = ray.SurfaceNormal;
            _playerTask = ZeroGTask.Repair;
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

                // I removed your DateTime code since there is no point for it
                // since that code is never run if you are in a vehicle and 
                // _enteringVehicle is false.

                var lookRotation = Quaternion.FromToRotation(_spaceWalkObj.ForwardVector, dir.Normalized) *
                                   _spaceWalkObj.Quaternion;

                _spaceWalkObj.Quaternion = Quaternion.Lerp(_spaceWalkObj.Quaternion, lookRotation,
                    Game.LastFrameTime * 15);

                _spaceWalkObj.Velocity = dir.Normalized * 1.5f;

                if (!(ped.Position.DistanceTo(doorPos) < 1.5f) && vehicle.HasBone("door_dside_f")) return;

                EnterVehicle_Reset(vehicle);
            }
        }

        private void EnterVehicle_Reset(Vehicle vehicle)
        {
            PlayerPed.Detach();
            _spaceWalkObj?.Delete();
            _spaceWalkObj = null;
            PlayerPed.Task.ClearAllImmediately();
            PlayerPed.SetIntoVehicle(vehicle, VehicleSeat.Driver);
            _enteringVehicle = false;
        }

        private void SpaceWalk_Toggle()
        {
            if (_spaceWalkObj == null || !PlayerPed.IsAttachedTo(_spaceWalkObj))
            {
                _spaceWalkObj = World.CreateVehicle(VehicleHash.Panto, Vector3.Zero, PlayerPed.Heading);
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
                        (AnimationFlags)15,
                        0.0f);
                }

                PlayerPed.SetAnimSpeed(swimmingAnimDict, swimmingAnimName, 0.1f);

                if (!_didSpaceWalkTut)
                {
                    GtsLibNet.DisplayHelpTextWithGxt("");
                    Core.Instance.Settings.SetValue("tutorial_info", "did_float_info", _didSpaceWalkTut = true);
                    Core.Instance.Settings.Save();
                }

                EntityFlightControl(_spaceWalkObj, 1f, 1f, !ArtificialCollision(PlayerPed, _spaceWalkObj));
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
                World.DrawMarker((MarkerType)28, bottom, Vector3.RelativeFront, Vector3.Zero,
                    new Vector3(radius, radius, radius), Color.FromArgb(120, Color.Blue));
                World.DrawMarker((MarkerType)28, middle, Vector3.RelativeFront, Vector3.Zero,
                    new Vector3(radius, radius, radius), Color.FromArgb(120, Color.Purple));
                World.DrawMarker((MarkerType)28, top, Vector3.RelativeFront, Vector3.Zero,
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
            float rotationMult = 1)
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
            var reverse = Game.GetControlNormal(2, Control.VehicleFlyThrottleDown);
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
                entity.Velocity = Vector3.Lerp(entity.Velocity, targetVelocity, Game.LastFrameTime);
            }
            else if (reverse > 0)
            {
                entity.Velocity = Vector3.Lerp(entity.Velocity, -entity.ForwardVector * flySpeed,
                    Game.LastFrameTime);
            }
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
    }
}