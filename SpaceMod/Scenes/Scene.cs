using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using GTA;
using GTA.Math;
using GTA.Native;
using GTS.Extensions;
using GTS.Library;
using GTS.OrbitalSystems;
using GTS.Scenes.Interiors;
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

    #region Delegates

    /// <summary>
    ///     Called when a <see cref="Scene" /> is exited.
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="newSceneFile"></param>
    /// <param name="exitRotation"></param>
    /// <param name="exitOffset"></param>
    public delegate void OnSceneExitEvent(Scene scene, string newSceneFile, Vector3 exitRotation, Vector3 exitOffset);

    /// <summary>
    ///     Called when an <see langword="object" /> within the scene is "mined".
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="mineableObject"></param>
    public delegate void OnMinedObjectEvent(Scene scene, Prop mineableObject);

    #endregion

    /// <summary>
    ///     A Scene controls in-game logic for player movement, and referential game variables pertaining to
    ///     physics. It also adds props to the game based on it's <see cref="SceneInfo" /> data.
    /// </summary>
    public sealed class Scene
    {
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
            _interiors = new List<Interior>();
            _blips = new List<Blip>();

            _playerTask = ZeroGTask.SpaceWalk;
            _startLock = new object();
            _updateLock = new object();
        }

        #region Fields

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

        #region Misc/Flags

        private Vector3 _lastPlayerPosition;
        private bool _didRaiseGears;
        private bool _didSpaceWalkTut;
        private bool _didJump;
        private bool _didSetTimecycle;
        private bool _didSetAreaTimecycle;
        private bool _didSetSpaceAudio;

        #endregion

        #region Events

        /// <summary>
        /// </summary>
        public event OnSceneExitEvent Exited;

        /// <summary>
        /// </summary>
        public event OnMinedObjectEvent Mined;

        #endregion

        private readonly object _startLock;
        private readonly object _updateLock;

        #region Lists

        private readonly List<Blip> _blips;
        private readonly List<Prop> _minableProps;
        private readonly List<Vehicle> _vehicles;
        private readonly List<Interior> _interiors;

        #endregion

        #region Physics

        private bool _enteringVehicle;
        private float _yawSpeed;
        private float _pitchSpeed;
        private float _rollSpeed;
        private float _verticalSpeed;
        private Entity _spaceWalkDummy;

        #endregion

        #region Tasks

        private ZeroGTask _playerTask;

        private DateTime _vehicleRepairTimeout;
        private DateTime _mineTimeout;

        private Vector3 _vehicleRepairPos;
        private Vector3 _vehicleRepairNormal;
        private Vector3 _lastMinePos;

        private Prop _minableObject;
        private Prop _weldingProp;
        private LoopedPtfx _weldPtfx;

        private bool _startedMining;

        #endregion

        #endregion

        #region Properties

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
        public OrbitalSystem Galaxy { get; private set; }

        /// <summary>
        ///     These are objects that always rotate towards the camera.
        /// </summary>
        public List<Billboardable> Billboards { get; private set; }

        /// <summary>
        ///     The filename of this scene.
        /// </summary>
        public string FileName { get; internal set; }

        /// <summary>
        /// </summary>
        internal Vehicle PlayerVehicle { get; private set; }

        /// <summary>
        /// </summary>
        internal Ped PlayerPed => Game.Player.Character ?? new Ped(0);

        public bool StopTile { get; set; }

        /// <summary>
        /// </summary>
        internal Vector3 PlayerPosition
        {
            get => PlayerPed.IsInVehicle() ? PlayerPed.CurrentVehicle.Position : PlayerPed.Position;
            set
            {
                if (PlayerPed.IsInVehicle())
                    PlayerPed.CurrentVehicle.Position = value;
                else PlayerPed.Position = value;
            }
        }

        #endregion

        #region Functions

        #region Internal

        /// <summary>
        /// </summary>
        internal void Start()
        {
            lock (_startLock)
            {
                CreateSpace();
                CreateInteriors();
                CreateTeleports();
                RefreshTimecycle();

                ConfigurePlayerVehicleForSpace();
                ResetPlayerPosition();

                // Core settings //////////////////////////////////////////////////////////////////////////////////////
                _didSpaceWalkTut = Core.Instance.Settings.GetValue("tutorial_info", "did_float_info", _didSpaceWalkTut);
                ///////////////////////////////////////////////////////////////////////////////////////////////////////

                _lastPlayerPosition = PlayerPosition;
                Function.Call(Hash.STOP_AUDIO_SCENES);
                Function.Call(Hash.START_AUDIO_SCENE, "CREATOR_SCENES_AMBIENCE");
                Utils.SetGravityLevel(Info.UseGravity ? Info.GravityLevel : 0f);
                GameplayCamera.RelativeHeading = 0;
                Utils.RemoveAllIpls(true);
            }
        }

        /// <summary>
        /// </summary>
        internal void Update()
        {
            if (!Monitor.TryEnter(_updateLock)) return;
            try
            {
                ConfigureRendering();
                SettingsUpdate();
                UpdateWormHoles();
                Galaxy.Update();
                PilotPlayer();
                PilotVehicle();
                HandleDeath();
                HandleScene();
                HandlePlayerVehicle();
                HandleTeleports();
                TileTerrain();
                BillboardBillboards();
                ConfigureAudio();
            }
            finally
            {
                Monitor.Exit(_updateLock);
            }
        }

        /// <summary>
        /// </summary>
        internal void Delete(bool newSceneLoading = false)
        {
            lock (_updateLock)
            {
                _spaceWalkDummy?.Delete();

                foreach (var v in _vehicles)
                    if (Entity.Exists(v) && PlayerVehicle != v)
                        v.Delete();

                if (PlayerVehicle != null)
                {
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

                Galaxy.Delete();

                foreach (var b in _blips)
                    b.Remove();

                foreach (var interior in _interiors)
                    interior.Remove();

                foreach (var s in Surfaces)
                    s.Delete();

                foreach (var billboard in Billboards)
                    billboard.Delete();

                GameplayCamera.ShakeAmplitude = 0;

                // Reset waves and wind speed.
                Function.Call(Hash._0x5E5E99285AE812DB);
                Function.Call(Hash.SET_WIND_SPEED, 1.0f);

                if (newSceneLoading) return;
                Function.Call(Hash.STOP_AUDIO_SCENE, "CREATOR_SCENES_AMBIENCE");
                Function.Call(Hash.CLEAR_TIMECYCLE_MODIFIER);
                Function.Call(Hash.SET_STREAMED_TEXTURE_DICT_AS_NO_LONGER_NEEDED, ReticleTextureDict);
                Function.Call(Hash.SET_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 1.0f);
                Function.Call(Hash.SET_PED_DENSITY_MULTIPLIER_THIS_FRAME, 1.0f);
                Function.Call(Hash.SET_SCENARIO_PED_DENSITY_MULTIPLIER_THIS_FRAME, 1.0f);
                Function.Call(Hash.SET_RANDOM_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 1.0f);
                Function.Call(Hash.SET_PARKED_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 1.0f);
                Function.Call(Hash.DISABLE_VEHICLE_DISTANTLIGHTS, false);
                Utils.RemoveAllIpls(false);
            }
        }

        #endregion

        #region Public

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
                TimeCycleModifier.Set(Info.TimecycleModifier, Info.TimecycleModifierStrength);
            else
                TimeCycleModifier.Clear();
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

        #endregion

        #region Private

        #region Utility

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

            var prop = World.CreateProp(model, Vector3.Zero, Vector3.Zero, false, false);

            if (Settings.LowConfigMode)
                prop.LodDistance = -1;

            prop.FreezePosition = true;

            prop.LodDistance = data.LodDistance;

            var orbital = new AttachedOrbital(prop, data.Position, data.Rotation);

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

            var prop = Utils.CreatePropNoOffset(model, Info.GalaxyCenter + data.Position, false);
            prop.Rotation = data.Rotation;

            if (Settings.LowConfigMode)
                RenderForLowConfig(prop);

            prop.FreezePosition = true;

            prop.LodDistance = data.LodDistance;

            var orbital = new Orbital(prop, data.Name, data.RotationSpeed)
            {
                WormHole = data.WormHole
            };

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

            Debug.Log($"Successfully loaded model: {data.Model}");

            var prop = Utils.CreatePropNoOffset(model, Info.GalaxyCenter + data.Position, false);

            prop.FreezePosition = true;

            prop.LodDistance = data.LodDistance;

            if (Settings.LowConfigMode)
                RenderForLowConfig(prop);

            var surface = new Surface(prop, data.Tile, data.TileSize);

            surface.GenerateTerrain();

            model.MarkAsNoLongerNeeded();

            return surface;
        }

        private Prop CreateProp(Vector3 position, string modelName)
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

            if (Settings.LowConfigMode)
                RenderForLowConfig(prop);

            model.MarkAsNoLongerNeeded();

            return prop;
        }

        private static void RenderForLowConfig(Prop prop)
        {
            prop.LodDistance *= 2;
        }

        private Model RequestModel(string modelName, int time = 5)
        {
            var model = new Model(modelName);
            var timout = DateTime.UtcNow + new TimeSpan(0, 0, 0, time);
            model.Request();
            while (!model.IsLoaded)
            {
                if (DateTime.UtcNow > timout)
                    break;
                Script.Yield();
            }
            return model;
        }

        private void ResetPlayerPosition()
        {
            var position = Info.GalaxyCenter;

            if (Info.SurfaceScene)
            {
                var newPosition = Utils.GetGroundHeightRay(position, PlayerPed);
                var timer = DateTime.Now + new TimeSpan(0, 0, 5);

                while (newPosition == Vector3.Zero && DateTime.Now < timer)
                {
                    newPosition = Utils.GetGroundHeightRay(position, PlayerPed);
                    Script.Yield();
                }

                if (newPosition != Vector3.Zero)
                    position = newPosition;
            }

            PlayerPosition = position;
        }

        #endregion

        #region Spawning

        private void BillboardBillboards()
        {
            foreach (var billboardable in Billboards)
            {
                var galaxyPosition = Database.GetGalaxyPosition();
                billboardable.Quaternion = Quaternion.FromToRotation(billboardable.ForwardVector,
                                               (billboardable.Position - galaxyPosition).Normalized) *
                                           billboardable.Quaternion;

                var originalPosition = Info.GalaxyCenter + billboardable.OriginalPosition;
                var distance = galaxyPosition.DistanceTo(originalPosition);
                var scale = (billboardable.ParallaxScaling + 1) * GameplayCamera.FieldOfView / distance;
                billboardable.Position = originalPosition - billboardable.ForwardVector * Math.Max(0, scale - 1);
            }
        }

        private void TileTerrain()
        {
            if (!Info.SurfaceScene) return;

            if (StopTile) return;

            foreach (var surface in Surfaces)
                surface.DoInfiniteTile(PlayerPosition, surface.TileSize);

            if (!Entity.Exists(Galaxy)) return;

            var xDistance = (PlayerPosition.X - _lastPlayerPosition.X) * Info.HorizonRotationMultiplier;
            var yDistance = (PlayerPosition.Y - _lastPlayerPosition.Y) * Info.HorizonRotationMultiplier;
            Galaxy.Quaternion = Quaternion.FromToRotation(Vector3.RelativeRight, Vector3.WorldUp * xDistance) *
                                Quaternion.FromToRotation(Vector3.RelativeFront, Vector3.WorldUp * yDistance) *
                                Galaxy.Quaternion;
            _lastPlayerPosition = PlayerPosition;
        }

        private void CreateSpace()
        {
            var skybox = CreateProp(PlayerPed.Position, Info.SkyboxModel);

            var orbitals = Info.Orbitals?.Select(CreateOrbital).Where(o => o != default(Orbital)).ToList();

            var attachedOrbitals = Info.AttachedOrbitals?.Select(CreateAttachedOrbital)
                .Where(o => o != default(AttachedOrbital)).ToList();

            Surfaces = Info.Surfaces?.Select(CreateSurface).Where(o => o != default(Surface)).ToList();

            Billboards = Info.Billboards
                ?.Select(x => new Billboardable(CreateProp(Info.GalaxyCenter + x.Position, x.Model).Handle, x.Position,
                    x.ParallaxScale)).ToList();

            WormHoles = orbitals?.Where(x => x.WormHole).ToList();

            Galaxy = new OrbitalSystem(skybox ?? new Prop(0), orbitals, attachedOrbitals, -0.3f);

            Info.SceneLinks.ForEach(CreateLink);
        }

        private void CreateInteriors()
        {
            foreach (var interiorInfo in Info.Interiors)
            {
                var interior = new Interior(interiorInfo.Name, interiorInfo.Type);

                interior.Request();

                _interiors.Add(interior);
            }

            Debug.Log("Finished creating interiors.");
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

        private void CreateLink(Link sceneLink)
        {
            if (!string.IsNullOrEmpty(sceneLink.Name))
            {
                var blip = World.CreateBlip(Info.GalaxyCenter + sceneLink.Position);

                blip.Sprite = (BlipSprite) 178;

                blip.Color = MarkerBlipColor;

                blip.Name = sceneLink.Name;

                _blips.Add(blip);
            }
        }

        #endregion

        #region Settings

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

        #endregion

        #region Configure

        private void ConfigureRendering()
        {
            UI.HideHudComponentThisFrame(HudComponent.AreaName);

            Function.Call(Hash.SET_RADAR_AS_INTERIOR_THIS_FRAME);

            DrawMarkers();

            if (Camera.Exists(World.RenderingCamera))
            {
                _didSetTimecycle = false;
            }
            else if (!_didSetTimecycle)
            {
                RefreshTimecycle();
                _didSetTimecycle = true;
            }

            HandleTimecycles();
        }

        private void ConfigurePlayerVehicleForSpace()
        {
            var vehicle = PlayerPed.CurrentVehicle;

            if (!Entity.Exists(vehicle)) return;

            PlayerVehicle = vehicle;
            vehicle.IsInvincible = true;
            vehicle.IsPersistent = true;
            vehicle.LodDistance = 512;

            if (Info.SurfaceScene)
            {
                PlayerPed.Task.ClearAllImmediately();
                vehicle.EngineRunning = false;
                vehicle.Quaternion = Quaternion.Identity;
                vehicle.Rotation = Vector3.Zero;
                vehicle.Heading = 230f;
                vehicle.FreezePosition = true;
                vehicle.Velocity = Vector3.Zero;
                vehicle.Speed = 0;
                vehicle.PositionNoOffset = Info.VehicleSurfaceSpawn + Vector3.WorldUp;
                if (!Function.Call<bool>(Hash._0x49733E92263139D1, vehicle.Handle, 5.0f))
                    Debug.Log("Couldn't place vehicle on ground properly.");
            }
            else
            {
                PlayerPed.CanRagdoll = false;
            }
        }

        private void ConfigureAudio()
        {
            if (Info.SurfaceScene) return;
            if (FollowCam.ViewMode == FollowCamViewMode.FirstPerson)
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

        private void DrawMarkers()
        {
            if (!Settings.ShowCustomGui)
                return;

            if (!Function.Call<bool>(Hash.HAS_STREAMED_TEXTURE_DICT_LOADED, ReticleTextureDict))
            {
                Function.Call(Hash.REQUEST_STREAMED_TEXTURE_DICT, ReticleTextureDict);
                while (!Function.Call<bool>(Hash.HAS_STREAMED_TEXTURE_DICT_LOADED, ReticleTextureDict))
                    Script.Yield();
            }

            foreach (var o in Galaxy.Orbitals)
                DrawMarkerAt(o.Position, o.Name);

            foreach (var l in Info.SceneLinks)
                DrawMarkerAt(Info.GalaxyCenter + l.Position, l.Name);
        }

        #endregion

        #region Handlers

        private void HandleTeleports()
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
                    Utils.DisplayHelpTextWithGxt("PRESS_E");

                    if (Game.IsControlJustReleased(2, Control.Context))
                    {
                        Game.FadeScreenOut(750);

                        Script.Wait(750);

                        PlayerPosition = t.End - Vector3.WorldUp;

                        PlayerPed.Heading = t.EndHeading;

                        GameplayCamera.RelativeHeading = 0;

                        Game.FadeScreenIn(750);
                    }
                }

                var distanceEnd = (t.End - PlayerPosition).LengthSquared();

                if (distanceEnd < distance)
                {
                    Utils.DisplayHelpTextWithGxt("PRESS_E");

                    if (Game.IsControlJustPressed(2, Control.Context))
                    {
                        Game.FadeScreenOut(750);

                        Script.Wait(750);

                        PlayerPosition = t.Start - Vector3.WorldUp;

                        PlayerPed.Heading = t.StartHeading;

                        GameplayCamera.RelativeHeading = 0;

                        Game.FadeScreenIn(750);
                    }
                }
            }
        }

        private void HandlePlayerVehicle()
        {
            if (!Info.SurfaceScene)
            {
                if (!Game.IsLoading && !Game.IsScreenFadedOut)
                    if (!_didRaiseGears && Entity.Exists(PlayerPed.CurrentVehicle))
                    {
                        PlayerPed.CurrentVehicle.LandingGear = VehicleLandingGear.Retracted;
                        _didRaiseGears = true;
                    }
                return;
            }

            if (string.IsNullOrEmpty(Info.NextScene))
                return;

            const float distance = 15 * 2;

            if (PlayerVehicle != null && PlayerPosition.DistanceToSquared(PlayerVehicle.Position) < distance &&
                !PlayerPed.IsInVehicle())
            {
                Utils.DisplayHelpTextWithGxt("RET_ORBIT");

                Game.DisableControlThisFrame(2, Control.Enter);

                if (Game.IsDisabledControlJustPressed(2, Control.Enter))
                    PlayerPed.SetIntoVehicle(PlayerVehicle, VehicleSeat.Driver);
            }

            if (PlayerVehicle != null && PlayerPed.IsInVehicle(PlayerVehicle))
            {
                Exited?.Invoke(this, Info.NextScene, Info.NextSceneRotation, Info.NextScenePosition);
            }
            else if (PlayerPed.IsInVehicle())
            {
                Utils.DisplayHelpTextWithGxt("RET_ORBIT2");

                Game.DisableControlThisFrame(2, Control.Context);

                PlayerPed.CurrentVehicle.IsPersistent = true;

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

            Utils.TerminateScriptByName("respawn_controller");
            Game.Globals[4].SetInt(1);
            Function.Call(Hash._DISABLE_AUTOMATIC_RESPAWN, true);
            Function.Call(Hash.SET_FADE_IN_AFTER_DEATH_ARREST, false);
            Function.Call(Hash.SET_FADE_OUT_AFTER_ARREST, false);
            Function.Call(Hash.SET_FADE_OUT_AFTER_DEATH, false);
            Function.Call(Hash.IGNORE_NEXT_RESTART, true);
        }

        private void HandleScene()
        {
            foreach (var orbital in Info.Orbitals)
            {
                if (string.IsNullOrEmpty(orbital?.NextScene))
                    continue;

                var position = Info.GalaxyCenter + orbital.Position;

                var distance = Vector3.DistanceSquared(PlayerPosition, position);

                if (distance <= orbital.TriggerDistance * orbital.TriggerDistance)
                    Exited?.Invoke(this, orbital.NextScene, orbital.NextSceneRotation, orbital.NextScenePosition);
            }

            foreach (var link in Info.SceneLinks)
            {
                if (string.IsNullOrEmpty(link?.NextScene))
                    continue;

                var position = Info.GalaxyCenter + link.Position;

                var distance = Vector3.DistanceSquared(PlayerPosition, position);

                if (distance <= link.TriggerDistance * link.TriggerDistance)
                    Exited?.Invoke(this, link.NextScene, link.NextSceneRotation, link.NextScenePosition);
            }

            Function.Call(Hash.SET_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0.0f);
            Function.Call(Hash.SET_PED_DENSITY_MULTIPLIER_THIS_FRAME, 0.0f);
            Function.Call(Hash.SET_SCENARIO_PED_DENSITY_MULTIPLIER_THIS_FRAME, 0.0f);
            Function.Call(Hash.SET_RANDOM_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0.0f);
            Function.Call(Hash.SET_PARKED_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 0.0f);
            Function.Call(Hash.DISABLE_VEHICLE_DISTANTLIGHTS, true);
            Function.Call(Hash.SET_WIND_SPEED, Info.WindSpeed);
            Function.Call(Hash._0xB96B00E976BE977F, Info.WaveStrength);
        }

        private void HandleTimecycles()
        {
            var area = Info.TimeCycleAreas.FirstOrDefault(
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
            TimeCycleModifier.Set(area.TimeCycleModifier, area.TimeCycleModifierStrength);
            if (!string.IsNullOrEmpty(area.WeatherName))
                Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, area.WeatherName);
            Function.Call(Hash.SET_CLOCK_TIME, area.Time, area.TimeMinutes);
            _didSetAreaTimecycle = true;
        }

        #endregion

        #region Physics

        private void PilotVehicle()
        {
            if (!Entity.Exists(PlayerVehicle))
                return;

            if (!PlayerPed.IsInVehicle(PlayerVehicle))
                return;

            SpaceWalk_Fly(PlayerVehicle, Settings.VehicleFlySpeed, Settings.MouseControlFlySensitivity,
                !PlayerVehicle.IsOnAllWheels);
        }

        private void PilotPlayer()
        {
            if (Info.SurfaceScene) return;

            // here's when we're flying around and stuff.
            if (PlayerPed.IsInVehicle())
            {
                if (!Entity.Exists(PlayerVehicle) || !PlayerPed.IsInVehicle(PlayerVehicle))
                {
                    PlayerVehicle = PlayerPed.CurrentVehicle;
                    PlayerVehicle.HasGravity = false;
                    Function.Call(Hash.SET_VEHICLE_GRAVITY, PlayerVehicle.Handle, false);
                }

                if (PlayerVehicle.Velocity.Length() > 0.15f)
                {
                    PlayerVehicle.LockStatus = VehicleLockStatus.StickPlayerInside;

                    if (Game.IsControlJustPressed(2, Control.Enter))
                        Utils.DisplayHelpTextWithGxt("GTS_LABEL_10");
                }
                else
                {
                    if (PlayerVehicle.LockStatus == VehicleLockStatus.StickPlayerInside)
                        PlayerVehicle.LockStatus = VehicleLockStatus.None;
                }

                PlayerPed.Task.ClearAnimation("swimming@first_person", "idle");
                _enteringVehicle = false;

                _spaceWalkDummy?.Delete();
                _spaceWalkDummy = null;
            }
            // here's where we're in space without a vehicle.
            else if (!PlayerPed.IsRagdoll && !PlayerPed.IsJumpingOutOfVehicle)
            {
                switch (_playerTask)
                {
                    // this let's us float
                    case ZeroGTask.SpaceWalk:
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
                                PlayerVehicle.Velocity = Vector3.Zero;
                                SpaceWalk_EnterVehicle(PlayerPed, PlayerVehicle);

                                // we also want to let the player mine stuff, repair stuff, etc.
                                if (!_enteringVehicle)
                                    if (PlayerVehicle.IsDamaged || PlayerVehicle.EngineHealth < 1000)
                                        SpaceWalk_RepairVehicle(PlayerPed, PlayerVehicle, 8f);
                            }

                            // we also want to allow the player to mine asteroids!
                            SpaceWalk_MineAsteroids(PlayerPed, PlayerVehicle, 5f);
                        }
                        break;
                    // this let's us mine asteroids.
                    case ZeroGTask.Mine:
                    {
                        if (_minableObject == null || !Entity.Exists(_spaceWalkDummy) || _lastMinePos == Vector3.Zero)
                        {
                            if (Entity.Exists(_spaceWalkDummy))
                                _spaceWalkDummy.Detach();

                            _playerTask = ZeroGTask.SpaceWalk;
                            return;
                        }

                        // attach the player to the mineable object.
                        if (!_startedMining)
                        {
                            var dir = _lastMinePos - _spaceWalkDummy.Position;
                            dir.Normalize();
                            _spaceWalkDummy.Quaternion = Quaternion.FromToRotation(_spaceWalkDummy.ForwardVector, dir) *
                                                         _spaceWalkDummy.Quaternion;
                            _mineTimeout = DateTime.UtcNow + new TimeSpan(0, 0, 0, 5);
                            _spaceWalkDummy.Position = _lastMinePos - dir;
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
                                _spaceWalkDummy.Detach();
                                _spaceWalkDummy.HasCollision = false;
                                _spaceWalkDummy.IsVisible = false;
                                _spaceWalkDummy.HasGravity = false;
                                PlayerPed.IsVisible = true;
                                Function.Call(Hash.SET_VEHICLE_GRAVITY, _spaceWalkDummy, false);
                                Utils.NotifyWithGxt("GTS_LABEL_26");
                                Mined?.Invoke(this, _minableObject);
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
                            _spaceWalkDummy == null ||
                            !_spaceWalkDummy.Exists())
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
                        Vector3 min, max, min2, max2;
                        float radius;
                        GetDimensions(PlayerPed, out min, out max, out min2, out max2, out radius);

                        // make sure we're within distance of the vehicle.
                        if (distance > radius)
                        {
                            // make sure to rotate the fly helper towards the repair point.
                            var dir = _vehicleRepairPos + _vehicleRepairNormal * 0.5f - _spaceWalkDummy.Position;
                            dir.Normalize();
                            var lookRotation = Quaternion.FromToRotation(_spaceWalkDummy.ForwardVector, dir) *
                                               _spaceWalkDummy.Quaternion;
                            _spaceWalkDummy.Quaternion = Quaternion.Lerp(_spaceWalkDummy.Quaternion, lookRotation,
                                Game.LastFrameTime * 5);

                            // now move the fly helper towards the direction of the repair point.
                            _spaceWalkDummy.Velocity = dir * 1.5f;

                            // make sure that we update the timer so that if the time runs out, we will fallback to the floating case.
                            _vehicleRepairTimeout = DateTime.UtcNow + new TimeSpan(0, 0, 0, 5);
                        }
                        else
                        {
                            // since we're in tange of the vehicle we want to start the repair sequence.
                            // we're going to stop the movement of the player, and play the repairing animation.
                            var lookRotation =
                                Quaternion.FromToRotation(_spaceWalkDummy.ForwardVector, -_vehicleRepairNormal) *
                                _spaceWalkDummy.Quaternion;
                            _spaceWalkDummy.Quaternion = Quaternion.Lerp(_spaceWalkDummy.Quaternion, lookRotation,
                                Game.LastFrameTime * 15);
                            _spaceWalkDummy.Velocity = Vector3.Zero;

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
        }

        private bool ArtificialCollision(Entity entity, Entity velocityUser, float bounceDamp = 0.25f,
            bool debug = false)
        {
            Vector3 min, max;
            Vector3 minVector2, maxVector2;
            float radius;

            GetDimensions(entity, out min, out max, out minVector2, out maxVector2, out radius);

            var offset = new Vector3(0, 0, radius);
            offset = PlayerPed.Quaternion * offset;

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

            var ray = World.RaycastCapsule(bottom, (top - bottom).Normalized, (top - bottom).Length(), radius,
                IntersectOptions.Everything, PlayerPed);

            if (!ray.DitHitAnything) return false;
            var normal = ray.SurfaceNormal;

            if (velocityUser != null)
                velocityUser.Velocity = (normal * velocityUser.Velocity.Length() + (entity.Position - ray.HitCoords)) *
                                        bounceDamp;

            return true;
        }

        private void GetDimensions(Entity entity, out Vector3 min, out Vector3 max, out Vector3 minVector2,
            out Vector3 maxVector2, out float radius)
        {
            entity.Model.GetDimensions(out min, out max);

            var minOffsetV2 = new Vector3(min.X, min.Y, 0);
            var maxOffsetV2 = new Vector3(max.X, max.Y, 0);

            minVector2 = PlayerPed.Quaternion * minOffsetV2;
            maxVector2 = PlayerPed.Quaternion * maxOffsetV2;
            radius = (minVector2 - maxVector2).Length() / 2.5f;
        }

        #endregion

        #region SpaceWalk

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
            Utils.DisplayHelpTextWithGxt("SW_MINE");
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

            var entVeh = (Vehicle) entHit;
            if (entVeh != vehicle) return;

            Utils.DisplayHelpTextWithGxt("SW_REPAIR");
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

            var doorPos = vehicle.HasBone("door_dside_f") ? vehicle.GetBoneCoord("door_dside_f") : vehicle.Position;

            var dist = ped.Position.DistanceTo(doorPos);

            var dir = doorPos - _spaceWalkDummy.Position;

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

                var lookRotation = Quaternion.FromToRotation(_spaceWalkDummy.ForwardVector, dir.Normalized) *
                                   _spaceWalkDummy.Quaternion;

                _spaceWalkDummy.Quaternion = Quaternion.Lerp(_spaceWalkDummy.Quaternion, lookRotation,
                    Game.LastFrameTime * 15);

                _spaceWalkDummy.Velocity = dir.Normalized * 1.5f;

                if (!(ped.Position.DistanceTo(doorPos) < 1.5f) && vehicle.HasBone("door_dside_f")) return;

                _spaceWalkDummy?.Delete();
                _spaceWalkDummy = null;
                PlayerPed.Detach();
                PlayerPed.Task.ClearAllImmediately();
                PlayerPed.SetIntoVehicle(vehicle, VehicleSeat.Driver);
                _enteringVehicle = false;
            }
        }

        private void SpaceWalk_Fly(Entity entityToFly, float flySpeed, float sensitivity, bool canFly = true)
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

            _yawSpeed = Mathf.Lerp(_yawSpeed, leftRight, Game.LastFrameTime * .7f);
            _pitchSpeed = Mathf.Lerp(_pitchSpeed, upDown, Game.LastFrameTime * 5);
            _rollSpeed = Mathf.Lerp(_rollSpeed, roll, Game.LastFrameTime * 5);
            _verticalSpeed = Mathf.Lerp(_verticalSpeed, fly, Game.LastFrameTime * 1.3f);

            var leftRightRotation =
                Quaternion.FromToRotation(entityToFly.ForwardVector, entityToFly.RightVector * _yawSpeed);
            var upDownRotation =
                Quaternion.FromToRotation(entityToFly.ForwardVector, entityToFly.UpVector * _pitchSpeed);
            var rollRotation = Quaternion.FromToRotation(entityToFly.RightVector, -entityToFly.UpVector * _rollSpeed);
            var rotation = leftRightRotation * upDownRotation * rollRotation * entityToFly.Quaternion;
            entityToFly.Quaternion = Quaternion.Lerp(entityToFly.Quaternion, rotation, Game.LastFrameTime * 1.3f);

            if (!canFly) return;

            if (fly > 0)
            {
                var targetVelocity = entityToFly.ForwardVector.Normalized * flySpeed * _verticalSpeed;
                entityToFly.Velocity = Vector3.Lerp(entityToFly.Velocity, targetVelocity, Game.LastFrameTime * 5);
            }
            else if (reverse > 0)
            {
                entityToFly.Velocity = Vector3.Lerp(entityToFly.Velocity, Vector3.Zero, Game.LastFrameTime * 2.5f);
            }
        }

        private void SpaceWalk_Toggle()
        {
            // so this is when we're not floating
            if (_spaceWalkDummy == null)
            {
                _spaceWalkDummy = World.CreateVehicle(VehicleHash.Panto, Vector3.Zero, PlayerPed.Heading);
                if (_spaceWalkDummy == null) return;
                var lastPosition = PlayerPed.Position;
                _spaceWalkDummy.HasCollision = false;
                _spaceWalkDummy.IsVisible = false;
                _spaceWalkDummy.HasGravity = false;
                Function.Call(Hash.SET_VEHICLE_GRAVITY, _spaceWalkDummy, false);
                PlayerPed.Task.ClearAllImmediately();
                PlayerPed.AttachTo(_spaceWalkDummy, 0);
                _spaceWalkDummy.Position = lastPosition;
                _spaceWalkDummy.Velocity = Vector3.Zero;
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
                }

                PlayerPed.SetAnimSpeed(swimmingAnimDict, swimmingAnimName, 0.1f);

                if (!_didSpaceWalkTut)
                {
                    Utils.DisplayHelpTextThisFrame(
                        "~BLIP_INFO_ICON~ To rotate your character:~n~Use ~INPUT_VEH_FLY_YAW_LEFT~ ~INPUT_VEH_FLY_YAW_RIGHT~ for left and right.~n~Use ~INPUT_VEH_FLY_ROLL_LR~ for roll.~n~Use ~INPUT_VEH_FLY_PITCH_UD~ for up-down pitch.",
                        "CELL_EMAIL_BCON");
                    Core.Instance.Settings.SetValue("tutorial_info", "did_float_info", _didSpaceWalkTut = true);
                    Core.Instance.Settings.Save();
                }

                SpaceWalk_Fly(_spaceWalkDummy, 1.5f, 1.5f, !ArtificialCollision(PlayerPed, _spaceWalkDummy));
            }
        }

        #endregion

        #region Orbital Updates

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
                Exited?.Invoke(this, orbitalData.NextScene, orbitalData.NextSceneRotation, Vector3.Zero);
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
                        var targetPos = Utils.RotatePointAroundPivot(PlayerPosition, wormHolePosition,
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
                    Exited?.Invoke(this, orbitalData.NextScene, orbitalData.NextSceneRotation, Vector3.Zero);
                }
            }
        }

        #endregion

        #endregion

        #endregion
    }
}