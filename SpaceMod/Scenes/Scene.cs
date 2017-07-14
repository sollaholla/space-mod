﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using GTA;
using GTA.Math;
using GTA.Native;
using GTS.Extensions;
using GTS.Library;
using GTS.OrbitalSystems;
using GTS.Scenes.Interiors;

namespace GTS.Scenes
{
    /// <summary>
    /// A player task type for zero G.
    /// </summary>
    public enum ZeroGTask
    {
        SpaceWalk,
        Mine,
        Repair
    }

    #region Delegates
    public delegate void OnExitEvent(Scene scene, string newSceneFile, Vector3 exitRotation, Vector3 exitOffset);
    public delegate void OnMinedObjectEvent(Scene scene, Prop mineableObject);
    #endregion

    public sealed class Scene
    {
        #region Fields
        public const BlipColor MarkerBlipColor = (BlipColor)27;
        public const string ReticleTextureDict = "helicopterhud";
        public const string ReticleTexture = "hud_lock";

        #region Misc/Flags
        private Vector3 lastPlayerPosition;
        private bool didRaiseGears;
        private bool didSpaceWalkTut;
        private bool didJump = false;
        private bool didSetTimecycle = false;
        #endregion

        #region Events
        public event OnExitEvent Exited;
        public event OnMinedObjectEvent Mined;
        #endregion

        private readonly object startLock;
        private readonly object updateLock;

        #region Lists
        private readonly List<Blip> blips;
        private readonly List<Prop> minableProps;
        private readonly List<Vehicle> vehicles;
        private readonly List<Orbital> terrainTiles;
        private readonly List<Interior> interiors;
        #endregion

        #region Settings
        private Vector3 vehicleSpawn;
        private bool useLowGJumping;
        private float jumpForce = 10.0f;
        private float timeCycleStrength = 1.0f;
        private float interior_vehicleAccelerationMult = 10.0f;
        private float interior_vehicleMaxSpeed = 1000.0f;
        private string timeCycleMod = string.Empty;
        #endregion

        #region Physics
        private bool enteringVehicle;
        private float yawSpeed;
        private float pitchSpeed;
        private float rollSpeed;
        private float verticalSpeed;
        private Entity spaceWalkDummy;
        #endregion

        #region Tasks
        private ZeroGTask playerTask;

        private DateTime vehicleRepairTimeout;
        private DateTime mineTimeout;

        private Vector3 vehicleRepairPos;
        private Vector3 vehicleRepairNormal;
        private Vector3 lastMinePos;

        private Prop minableObject;
        private Prop weldingProp;
        private LoopedPtfx weldPtfx;

        private bool startedMining;
        #endregion

        #endregion

        /// <summary>
        /// Our standard constructor.
        /// </summary>
        /// <param name="sceneData">The data this scene is based off of.</param>
        public Scene(SceneInfo sceneData)
        {
            Info = sceneData;

            vehicles = new List<Vehicle>();
            minableProps = new List<Prop>();
            terrainTiles = new List<Orbital>();
            interiors = new List<Interior>();
            blips = new List<Blip>();

            playerTask = ZeroGTask.SpaceWalk;
            startLock = new object();
            updateLock = new object();
            useLowGJumping = Settings.MoonJump;
        }

        #region Properties
        /// <summary>
        /// The <see cref="Scenes.SceneInfo"/> file that was deserialized.
        /// </summary>
        public SceneInfo Info { get; }

        /// <summary>
        /// The wormholes in this scene.
        /// </summary>
        public List<Orbital> WormHoles  { get; private set; }

        /// <summary>
        /// The orbital system of this scene.
        /// </summary>
        public OrbitalSystem Galaxy     { get; private set; }

        /// <summary>
        /// The filename of this scene.
        /// </summary>
        public string FileName          { get; internal set; }

        internal Weather Weather        { get; private set; }
        internal Vehicle PlayerVehicle  { get; private set; }
        internal int IplCount           { get; private set; }

        internal Ped PlayerPed => Game.Player.Character;
        internal Vector3 PlayerPosition {
            get { return PlayerPed.IsInVehicle() ? PlayerPed.CurrentVehicle.Position : PlayerPed.Position; }
            set {
                if (PlayerPed.IsInVehicle())
                    PlayerPed.CurrentVehicle.Position = value;
                else PlayerPed.Position = value;
            }
        }
        #endregion

        #region Functions

        #region Internal
        internal void Start()
        {
            lock (startLock)
            {
                CreateSpace();
                CreateInteriors();
                CreateTeleports();
                ReadSettings();
                RefreshTimecycle();

                ConfigurePlayerVehicleForSpace();
                ResetPlayerPosition();

                lastPlayerPosition = PlayerPosition;

                Function.Call(Hash.START_AUDIO_SCENE, "CREATOR_SCENES_AMBIENCE");
                Utils.SetGravityLevel(Info.UseGravity ? Info.GravityLevel : 0f);
            }
        }

        internal void Update()
        {
            if (!Monitor.TryEnter(updateLock))
            {
                return;
            }

            try
            {
                ConfigureHUD();
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
            }
            finally
            {
                Monitor.Exit(updateLock);
            }
        }

        internal void Delete()
        {
            lock (updateLock)
            {
                spaceWalkDummy?.Delete();

                foreach (Vehicle v in vehicles)
                {
                    if (Entity.Exists(v) && PlayerVehicle != v)
                    {
                        v.Delete();
                    }
                }

                if (PlayerVehicle != null)
                {
                    PlayerPed.SetIntoVehicle(PlayerVehicle, VehicleSeat.Driver);
                    PlayerVehicle.LockStatus = VehicleLockStatus.None;

                    float heading = PlayerVehicle.Heading;
                    PlayerVehicle.Quaternion = Quaternion.Identity;
                    PlayerVehicle.Heading = heading;
                    PlayerVehicle.Velocity = Vector3.Zero;
                    PlayerVehicle.IsInvincible = false;
                    PlayerVehicle.EngineRunning = true;
                    PlayerVehicle.FreezePosition = false;
                    PlayerVehicle.IsPersistent = true;
                }

                Galaxy.Delete();

                foreach (Blip b in blips)
                {
                    b.Remove();
                }

                foreach (Interior interior in interiors)
                {
                    interior.Remove();
                }

                GameplayCamera.ShakeAmplitude = 0;

                Function.Call(Hash.STOP_AUDIO_SCENE, "CREATOR_SCENES_AMBIENCE");

                Function.Call(Hash.CLEAR_TIMECYCLE_MODIFIER);

                Function.Call(Hash.SET_STREAMED_TEXTURE_DICT_AS_NO_LONGER_NEEDED, ReticleTextureDict);
            }
        }
        #endregion

        #region Public
        public void AddMinableProp(Prop prop)
        {
            if (minableProps.Contains(prop))
                return;
            minableProps.Add(prop);
        }

        public void RemoveMinableProp(Prop prop)
        {
            minableProps.Remove(prop);
        }

        public void RefreshTimecycle()
        {
            if (!string.IsNullOrEmpty(timeCycleMod) && timeCycleStrength > 0)
            {
                TimeCycleModifier.Set(timeCycleMod, timeCycleStrength);
            }
            else
            {
                TimeCycleModifier.Clear();
            }
        }

        public Interior GetInterior(string name)
        {
            return interiors.Find(x => x.Name == name);
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

            Model model = RequestModel(data.Model);

            if (!model.IsLoaded)
            {
                Debug.Log($"Failed to load model: {data.Model}", DebugMessageType.Error);
                return default(AttachedOrbital);
            }

            Debug.Log($"Successfully loaded model: {data.Model}");

            Prop prop = World.CreateProp(model, Vector3.Zero, Vector3.Zero, false, false);

            prop.FreezePosition = true;

            AttachedOrbital orbital = new AttachedOrbital(prop, data.Position);

            model.MarkAsNoLongerNeeded();

            return orbital;
        }

        private Orbital CreateOrbital(OrbitalInfo data, bool surface)
        {
            if (string.IsNullOrEmpty(data.Model))
            {
                Debug.Log("Entity model was not set in the xml.", DebugMessageType.Error);
                return default(Orbital);
            }

            Model model = RequestModel(data.Model);

            if (!model.IsLoaded)
            {
                Debug.Log($"Failed to load model: {data.Model}", DebugMessageType.Error);
                return default(Orbital);
            }

            Debug.Log($"Successfully loaded model: {data.Model}");

            Prop prop = Utils.CreatePropNoOffset(model, Info.GalaxyCenter + data.Position, false);

            prop.FreezePosition = true;

            Orbital orbital = new Orbital(prop, data.Name, data.RotationSpeed)
            {
                WormHole = data.WormHole
            };

            if (surface && data.Tile)
            {
                terrainTiles.Add(orbital);
            }

            if (!string.IsNullOrEmpty(data.Name))
            {
                Blip blip = orbital.AddBlip();
                blip.Sprite = BlipSprite.Crosshair2;
                blip.Color = MarkerBlipColor;
                blip.Name = orbital.Name;
            }

            model.MarkAsNoLongerNeeded();

            return orbital;
        }

        private Prop CreateProp(Vector3 position, string modelName)
        {
            if (string.IsNullOrEmpty(modelName))
            {
                return new Prop(0);
            }

            Model model = RequestModel(modelName);

            if (!model.IsLoaded)
            {
                Debug.Log($"Failed to load model: {modelName}", DebugMessageType.Error);
                return new Prop(0);
            }

            Debug.Log($"Successfully loaded model: {modelName}");

            Prop prop = World.CreateProp(model, Vector3.Zero, Vector3.Zero, false, false);

            prop.Position = position;

            prop.FreezePosition = true;

            model.MarkAsNoLongerNeeded();

            return prop;
        }

        private Model RequestModel(string modelName, int time = 5)
        {
            Model model = new Model(modelName);
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
            Vector3 position = Info.GalaxyCenter;

            if (Info.SurfaceScene)
            {
                Vector3 newPosition = position.MoveToGroundArtificial(PlayerPed);

                if (newPosition != Vector3.Zero)
                    position = newPosition;
            }

            PlayerPosition = position;
        }
        #endregion

        #region Spawning
        private void TileTerrain()
        {
            if (Info.SurfaceScene)
            {
                foreach (Orbital orbital in terrainTiles)
                {
                    orbital.DoInfiniteTile(PlayerPosition, 1024);
                }

                if (Entity.Exists(Galaxy))
                {
                    float xDistance = (PlayerPosition.X - lastPlayerPosition.X) * Info.HorizonRotationMultiplier;
                    float yDistance = (PlayerPosition.Y - lastPlayerPosition.Y) * Info.HorizonRotationMultiplier;
                    Galaxy.Quaternion = Quaternion.FromToRotation(Vector3.RelativeRight, Vector3.WorldUp * xDistance) * Quaternion.FromToRotation(Vector3.RelativeFront, Vector3.WorldUp * yDistance) * Galaxy.Quaternion;
                    lastPlayerPosition = PlayerPosition;
                }
            }
        }

        private void CreateSpace()
        {
            Prop skybox = CreateProp(PlayerPed.Position, Info.SkyboxModel);

            List<Orbital> orbitals = Info.Orbitals?.Select(x => CreateOrbital(x, Info.SurfaceScene)).Where(o => o != default(Orbital)).ToList();

            List<AttachedOrbital> attachedOrbitals = Info.AttachedOrbitals?.Select(CreateAttachedOrbital).Where(o => o != default(AttachedOrbital)).ToList();

            WormHoles = orbitals?.Where(x => x.WormHole).ToList();

            Galaxy = new OrbitalSystem(skybox ?? new Prop(0), orbitals, attachedOrbitals, -0.3f);

            Info.SceneLinks.ForEach(CreateLink);
        }

        private void CreateInteriors()
        {
            foreach (InteriorInfo interiorInfo in Info.Interiors)
            {
                Interior interior = new Interior(interiorInfo.Name, interiorInfo.Type);

                interior.Request();

                interiors.Add(interior);
            }
        }

        private void CreateTeleports()
        {
            foreach (TeleportPoint point in Info.Teleports)
            {
                Blip blipStart = World.CreateBlip(point.Start);

                blipStart.Sprite = BlipSprite.Garage2;

                blipStart.Name = "Teleport";

                blips.Add(blipStart);
            }
        }

        private void CreateLink(Link sceneLink)
        {
            if (!string.IsNullOrEmpty(sceneLink.Name))
            {
                Blip blip = World.CreateBlip(Info.GalaxyCenter + sceneLink.Position);

                blip.Sprite = BlipSprite.Crosshair2;

                blip.Color = MarkerBlipColor;

                blip.Name = sceneLink.Name;

                blips.Add(blip);
            }
        }
        #endregion

        #region Settings
        private void ReadSettings()
        {
            //Extra settings///////////////////////////////////////////////////////////////////////////////////////////////
            ScriptSettings settings = ScriptSettings.Load(Database.PathToScenes + "/" + "ExtraSettings.ini");
            var section = Path.GetFileNameWithoutExtension(FileName);
            Weather = (Weather)settings.GetValue(section, "weather", 1);
            vehicleSpawn = ParseVector3.Read(settings.GetValue(section, "vehicle_surface_spawn"), Settings.DefaultVehicleSpawn);
            jumpForce = settings.GetValue(section, "jump_force_override", jumpForce);
            useLowGJumping = settings.GetValue(section, "low_gravity_jumping", useLowGJumping);
            timeCycleMod = settings.GetValue(section, "time_cycle_mod", timeCycleMod);
            timeCycleStrength = settings.GetValue(section, "time_cycle_strength", timeCycleStrength);
            interior_vehicleAccelerationMult = settings.GetValue(section, "interior_vehicle_acceleration_mult", interior_vehicleAccelerationMult);
            interior_vehicleMaxSpeed = settings.GetValue(section, "interior_vehicle_max_speed", interior_vehicleMaxSpeed);
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////

            // Core settings //////////////////////////////////////////////////////////////////////////////////////
            didSpaceWalkTut = Core.Instance.Settings.GetValue("tutorial_info", "did_float_info", didSpaceWalkTut);
            ///////////////////////////////////////////////////////////////////////////////////////////////////////
        }

        private void SettingsUpdate()
        {
            if (useLowGJumping && Info.SurfaceScene)
            {
                if (!didJump && PlayerPed.IsJumping)
                {
                    PlayerPed.Velocity += PlayerPed.UpVector * jumpForce;

                    didJump = true;
                }
                else if (didJump && !PlayerPed.IsJumping && !PlayerPed.IsInAir)
                {
                    didJump = false;
                }
            }
            else
            {
                didJump = false;
            }

        }
        #endregion

        #region Configure
        private void ConfigureHUD()
        {
            UI.HideHudComponentThisFrame(HudComponent.AreaName);

            Function.Call(Hash.SET_RADAR_AS_INTERIOR_THIS_FRAME);

            DrawMarkers();

            if (Camera.Exists(World.RenderingCamera))
            {
                didSetTimecycle = false;
            }
            else if (!didSetTimecycle)
            {
                RefreshTimecycle();
                didSetTimecycle = true;
            }
        }

        private void ConfigurePlayerVehicleForSpace()
        {
            Vehicle vehicle = PlayerPed.CurrentVehicle;

            if (Entity.Exists(vehicle))
            {
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
                    vehicle.PositionNoOffset = vehicleSpawn + Vector3.WorldUp;
                    Script.Yield();
                    DateTime groundPlacementTimeout = DateTime.UtcNow + new TimeSpan(0, 0, 5);
                    while (!Function.Call<bool>(Hash._0x49733E92263139D1, vehicle.Handle, 5.0f))
                    {
                        if (DateTime.UtcNow > groundPlacementTimeout)
                        {
                            Debug.Log("Couldn't place vehicle on ground properly.", DebugMessageType.Debug);
                            break;
                        }
                        Script.Yield();
                    }
                }
                else PlayerPed.CanRagdoll = false;
                Script.Yield();
            }
        }

        private void DrawMarkers()
        {
            if (!Settings.ShowCustomUi)
            {
                return;
            }

            if (!Function.Call<bool>(Hash.HAS_STREAMED_TEXTURE_DICT_LOADED, ReticleTextureDict))
            {
                Function.Call(Hash.REQUEST_STREAMED_TEXTURE_DICT, ReticleTextureDict);

                while (!Function.Call<bool>(Hash.HAS_STREAMED_TEXTURE_DICT_LOADED, ReticleTextureDict))
                    Script.Yield();
            }

            foreach (Orbital o in Galaxy.Orbitals)
            {
                DrawMarkerAt(o.Position, o.Name);
            }

            foreach (Link l in Info.SceneLinks)
            {
                DrawMarkerAt(Info.GalaxyCenter + l.Position, l.Name);
            }
        }

        private void DrawMarkerAt(Vector3 position, string name)
        {
            if (string.IsNullOrEmpty(name))
                return;

            const float Scale = 64f;
            const float Width = (1f / 1920) / (1f / Scale);
            const float Height = (1f / 1080) / (1f / Scale);

            Function.Call(Hash.SET_DRAW_ORIGIN, position.X, position.Y, position.Z, 0);

            /////////////////////////////////////////////////////////////
            Function.Call(Hash.DRAW_SPRITE, ReticleTextureDict, ReticleTexture, 0, 0, Width, Height, 45f, 128, 0, 128, 200);
            /////////////////////////////////////////////////////////////

            /////////////////////////////////////////////////////////////
            Function.Call(Hash.SET_TEXT_FONT, (int)GTA.Font.Monospace);
            Function.Call(Hash.SET_TEXT_SCALE, 0.3f, 0.3f);
            Function.Call(Hash.SET_TEXT_COLOUR, 128, 0, 128, 200);
            Function.Call(Hash.SET_TEXT_DROPSHADOW, 1, 1, 1, 1, 1);
            Function.Call(Hash.SET_TEXT_EDGE, 1, 1, 1, 1, 205);
            Function.Call(Hash.SET_TEXT_JUSTIFICATION, 0);
            Function.Call(Hash.SET_TEXT_WRAP, 0, Width);
            Function.Call(Hash._SET_TEXT_ENTRY, "CELL_EMAIL_BCON");
            Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, name);
            Function.Call(Hash._DRAW_TEXT, 0f, -0.01f);
            /////////////////////////////////////////////////////////////

            Function.Call(Hash.CLEAR_DRAW_ORIGIN);
        }
        #endregion

        #region Handlers
        private void HandleTeleports()
        {
            Vector3 scale = new Vector3(1, 1, 1) * .3f;
            const float distance = 4;

            foreach (TeleportPoint t in Info.Teleports)
            {
                World.DrawMarker(MarkerType.VerticalCylinder, t.Start - Vector3.WorldUp, Vector3.RelativeRight, Vector3.Zero, scale, Color.Purple);

                World.DrawMarker(MarkerType.VerticalCylinder, t.End - Vector3.WorldUp, Vector3.RelativeRight, Vector3.Zero, scale, Color.Purple);

                //////////////////////////////////////////////////
                // NOTE: Using lengthSquared because it's faster.
                //////////////////////////////////////////////////
                float distanceStart = (t.Start - PlayerPosition).LengthSquared();

                if (distanceStart < distance)
                {
                    Utils.DisplayHelpTextWithGXT("0x81EB34E5");

                    Game.DisableControlThisFrame(2, Control.Context);

                    if (Game.IsDisabledControlJustReleased(2, Control.Context))
                    {
                        Game.FadeScreenOut(750);

                        Script.Wait(750);

                        PlayerPosition = t.End - Vector3.WorldUp;

                        Game.FadeScreenIn(750);
                    }
                }

                float distanceEnd = (t.End - PlayerPosition).LengthSquared();

                if (distanceEnd < distance)
                {
                    Utils.DisplayHelpTextWithGXT("0x5F43EF97");

                    Game.DisableControlThisFrame(2, Control.Context);

                    if (Game.IsDisabledControlJustPressed(2, Control.Context))
                    {
                        Game.FadeScreenOut(750);

                        Script.Wait(750);

                        PlayerPosition = t.Start - Vector3.WorldUp;

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
                {
                    if (!didRaiseGears && Entity.Exists(PlayerPed.CurrentVehicle))
                    {
                        PlayerPed.CurrentVehicle.LandingGear = VehicleLandingGear.Retracted;
                        didRaiseGears = true;
                    }
                }
                return;
            }

            if (string.IsNullOrEmpty(Info.NextScene))
                return;

            const float distance = 15 * 2;

            if (PlayerVehicle != null && PlayerPosition.DistanceToSquared(PlayerVehicle.Position) < distance && !PlayerPed.IsInVehicle())
            {
                Utils.DisplayHelpTextWithGXT("RET_ORBIT");

                Game.DisableControlThisFrame(2, Control.Enter);

                if (Game.IsDisabledControlJustPressed(2, Control.Enter))
                {
                    PlayerPed.SetIntoVehicle(PlayerVehicle, VehicleSeat.Driver);
                }
            }

            if (PlayerVehicle != null && PlayerPed.IsInVehicle(PlayerVehicle))
            {
                Exited?.Invoke(this, Info.NextScene, Info.NextSceneRotation, Info.NextScenePosition);
            }
            else if (PlayerPed.IsInVehicle())
            {
                Utils.DisplayHelpTextWithGXT("RET_ORBIT2");

                Game.DisableControlThisFrame(2, Control.Context);

                PlayerPed.CurrentVehicle.IsPersistent = true;

                if (!Game.IsDisabledControlJustPressed(2, Control.Context)) return;

                vehicles.Add(PlayerVehicle);

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
                Vector3 spawn = Info.GalaxyCenter;
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
            foreach (OrbitalInfo orbital in Info.Orbitals)
            {
                if (orbital == null)
                    continue;

                if (string.IsNullOrEmpty(orbital.NextScene))
                    continue;

                Vector3 position = Info.GalaxyCenter + orbital.Position;

                float distance = Vector3.DistanceSquared(PlayerPosition, position);

                if (distance <= orbital.TriggerDistance * orbital.TriggerDistance)
                {
                    Exited?.Invoke(this, orbital.NextScene, orbital.NextSceneRotation, orbital.NextScenePosition);
                }
            }

            foreach (Link link in Info.SceneLinks)
            {
                if (link == null)
                    continue;

                if (string.IsNullOrEmpty(link.NextScene))
                    continue;

                Vector3 position = Info.GalaxyCenter + link.Position;

                float distance = Vector3.DistanceSquared(PlayerPosition, position);

                if (distance <= link.TriggerDistance * link.TriggerDistance)
                {
                    Exited?.Invoke(this, link.NextScene, link.NextSceneRotation, link.NextScenePosition);
                }
            }
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
                if (!Entity.Exists(PlayerVehicle))
                {
                    PlayerVehicle = PlayerPed.CurrentVehicle;
                    PlayerVehicle.HasGravity = false;
                    Function.Call(Hash.SET_VEHICLE_GRAVITY, PlayerVehicle.Handle, false);
                }

                if (PlayerVehicle.Velocity.Length() > 0.15f)
                {
                    PlayerVehicle.LockStatus = VehicleLockStatus.StickPlayerInside;

                    if (Game.IsControlJustPressed(2, Control.Enter))
                    {
                        Utils.DisplayHelpTextWithGXT("GTS_LABEL_10");
                    }
                }
                else
                {
                    if (PlayerVehicle.LockStatus == VehicleLockStatus.StickPlayerInside)
                    {
                        PlayerVehicle.LockStatus = VehicleLockStatus.None;
                    }
                }

                PlayerPed.Task.ClearAnimation("swimming@base", "idle");
                enteringVehicle = false;
            }
            // here's where we're in space without a vehicle.
            else if (!PlayerPed.IsRagdoll && !PlayerPed.IsJumpingOutOfVehicle)
            {
                switch (playerTask)
                {
                    // this let's us float
                    case ZeroGTask.SpaceWalk:
                        if (Settings.UseSpaceWalk)
                        {
                            // make sure that we're floating first!
                            if (!enteringVehicle)
                            {
                                SpaceWalk_Toggle();
                            }

                            // if the last vehicle is null, then there's nothing to do here.
                            if (PlayerVehicle != null)
                            {
                                // since we're floating already or, "not in a vehicle" technically, we want to stop our vehicle
                                // from moving and allow the payer to re-enter it.
                                PlayerVehicle.LockStatus = VehicleLockStatus.None;
                                PlayerVehicle.Velocity = Vector3.Zero;
                                SpaceWalk_EnterVehicle(PlayerPed, PlayerVehicle);

                                // we also want to let the player mine stuff, repair stuff, etc.
                                if (!enteringVehicle)
                                {
                                    // the vehicle is damaged so let's allow the player to repair it.
                                    if (PlayerVehicle.IsDamaged || PlayerVehicle.EngineHealth < 1000)
                                    {
                                        SpaceWalk_RepairVehicle(PlayerPed, PlayerVehicle, 8f);
                                    }
                                }
                            }

                            // we also want to allow the player to mine asteroids!
                            SpaceWalk_MineAsteroids(PlayerPed, PlayerVehicle, 5f);
                        }
                        break;
                    // this let's us mine asteroids.
                    case ZeroGTask.Mine:
                        {
                            if (minableObject == null || !Entity.Exists(spaceWalkDummy) || lastMinePos == Vector3.Zero)
                            {
                                if (Entity.Exists(spaceWalkDummy))
                                {
                                    spaceWalkDummy.Detach();
                                }

                                playerTask = ZeroGTask.SpaceWalk;
                                return;
                            }

                            // attach the player to the mineable object.
                            if (!startedMining)
                            {
                                var dir = lastMinePos - spaceWalkDummy.Position;
                                dir.Normalize();
                                spaceWalkDummy.Quaternion = Quaternion.FromToRotation(spaceWalkDummy.ForwardVector, dir) * spaceWalkDummy.Quaternion;
                                mineTimeout = DateTime.UtcNow + new TimeSpan(0, 0, 0, 5);
                                spaceWalkDummy.Position = lastMinePos - dir;
                                startedMining = true;
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

                                if (DateTime.UtcNow > mineTimeout)
                                {
                                    PlayerPed.Task.ClearAnimation("amb@world_human_welding@male@base", "base");
                                    SpaceWalk_RemoveWeldingProp();
                                    spaceWalkDummy.Detach();
                                    spaceWalkDummy.HasCollision = false;
                                    spaceWalkDummy.IsVisible = false;
                                    spaceWalkDummy.HasGravity = false;
                                    PlayerPed.IsVisible = true;
                                    Function.Call(Hash.SET_VEHICLE_GRAVITY, spaceWalkDummy, false);
                                    Utils.NotifyWithGXT("GTS_LABEL_26");
                                    Mined?.Invoke(this, minableObject);
                                    lastMinePos = Vector3.Zero;
                                    minableObject = null;
                                    startedMining = false;
                                    playerTask = ZeroGTask.SpaceWalk;
                                }
                            }
                        }
                        break;
                    // this lets us repair stuff.
                    case ZeroGTask.Repair:
                        {
                            // the vehicle repair failed somehow and we need to fallback to the first switch case.
                            if (vehicleRepairPos == Vector3.Zero || vehicleRepairNormal == Vector3.Zero || spaceWalkDummy == null ||
                                !spaceWalkDummy.Exists())
                            {
                                playerTask = ZeroGTask.SpaceWalk;
                                return;
                            }

                            // If we decide to move in another direction, let's cancel.
                            if (Game.IsControlJustPressed(2, Control.VehicleAccelerate) ||
                                Game.IsControlJustPressed(2, Control.MoveLeft) ||
                                Game.IsControlJustPressed(2, Control.MoveRight) ||
                                Game.IsControlJustPressed(2, Control.VehicleBrake))
                            {
                                playerTask = ZeroGTask.SpaceWalk;
                                return;
                            }

                            // get some params for this sequence.
                            float distance = PlayerPosition.DistanceTo(vehicleRepairPos);
                            Vector3 min, max, min2, max2;
                            float radius;
                            GetDimensions(PlayerPed, out min, out max, out min2, out max2, out radius);

                            // make sure we're within distance of the vehicle.
                            if (distance > radius)
                            {
                                // make sure to rotate the fly helper towards the repair point.
                                Vector3 dir = (vehicleRepairPos + (vehicleRepairNormal * 0.5f)) - spaceWalkDummy.Position;
                                dir.Normalize();
                                Quaternion lookRotation = Quaternion.FromToRotation(spaceWalkDummy.ForwardVector, dir) * spaceWalkDummy.Quaternion;
                                spaceWalkDummy.Quaternion = Quaternion.Lerp(spaceWalkDummy.Quaternion, lookRotation, Game.LastFrameTime * 5);

                                // now move the fly helper towards the direction of the repair point.
                                spaceWalkDummy.Velocity = dir * 1.5f;

                                // make sure that we update the timer so that if the time runs out, we will fallback to the floating case.
                                vehicleRepairTimeout = DateTime.UtcNow + new TimeSpan(0, 0, 0, 5);
                            }
                            else
                            {
                                // since we're in tange of the vehicle we want to start the repair sequence.
                                // we're going to stop the movement of the player, and play the repairing animation.
                                Quaternion lookRotation = Quaternion.FromToRotation(spaceWalkDummy.ForwardVector, -vehicleRepairNormal) *
                                                          spaceWalkDummy.Quaternion;
                                spaceWalkDummy.Quaternion = Quaternion.Lerp(spaceWalkDummy.Quaternion, lookRotation, Game.LastFrameTime * 15);
                                spaceWalkDummy.Velocity = Vector3.Zero;

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
                                if (DateTime.UtcNow > vehicleRepairTimeout)
                                {
                                    // repair the vehicle.
                                    PlayerVehicle.Repair();

                                    // let the player know what he/she's done.
                                    //SpaceModLib.NotifyWithGXT("Vehicle ~b~repaired~s~.", true);
                                    SpaceWalk_RemoveWeldingProp();

                                    // clear the repairing animation.
                                    PlayerPed.Task.ClearAnimation("amb@world_human_welding@male@base", "base");

                                    // reset the player to the floating sate.
                                    playerTask = ZeroGTask.SpaceWalk;
                                }
                            }
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("The player state specified is out of range, and does not exist.");
                }
            }
        }

        private bool ArtificialCollision(Entity entity, Entity velocityUser, float bounceDamp = 0.25f, bool debug = false)
        {
            Vector3 min, max;
            Vector3 minVector2, maxVector2;
            float radius;

            GetDimensions(entity, out min, out max, out minVector2, out maxVector2, out radius);

            Vector3 offset = new Vector3(0, 0, radius);
            offset = PlayerPed.Quaternion * offset;

            min = entity.GetOffsetInWorldCoords(min);
            max = entity.GetOffsetInWorldCoords(max);

            Vector3 bottom = min - minVector2 + offset;
            Vector3 middle = (min + max) / 2;
            Vector3 top = max - maxVector2 - offset;

            if (debug)
            {
                World.DrawMarker((MarkerType)28, bottom, Vector3.RelativeFront, Vector3.Zero,
                    new Vector3(radius, radius, radius), Color.FromArgb(120, Color.Blue));
                World.DrawMarker((MarkerType)28, middle, Vector3.RelativeFront, Vector3.Zero,
                    new Vector3(radius, radius, radius), Color.FromArgb(120, Color.Purple));
                World.DrawMarker((MarkerType)28, top, Vector3.RelativeFront, Vector3.Zero,
                    new Vector3(radius, radius, radius), Color.FromArgb(120, Color.Orange));
            }

            RaycastResult ray = World.RaycastCapsule(bottom, (top - bottom).Normalized, (top - bottom).Length(), radius,
                IntersectOptions.Everything, PlayerPed);

            if (!ray.DitHitAnything) return false;
            Vector3 normal = ray.SurfaceNormal;

            if (velocityUser != null)
            {
                velocityUser.Velocity = (normal * velocityUser.Velocity.Length() + (entity.Position - ray.HitCoords)) * bounceDamp;
            }

            return true;
        }

        private void GetDimensions(Entity entity, out Vector3 min, out Vector3 max, out Vector3 minVector2, out Vector3 maxVector2, out float radius)
        {
            entity.Model.GetDimensions(out min, out max);

            Vector3 minOffsetV2 = new Vector3(min.X, min.Y, 0);
            Vector3 maxOffsetV2 = new Vector3(max.X, max.Y, 0);

            minVector2 = PlayerPed.Quaternion * minOffsetV2;
            maxVector2 = PlayerPed.Quaternion * maxOffsetV2;
            radius = (minVector2 - maxVector2).Length() / 2.5f;
        }
        #endregion

        #region SpaceWalk
        private void SpaceWalk_CreateWeldingProp(Ped ped)
        {
            if (Entity.Exists(weldingProp))
                return;

            weldingProp = World.CreateProp("prop_weld_torch", ped.Position, false, false);
            weldingProp.AttachTo(ped, ped.GetBoneIndex(Bone.SKEL_R_Hand), new Vector3(0.14f, 0.06f, 0f), new Vector3(28.0f, -170f, -5.0f));
            weldPtfx = new LoopedPtfx("core", "ent_anim_welder");
            weldPtfx.Start(weldingProp, 1.0f, new Vector3(-0.2f, 0.15f, 0), Vector3.Zero, null);
        }

        private void SpaceWalk_RemoveWeldingProp()
        {
            if (!Entity.Exists(weldingProp))
                return;

            weldPtfx.Remove();
            weldingProp.Delete();
        }

        private void SpaceWalk_MineAsteroids(Ped ped, Vehicle vehicle, float maxDistanceFromObject)
        {
            // make sure the ped isn't in a vehicle. which he shouldn't be, but just in case.
            if (Entity.Exists(vehicle) && ped.IsInVehicle(vehicle)) return;

            // let's start our raycast.
            RaycastResult ray = World.Raycast(ped.Position, ped.ForwardVector, maxDistanceFromObject, IntersectOptions.Everything, ped);
            if (!ray.DitHitEntity) return;

            // now that we have the hit entity, lets check to see if it's a designated mineable object.
            Entity entHit = ray.HitEntity;

            // this is a registered mineable object.
            if (minableProps.Contains(entHit))
            {
                // let's start mining!
                Utils.DisplayHelpTextWithGXT("SW_MINE");
                Game.DisableControlThisFrame(2, Control.Context);
                if (Game.IsDisabledControlJustPressed(2, Control.Context))
                {
                    minableObject = entHit as Prop;
                    lastMinePos = ray.HitCoords;
                    playerTask = ZeroGTask.Mine;
                }
            }
        }

        private void SpaceWalk_RepairVehicle(Ped ped, Vehicle vehicle, float maxDistFromVehicle)
        {
            if (ped.IsInVehicle(vehicle)) return;

            RaycastResult ray = World.Raycast(ped.Position, ped.ForwardVector, maxDistFromVehicle, IntersectOptions.Everything, ped);

            if (!ray.DitHitEntity) return;
            Entity entHit = ray.HitEntity;
            if (entHit.GetType() != typeof(Vehicle))
                return;

            Vehicle entVeh = (Vehicle)entHit;
            if (entVeh != vehicle) return;

            Utils.DisplayHelpTextWithGXT("SW_REPAIR");
            Game.DisableControlThisFrame(2, Control.Context);

            if (Game.IsDisabledControlJustPressed(2, Control.Context))
            {
                vehicleRepairTimeout = DateTime.UtcNow + new TimeSpan(0, 0, 0, 0, 5000);
                vehicleRepairPos = ray.HitCoords;
                vehicleRepairNormal = ray.SurfaceNormal;
                playerTask = ZeroGTask.Repair;
            }
        }

        private void SpaceWalk_EnterVehicle(Ped ped, Vehicle vehicle)
        {
            if (ped.IsInVehicle(vehicle)) return;

            Vector3 doorPos = vehicle.HasBone("door_dside_f") ? vehicle.GetBoneCoord("door_dside_f") : vehicle.Position;

            float dist = ped.Position.DistanceTo(doorPos);

            Vector3 dir = doorPos - spaceWalkDummy.Position;

            if (!enteringVehicle)
            {
                if (dist < 10f)
                {
                    Game.DisableControlThisFrame(2, Control.Enter);

                    if (Game.IsDisabledControlJustPressed(2, Control.Enter))
                    {
                        enteringVehicle = true;
                    }
                }
            }
            else
            {
                if (Game.IsControlJustPressed(2, Control.VehicleAccelerate) ||
                    Game.IsControlJustPressed(2, Control.MoveLeft) ||
                    Game.IsControlJustPressed(2, Control.MoveRight) ||
                    Game.IsControlJustPressed(2, Control.VehicleBrake))
                {
                    enteringVehicle = false;
                    return;
                }

                // I removed your DateTime code since there is no point for it
                // since that code is never run if you are in a vehicle and 
                // _enteringVehicle is false.

                Quaternion lookRotation = Quaternion.FromToRotation(spaceWalkDummy.ForwardVector, dir.Normalized) * spaceWalkDummy.Quaternion;

                spaceWalkDummy.Quaternion = Quaternion.Lerp(spaceWalkDummy.Quaternion, lookRotation, Game.LastFrameTime * 15);

                spaceWalkDummy.Velocity = dir.Normalized * 1.5f;

                if (ped.Position.DistanceTo(doorPos) < 1.5f || !vehicle.HasBone("door_dside_f"))
                {
                    SpaceWalk_DeleteDummy();

                    PlayerPed.Detach();

                    PlayerPed.Task.ClearAllImmediately();

                    PlayerPed.SetIntoVehicle(vehicle, VehicleSeat.Driver);

                    enteringVehicle = false;
                }
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
            float leftRight = Game.CurrentInputMode == InputMode.MouseAndKeyboard ? Game.GetControlNormal(2, Control.MoveLeftRight) : Game.GetControlNormal(2, Control.VehicleFlyYawRight) - Game.GetControlNormal(2, Control.VehicleFlyYawLeft);
            float upDown = Game.GetControlNormal(2, Control.VehicleFlyPitchUpDown);
            float roll = Game.GetControlNormal(2, Control.VehicleFlyRollLeftRight);
            float fly = Game.GetControlNormal(2, Control.VehicleFlyThrottleUp);
            float reverse = Game.GetControlNormal(2, Control.VehicleFlyThrottleDown);
            float mouseControlNormal = Game.GetControlNormal(0, Control.VehicleFlyMouseControlOverride);

            if (mouseControlNormal > 0)
            {
                leftRight *= sensitivity;
                upDown *= sensitivity;
                roll *= sensitivity;
            }

            yawSpeed = Mathf.Lerp(yawSpeed, leftRight, Game.LastFrameTime * .7f);
            pitchSpeed = Mathf.Lerp(pitchSpeed, upDown, Game.LastFrameTime * 5);
            rollSpeed = Mathf.Lerp(rollSpeed, roll, Game.LastFrameTime * 5);
            this.verticalSpeed = Mathf.Lerp(this.verticalSpeed, fly, Game.LastFrameTime * 1.3f);

            Quaternion leftRightRotation = Quaternion.FromToRotation(entityToFly.ForwardVector, entityToFly.RightVector * yawSpeed);
            Quaternion upDownRotation = Quaternion.FromToRotation(entityToFly.ForwardVector, entityToFly.UpVector * pitchSpeed);
            Quaternion rollRotation = Quaternion.FromToRotation(entityToFly.RightVector, -entityToFly.UpVector * rollSpeed);
            Quaternion rotation = leftRightRotation * upDownRotation * rollRotation * entityToFly.Quaternion;
            entityToFly.Quaternion = Quaternion.Lerp(entityToFly.Quaternion, rotation, Game.LastFrameTime * 1.3f);

            if (canFly)
            {
                if (fly > 0)
                {
                    var targetVelocity = entityToFly.ForwardVector.Normalized * flySpeed * this.verticalSpeed;
                    entityToFly.Velocity = Vector3.Lerp(entityToFly.Velocity, targetVelocity, Game.LastFrameTime * 5);
                }
                else if (reverse > 0)
                {
                    entityToFly.Velocity = Vector3.Lerp(entityToFly.Velocity, Vector3.Zero, Game.LastFrameTime * 2.5f);
                }
            }
        }

        private void SpaceWalk_DeleteDummy()
        {
            if (spaceWalkDummy != null)
            {
                if (PlayerPed.IsAttachedTo(spaceWalkDummy))
                {
                    PlayerPed.Detach();
                }

                spaceWalkDummy.Delete();

                spaceWalkDummy = null;
            }
        }

        private void SpaceWalk_Toggle()
        {
            // so this is when we're not floating
            if (spaceWalkDummy == null || !spaceWalkDummy.Exists())
            {
                spaceWalkDummy = World.CreateVehicle(VehicleHash.Panto, PlayerPosition, PlayerPed.Heading);

                if (spaceWalkDummy != null)
                {
                    spaceWalkDummy.HasCollision = false;
                    spaceWalkDummy.IsVisible = false;
                    spaceWalkDummy.HasGravity = false;

                    Function.Call(Hash.SET_VEHICLE_GRAVITY, spaceWalkDummy, false);
                    PlayerPed.Task.ClearAllImmediately();
                    PlayerPed.AttachTo(spaceWalkDummy, 0);

                    spaceWalkDummy.Velocity = Vector3.Zero;


                }
            }
            else // and this is when we're floating
            {
                // we always want to be unarmed, because animations don't look right.
                if (PlayerPed.Weapons.Current.Hash != WeaponHash.Unarmed)
                {
                    PlayerPed.Weapons.Select(WeaponHash.Unarmed);
                }

                // we're not playing the swimming animation yet.
                if (!PlayerPed.IsPlayingAnim("swimming@base", "idle"))
                {
                    PlayerPed.Task.PlayAnimation("swimming@base", "idle", 8.0f, -8.0f, -1,
                        AnimationFlags.Loop,
                        0.0f);

                    DateTime timout = DateTime.UtcNow + new TimeSpan(0, 0, 0, 2);
                    while (!PlayerPed.IsPlayingAnim("swimming@base", "idle"))
                    {
                        Script.Yield();

                        if (DateTime.UtcNow > timout)
                            break;
                    }

                    PlayerPed.SetAnimSpeed("swimming@base", "idle", 0.3f);
                }
                else // now we're playing the swimming animation.
                {
                    if (!didSpaceWalkTut)
                    {
                        Utils.DisplayHelpTextThisFrame("~BLIP_INFO_ICON~ To rotate your character:~n~Use ~INPUT_VEH_FLY_YAW_LEFT~ ~INPUT_VEH_FLY_YAW_RIGHT~ for left and right.~n~Use ~INPUT_VEH_FLY_ROLL_LR~ for roll.~n~Use ~INPUT_VEH_FLY_PITCH_UD~ for up-down pitch.", "CELL_EMAIL_BCON");
                        Core.Instance.Settings.SetValue("tutorial_info", "did_float_info", didSpaceWalkTut = true);
                        Core.Instance.Settings.Save();
                    }

                    SpaceWalk_Fly(spaceWalkDummy, 1.5f, 1.5f, !ArtificialCollision(PlayerPed, spaceWalkDummy));
                }
            }
        }
        #endregion

        #region Orbital Updates
        private void UpdateWormHoles()
        {
            foreach (Orbital wormhole in WormHoles)
            {
                UpdateWormHole(wormhole);
            }
        }

        private void UpdateWormHole(Orbital wormHole)
        {
            OrbitalInfo data = Info.Orbitals.Find(o => o.Name == wormHole.Name);

            if (data == null)
            {
                return;
            }

            EnterWormHole(wormHole.Position, data);
        }

        private void EnterWormHole(Vector3 wormHolePosition, OrbitalInfo orbitalData)
        {
            float distanceToWormHole = PlayerPosition.DistanceTo(wormHolePosition);
            float escapeDistance = orbitalData.TriggerDistance * 20f;
            float gravitationalPullDistance = orbitalData.TriggerDistance * 15f;

            if (distanceToWormHole <= orbitalData.TriggerDistance)
                Exited?.Invoke(this, orbitalData.NextScene, orbitalData.NextSceneRotation, Vector3.Zero);
            else
            {
                if (distanceToWormHole <= escapeDistance)
                {
                    if (!GameplayCamera.IsShaking)
                        GameplayCamera.Shake(CameraShake.SkyDiving, 0);
                    else GameplayCamera.ShakeAmplitude = 1.5f;

                    if (distanceToWormHole > gravitationalPullDistance)
                    {
                        Vector3 targetDir = wormHolePosition - PlayerPosition;
                        Vector3 targetVelocity = targetDir * 50;

                        if (PlayerPed.IsInVehicle())
                            PlayerPed.CurrentVehicle.Velocity = targetVelocity;
                        else PlayerPed.Velocity = targetVelocity;
                    }
                    else
                    {
                        DateTime timeout = DateTime.UtcNow + new TimeSpan(0, 0, 0, 7);
                        while (DateTime.UtcNow < timeout)
                        {
                            Vector3 direction = PlayerPosition - wormHolePosition;
                            direction.Normalize();
                            Vector3 targetPos = Utils.RotatePointAroundPivot(PlayerPosition, wormHolePosition,
                                new Vector3(0, 0, 2000 * Game.LastFrameTime));
                            Vector3 playerPos = PlayerPed.IsInVehicle()
                                ? PlayerPed.CurrentVehicle.Position
                                : PlayerPosition;
                            Vector3 targetVelocity = targetPos - playerPos;
                            if (PlayerPed.IsInVehicle())
                                PlayerPed.CurrentVehicle.Velocity = targetVelocity;
                            else PlayerPed.Velocity = targetVelocity;
                            Script.Yield();
                        }
                        Exited?.Invoke(this, orbitalData.NextScene, orbitalData.NextSceneRotation, Vector3.Zero);
                    }
                }
            }
        }
        #endregion

        #endregion

        #endregion
    }
}
