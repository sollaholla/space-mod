﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using GTA;
using GTA.Math;
using GTA.Native;
using Font = GTA.Font;

namespace SpaceMod.DataClasses
{
    public class CustomScene
    {
        public delegate void OnExitEvent(CustomScene scene, string newSceneFile, Vector3 exitRotation);

        public event OnExitEvent Exited;

        private readonly object _startLock = new object();
        private readonly object _updateLock = new object();

        private float _leftRightFly;
        private float _upDownFly;
        private float _rollFly;
        private float _fly;

        public CustomScene(CustomXmlScene sceneData)
        {
            SceneData = sceneData;
        }

        public Ped PlayerPed => Game.Player.Character;

        public Vector3 PlayerPosition {
            get { return PlayerPed.Position; }
            set { PlayerPed.Position = value; }
        }

        public CustomXmlScene SceneData { get; }

        public List<Orbital> WormHoles { get; private set; }

        public OrbitalSystem OrbitalSystem { get; private set; }

        public List<Tuple<UIText, UIText, Link>> DistanceText { get; private set; }

        public IplData LastIpl { get; private set; }

        public Camera SpaceCamera { get; private set; }

        public Vehicle PlayerLastVehicle { get; private set; }

        public void Start()
        {
            lock (_startLock)
            {
                Prop spaceDome = CreateProp(Vector3.Zero, SceneData.SpaceDomeModel);

                List<Orbital> orbitals =
                    SceneData.Orbitals?.Select(CreateOrbital).Where(o => o != default(Orbital)).ToList();

                List<LockedOrbital> lockedOrbitals =
                    SceneData.LockedOrbitals?.Select(CreateLockedOrbital).Where(o => o != default(LockedOrbital)).ToList();

                WormHoles = orbitals?.Where(x => x.IsWormHole).ToList();



                OrbitalSystem = new OrbitalSystem(spaceDome.Handle, orbitals, lockedOrbitals, -0.3f);

                DistanceText = new List<Tuple<UIText, UIText, Link>>();

                SceneData.SceneLinks.ForEach(link =>
                {
                    if (string.IsNullOrEmpty(link.Name)) return;
                    var text = new UIText(string.Empty, new Point(), 0.5f)
                    {
                        Centered = true,
                        Font = Font.Monospace,
                        Shadow = true
                    };
                    var distanceText = new UIText(string.Empty, new Point(), 0.5f)
                    {
                        Centered = true,
                        Font = Font.Monospace,
                        Shadow = true
                    };
                    var tuple = new Tuple<UIText, UIText, Link>(text, distanceText, link);
                    DistanceText.Add(tuple);
                });

                MovePlayerToGalaxy();

                if (SceneData.SurfaceFlag)
                {
                    Vehicle vehicle = PlayerPed.CurrentVehicle;

                    PlayerPed.Task.ClearAllImmediately();

                    if (vehicle != null && vehicle.Exists())
                    {
                        float heading = vehicle.Heading;

                        vehicle.Quaternion = Quaternion.Identity;

                        vehicle.Rotation = Vector3.Zero;

                        vehicle.Heading = heading;

                        PlayerLastVehicle = vehicle;

                        vehicle.Position = PlayerPosition.Around(15);

                        vehicle.LandingGear = VehicleLandingGear.Deployed;
                    }
                }
                else
                {
                    if (WormHoles.Any())
                    {
                        SpaceCamera = World.CreateCamera(GameplayCamera.Position, GameplayCamera.Rotation,
                            GameplayCamera.FieldOfView);
                        World.RenderingCamera = SpaceCamera;
                    }
                }

                SceneData.Ipls?.ForEach(iplData =>
                {
                    Ipl ipl = new Ipl(iplData.Name, iplData.Type);
                    ipl.Request();
                    iplData.CurrentIpl = ipl;
                });
            }
        }

        public static LockedOrbital CreateLockedOrbital(LockedOrbitalData data)
        {
            if (string.IsNullOrEmpty(data.Model))
            {
                DebugLogger.Log("CreateLockedOrbital::Entity model was not set in the xml.", MessageType.Error);
                return default(LockedOrbital);
            }

            Model model = RequestModel(data.Model);
            if (!model.IsLoaded)
            {
                DebugLogger.Log($"CreateLockedOrbital::Failed to load model: {data.Model}", MessageType.Error);
                return default(LockedOrbital);
            }
            DebugLogger.Log($"CreateLockedOrbital::Successfully loaded model: {data.Model}", MessageType.Debug);
            Prop prop = World.CreateProp(model, Vector3.Zero, Vector3.Zero, false, false);
            prop.FreezePosition = true;
            LockedOrbital orbital = new LockedOrbital(prop.Handle, data.Offset);
            return orbital;
        }

        public Orbital CreateOrbital(OrbitalData data)
        {
            if (string.IsNullOrEmpty(data.Model))
            {
                DebugLogger.Log("CreateOrbital::Entity model was not set in the xml.", MessageType.Error);
                return default(Orbital);
            }

            Model model = RequestModel(data.Model);
            if (!model.IsLoaded)
            {
                DebugLogger.Log($"CreateOrbital::Failed to load model: {data.Model}", MessageType.Error);
                return default(Orbital);
            }
            DebugLogger.Log($"CreateOrbital::Successfully loaded model: {data.Model}", MessageType.Debug);
            Prop prop = World.CreateProp(model, Vector3.Zero, Vector3.Zero, false, false);
            prop.FreezePosition = true;
            prop.Position = (SceneData.SurfaceFlag ? Database.PlanetSurfaceGalaxyCenter : Database.GalaxyCenter) + data.OriginOffset;
            Orbital orbital = new Orbital(prop.Handle, data.Name, null, Vector3.Zero, data.RotationSpeed)
            {
                IsWormHole = data.IsWormHole
            };
            return orbital;
        }

        public static Prop CreateProp(Vector3 position, string modelName)
        {
            if (string.IsNullOrEmpty(modelName)) return default(Prop);
            Model model = RequestModel(modelName);
            if (!model.IsLoaded)
            {
                DebugLogger.Log($"CreateProp::Failed to load model: {modelName}", MessageType.Error);
                return default(Prop);
            }
            DebugLogger.Log($"CreateProp::Successfully loaded model: {modelName}", MessageType.Debug);
            Prop prop = World.CreateProp(model, position, Vector3.Zero, false, false);
            prop.FreezePosition = true;
            return prop;
        }

        public static Model RequestModel(string modelName)
        {
            Model model = new Model(modelName);
            var timout = DateTime.UtcNow + new TimeSpan(0, 0, 0, 5);
            model.Request();
            while (!model.IsLoaded)
            {
                Script.Yield();
                if (DateTime.UtcNow > timout)
                    break;
            }

            return model;
        }

        public void Update()
        {
            if (!Monitor.TryEnter(_updateLock)) return;

            try
            {
                VehicleFly();
                UpdateCamera();

                OrbitalSystem?.Process(Database.GetValidGalaxyDomePosition(PlayerPed));

                DistanceText?.ForEach(text =>
                {
                    var position = Database.GalaxyCenter + text.Item3.OriginOffset;
                    Utilities.ShowUIPosition(null, DistanceText.IndexOf(text) + OrbitalSystem.Orbitals.Count,
                        position, Database.PathToSprites, text.Item3.Name,
                        text.Item1, text.Item2);

                    float distance = Vector3.Distance(position, PlayerPosition);
                    float targetDistance = text.Item3.ExitDistance;
                    if (distance > targetDistance) return;
                    Exited?.Invoke(this, text.Item3.NextSceneFile, text.Item3.ExitRotation);
                });

                TryToStartNextScene();

                if (SceneData.SurfaceFlag && PlayerLastVehicle != null &&
                    !string.IsNullOrEmpty(SceneData.NextSceneOffSurface))
                {
                    if (PlayerPed.IsGettingIntoAVehicle || PlayerPed.IsInVehicle())
                    {
                        Exited?.Invoke(this, SceneData.NextSceneOffSurface, SceneData.SurfaceExitRotation);
                    }
                }

                SceneData.Ipls?.ForEach(UpdateTeleports);

                WormHoles?.ForEach(UpdateWormHole);
            }
            finally
            {
                Monitor.Exit(_updateLock);
            }
        }

        private void UpdateCamera()
        {
            if (SpaceCamera != null)
            {
                if (FollowCam.ViewMode != FollowCamViewMode.FirstPerson && WormHoles.Any())
                {
                    World.RenderingCamera = SpaceCamera;
                    SpaceCamera.Position = GameplayCamera.Position;
                    SpaceCamera.Rotation = GameplayCamera.Rotation;
                    SpaceCamera.FieldOfView = GameplayCamera.FieldOfView;
                }
                else World.RenderingCamera = null;
            }
        }

        private void VehicleFly()
        {
            Vehicle vehicle = PlayerPed.CurrentVehicle;
            if (vehicle == null) return;
            if (!vehicle.Exists()) return;
            if (vehicle.Driver != PlayerPed) return;

            if (!vehicle.IsOnAllWheels)
            {
                float leftRight = Game.GetControlNormal(2, Control.MoveLeftRight);
                float upDown = Game.GetControlNormal(2, Control.VehicleFlyPitchUpDown);
                float roll = Game.GetControlNormal(2, Control.VehicleFlyRollLeftRight);
                float fly = Game.GetControlNormal(2, Control.VehicleFlyThrottleUp);

                _leftRightFly = Mathf.Lerp(_leftRightFly, leftRight, Game.LastFrameTime * 2.5f);
                _upDownFly = Mathf.Lerp(_upDownFly, upDown, Game.LastFrameTime * 5);
                _rollFly = Mathf.Lerp(_rollFly, roll, Game.LastFrameTime * 5);
                _fly = Mathf.Lerp(_fly, fly, Game.LastFrameTime * 5);

                Quaternion leftRightRotation = Quaternion.FromToRotation(vehicle.ForwardVector, vehicle.RightVector * _leftRightFly);
                Quaternion upDownRotation = Quaternion.FromToRotation(vehicle.ForwardVector, vehicle.UpVector * _upDownFly);
                Quaternion rollRotation = Quaternion.FromToRotation(vehicle.RightVector, -vehicle.UpVector * _rollFly);
                Quaternion rotation = leftRightRotation * upDownRotation * rollRotation * vehicle.Quaternion;
                vehicle.Quaternion = Quaternion.Lerp(vehicle.Quaternion, rotation, Game.LastFrameTime * 1.3f);
            }

            var vehicleVelocity = vehicle.ForwardVector * vehicle.Acceleration * _fly * Game.LastFrameTime;
            vehicleVelocity.X = Mathf.Clamp(vehicleVelocity.X, -10, 10);
            vehicleVelocity.Y = Mathf.Clamp(vehicleVelocity.Y, -10, 10);
            vehicleVelocity.Z = Mathf.Clamp(vehicleVelocity.Z, -10, 10);
            vehicle.Velocity += vehicleVelocity;
        }

        private void TryToStartNextScene()
        {
            SceneData.Orbitals?.ForEach(orbital =>
            {
                if (orbital == null) return;
                if (orbital.NextSceneFile == string.Empty) return;
                Vector3 position = Database.GalaxyCenter + orbital.OriginOffset;
                float distance = Vector3.Distance(PlayerPosition, position);
                if (distance > orbital.ExitDistance) return;
                Exited?.Invoke(this, orbital.NextSceneFile, orbital.ExitRotation);
            });
        }

        private void UpdateTeleports(IplData data)
        {
            data.Teleports?.ForEach(teleport =>
            {
                Vector3 start = teleport.Start;
                Vector3 end = teleport.End;

                World.DrawMarker(MarkerType.UpsideDownCone, start, Vector3.RelativeRight, Vector3.Zero, new Vector3(0.5f, 0.5f, 0.5f), Color.Purple);
                World.DrawMarker(MarkerType.UpsideDownCone, end, Vector3.RelativeRight, Vector3.Zero, new Vector3(0.5f, 0.5f, 0.5f), Color.Purple);

                float distanceToStart = Vector3.Distance(PlayerPosition, start);
                float distanceToEnd = Vector3.Distance(PlayerPosition, end);
                if (teleport.EndIpl.CurrentIpl == null)
                {
                    if (distanceToStart < 1.5f && teleport.EndIpl != null)
                    {
                        Utilities.DisplayHelpTextThisFrame("Press ~INPUT_CONTEXT~ to enter.");
                        Game.DisableControlThisFrame(2, Control.Context);
                        if (Game.IsDisabledControlJustPressed(2, Control.Context))
                        {
                            Game.FadeScreenOut(1000);
                            Script.Wait(1000);

                            PlayerPosition = end;

                            DebugLogger.Log($"Creating Ipl {teleport.EndIpl.Name}", MessageType.Debug);

                            Ipl endIpl = new Ipl(teleport.EndIpl.Name, teleport.EndIpl.Type);
                            endIpl.Request();
                            teleport.EndIpl.CurrentIpl = endIpl;

                            LastIpl = SceneData.CurrentIplData;
                            SceneData.CurrentIplData = teleport.EndIpl;

                            Script.Wait(1000);
                            Game.FadeScreenIn(1000);
                        }
                    }
                }
                else
                {
                    UpdateTeleports(teleport.EndIpl);

                    if (distanceToEnd < 1.5f)
                    {
                        Utilities.DisplayHelpTextThisFrame("Press ~INPUT_CONTEXT~ to exit.");
                        Game.DisableControlThisFrame(2, Control.Context);
                        if (Game.IsDisabledControlJustPressed(2, Control.Context))
                        {
                            Game.FadeScreenOut(1000);
                            Script.Wait(1000);

                            PlayerPosition = start;

                            teleport.EndIpl.CurrentIpl.Remove();
                            teleport.EndIpl.CurrentIpl = null;

                            SceneData.CurrentIplData = LastIpl;

                            Script.Wait(1000);
                            Game.FadeScreenIn(1000);
                        }
                    }
                }
            });
        }

        private void UpdateWormHole(Orbital wormHole)
        {
            OrbitalData data = SceneData.Orbitals.Find(o => o.Name == wormHole.Name);
            if (data == null) return;
            EnterWormHole(wormHole.Position, data);
        }

        public void Delete()
        {
            lock (_updateLock)
            {
                if (PlayerLastVehicle != null)
                {
                    PlayerPed.SetIntoVehicle(PlayerLastVehicle, VehicleSeat.Driver);

                    float heading = PlayerLastVehicle.Heading;
                    PlayerLastVehicle.Quaternion = Quaternion.Identity;
                    PlayerLastVehicle.Heading = heading;
                    PlayerLastVehicle.Velocity = Vector3.Zero;
                }

                World.RenderingCamera = null;
                SpaceCamera?.Destroy();
                OrbitalSystem?.Abort();
            }
        }

        private void MovePlayerToGalaxy()
        {
            var position = SceneData.SurfaceFlag ? Database.PlanetSurfaceGalaxyCenter : Database.GalaxyCenter;
            if (!PlayerPed.IsInVehicle()) PlayerPosition = position;
            else PlayerPed.CurrentVehicle.Position = position;
        }

        private void EnterWormHole(Vector3 wormHolePosition, OrbitalData orbitalData)
        {
            float distanceToWormHole = PlayerPosition.DistanceTo(wormHolePosition);
            float escapeDistance = orbitalData.ExitDistance * 20f;
            float gravitationalPullDistance = orbitalData.ExitDistance * 15f;

            if (distanceToWormHole <= orbitalData.ExitDistance)
            {
                Exited?.Invoke(this, orbitalData.NextSceneFile, orbitalData.ExitRotation);
            }
            else
            {
                if (distanceToWormHole > escapeDistance)
                {
                    SpaceCamera.FieldOfView = Mathf.Lerp(SpaceCamera.FieldOfView, GameplayCamera.FieldOfView,
                        Game.LastFrameTime * 5);
                }
                else
                {
                    if (!SpaceCamera.IsShaking)
                    {
                        SpaceCamera.Shake(CameraShake.SkyDiving, 0);
                    }
                    else
                    {
                        SpaceCamera.ShakeAmplitude = 1.5f;
                    }

                    if (distanceToWormHole > gravitationalPullDistance)
                    {
                        Vector3 velocity = PlayerPed.IsInVehicle()
                            ? PlayerPed.CurrentVehicle.Velocity
                            : PlayerPed.Velocity;

                        Vector3 targetDir = wormHolePosition - PlayerPosition;
                        Vector3 targetVelocity = targetDir * 199.861639f; // Speed of light divided by 1,500,000

                        if (PlayerPed.IsInVehicle())
                        {
                            PlayerPed.CurrentVehicle.Velocity = targetVelocity;
                        }
                        else
                        {
                            PlayerPed.Velocity = targetVelocity;
                        }
                    }
                    else
                    {
                        DateTime timeout = DateTime.UtcNow + new TimeSpan(0, 0, 0, 7);
                        while (DateTime.UtcNow < timeout)
                        {
                            Script.Yield();

                            Vector3 direction = PlayerPosition - wormHolePosition;
                            direction.Normalize();

                            Vector3 targetPos = Utilities.RotatePointAroundPivot(PlayerPosition, wormHolePosition,
                                new Vector3(0, 0, 2000 * Game.LastFrameTime));

                            Vector3 playerPos = PlayerPed.IsInVehicle()
                                ? PlayerPed.CurrentVehicle.Position
                                : PlayerPosition;

                            Vector3 targetVelocity = targetPos - playerPos;

                            if (PlayerPed.IsInVehicle())
                            {
                                PlayerPed.CurrentVehicle.Velocity = targetVelocity;
                            }
                            else
                            {
                                PlayerPed.Velocity = targetVelocity;
                            }

                            UpdateCamera();
                        }

                        Exited?.Invoke(this, orbitalData.NextSceneFile, orbitalData.ExitRotation);
                    }
                }
            }
        }
    }
}