using System;
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
    public enum PlayerState
    {
        Floating,
        Mining,
        Repairing
    }

    public class CustomScene
    {
        public delegate void OnExitEvent(CustomScene scene, string newSceneFile, Vector3 exitRotation);

        public event OnExitEvent Exited;

        private readonly object _startLock = new object();
        private readonly object _updateLock = new object();

        private readonly List<Blip> _sceneLinkBlips = new List<Blip>();

        private float _leftRightFly;
        private float _upDownFly;
        private float _rollFly;
        private float _fly;

        private PlayerState _playerState;
        private Entity _flyHelper;

        public CustomScene(CustomXmlScene sceneData)
        {
            SceneData = sceneData;
        }

        internal Ped PlayerPed => Game.Player.Character;

        internal Vector3 PlayerPosition {
            get { return PlayerPed.Position; }
            set { PlayerPed.Position = value; }
        }

        public CustomXmlScene SceneData { get; }

        public List<Orbital> WormHoles { get; private set; }

        public OrbitalSystem OrbitalSystem { get; private set; }

        public string SceneFile { get; internal set; }

        internal List<Tuple<UIText, UIText, Link>> DistanceText { get; private set; }

        internal IplData LastIpl { get; private set; }

        internal Vehicle PlayerLastVehicle { get; private set; }

        internal int IplCount { get; private set; }

        internal void Start()
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
                    Blip blip =
                        World.CreateBlip((SceneData.SurfaceFlag
                                             ? Database.PlanetSurfaceGalaxyCenter
                                             : Database.GalaxyCenter) + link.OriginOffset);
                    blip.Sprite = BlipSprite.Crosshair2;
                    blip.Color = BlipColor.Blue;
                    blip.Name = link.Name;
                    _sceneLinkBlips.Add(blip);
                });

                MovePlayerToGalaxy();

                Vehicle vehicle = PlayerPed.CurrentVehicle;

                if (SceneData.SurfaceFlag)
                {
                    PlayerPed.Task.ClearAllImmediately();

                    if (vehicle != null && vehicle.Exists())
                    {
                        PlayerLastVehicle = vehicle;
                        PlayerLastVehicle.IsPersistent = true;

                        vehicle.Quaternion = Quaternion.Identity;
                        vehicle.Rotation = Vector3.Zero;
                        vehicle.Position = StaticSettings.VehicleSurfaceSpawn + new Vector3(0, 0, 0.5f);
                        vehicle.LandingGear = VehicleLandingGear.Deployed;
                        vehicle.IsInvincible = true;
                        vehicle.Velocity = Vector3.Zero;
                        vehicle.EngineRunning = false;
                    }
                }
                else
                {
                    if (vehicle != null && vehicle.Exists())
                    {
                        PlayerLastVehicle = vehicle;
                        PlayerLastVehicle.IsPersistent = true;
                    }

                    PlayerPed.CanRagdoll = false;
                }

                SceneData.Ipls?.ForEach(iplData =>
                {
                    Ipl ipl = new Ipl(iplData.Name, iplData.Type);
                    ipl.Request();
                    iplData.CurrentIpl = ipl;
                });

                if (SceneData.Ipls != null) IplCount = SceneData.Ipls.Count;
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
            if (!string.IsNullOrEmpty(data.Name))
            {
                Blip blip = orbital.AddBlip();
                blip.Sprite = BlipSprite.Crosshair2;
                blip.Color = BlipColor.Blue;
                blip.Name = orbital.Name;
            }
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

        internal void Update()
        {
            if (!Monitor.TryEnter(_updateLock)) return;

            try
            {
                VehicleFly();
                PlayerFly();

                OrbitalSystem?.Process(Database.GetValidGalaxyDomePosition(PlayerPed));

                DistanceText?.ForEach(text =>
                {
                    var position = Database.GalaxyCenter + text.Item3.OriginOffset;
                    if (StaticSettings.ShowCustomUI)
                    {
                        Utilities.ShowUIPosition(null, DistanceText.IndexOf(text) + OrbitalSystem.Orbitals.Count,
                                position, Database.PathToSprites, text.Item3.Name,
                                text.Item1, text.Item2);
                    }

                    float distance = Vector3.Distance(position, PlayerPosition);
                    float targetDistance = text.Item3.ExitDistance;
                    if (distance > targetDistance) return;
                    Exited?.Invoke(this, text.Item3.NextSceneFile, text.Item3.ExitRotation);
                });

                TryToStartNextScene();

                if (SceneData.SurfaceFlag && PlayerLastVehicle != null &&
                    !string.IsNullOrEmpty(SceneData.NextSceneOffSurface))
                {
                    float distance = Vector3.Distance(PlayerPosition, PlayerLastVehicle.Position);

                    if (distance < 15)
                    {
                        Utilities.DisplayHelpTextThisFrame("Press ~INPUT_ENTER~ to leave.");

                        Game.DisableControlThisFrame(2, Control.Enter);

                        if (Game.IsDisabledControlJustPressed(2, Control.Enter))
                        {
                            PlayerPed.SetIntoVehicle(PlayerLastVehicle, VehicleSeat.Driver);
                        }
                    }

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

        private void VehicleFly()
        {
            Vehicle vehicle = PlayerPed.CurrentVehicle;
            if (vehicle == null) return;
            if (!vehicle.Exists()) return;
            if (vehicle.Driver != PlayerPed) return;

            FlyEntity(vehicle, StaticSettings.VehicleFlySpeed, StaticSettings.MouseControlFlySensitivity,
                !vehicle.IsOnAllWheels);
        }

        private void PlayerFly()
        {
            if (SceneData.SurfaceFlag) return;
            if (PlayerPed.IsInVehicle())
            {
                DeleteFlyHelper();
                PlayerPed.Task.ClearAnimation("swimming@base", "idle");
            }
            else if (!PlayerPed.IsRagdoll && !PlayerPed.IsGettingUp && !PlayerPed.IsJumpingOutOfVehicle)
            {
                switch (_playerState)
                {
                    case PlayerState.Floating:
                        {
                            if (_flyHelper == null || !_flyHelper.Exists())
                            {
                                _flyHelper = World.CreateVehicle(VehicleHash.Panto, PlayerPosition, PlayerPed.Heading);

                                _flyHelper.HasCollision = false;

                                _flyHelper.IsVisible = false;

                                _flyHelper.HasGravity = false;

                                Function.Call(Hash.SET_VEHICLE_GRAVITY, _flyHelper, false);

                                PlayerPed.Task.ClearAllImmediately();

                                PlayerPed.AttachTo(_flyHelper, 0);
                                
                                _flyHelper.Velocity = Vector3.Zero;
                            }
                            else
                            {
                                if (PlayerPed.Weapons.Current.Hash != WeaponHash.Unarmed)
                                {
                                    PlayerPed.Weapons.Select(WeaponHash.Unarmed);
                                }
                                if (!PlayerPed.IsPlayingAnim("swimming@base", "idle"))
                                {
                                    PlayerPed.Task.PlayAnimation("swimming@base", "idle", 8.0f, -8.0f, -1, AnimationFlags.Loop, 
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
                                else FlyEntity(_flyHelper, 1.5f, 1.5f);
                            }

                            if (PlayerLastVehicle != null)
                                TryReenterVehicle(PlayerPed, PlayerLastVehicle);
                        }
                        break;
                    case PlayerState.Mining:
                        {
                            DeleteFlyHelper();
                        }
                        break;
                    case PlayerState.Repairing:
                        {
                            DeleteFlyHelper();
                        }
                        break;
                }
            }
        }

        private void TryReenterVehicle(Ped ped, Vehicle vehicle)
        {
            if (ped.IsInVehicle(vehicle)) return;
            
            Vector3 doorPos = vehicle.GetBoneCoord("door_dside_f");
            float dist = ped.Position.DistanceTo(doorPos);

            if (dist < 5f)
            {
                Game.DisableControlThisFrame(2, Control.Enter);

                Utilities.DisplayHelpTextThisFrame("Press ~INPUT_ENTER~ to enter vehicle.");

                if (Game.IsDisabledControlJustPressed(2, Control.Enter))
                {
                    Vector3 dir = doorPos - _flyHelper.Position;
                    Quaternion rotation = Quaternion.FromToRotation(_flyHelper.ForwardVector, dir) * ped.Quaternion;
                    _flyHelper.Quaternion = Quaternion.Lerp(_flyHelper.Quaternion, Utilities.LookRotation(dir), Game.LastFrameTime * 5);

                    UI.Notify("New Rot: " + dir.ToString());
                    UI.Notify("Old Rot: " + ped.Rotation.ToString());

                    //ped.Task.WarpIntoVehicle(vehicle, VehicleSeat.Driver);
                }
            }
        }

        private void FlyEntity(Entity entity, float flySpeed, float sensitivity, bool canFly = true)
        {
            float leftRight = Game.GetControlNormal(2, Control.MoveLeftRight);
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

            _leftRightFly = Mathf.Lerp(_leftRightFly, leftRight, Game.LastFrameTime * 2.5f);
            _upDownFly = Mathf.Lerp(_upDownFly, upDown, Game.LastFrameTime * 5);
            _rollFly = Mathf.Lerp(_rollFly, roll, Game.LastFrameTime * 5);
            _fly = Mathf.Lerp(_fly, fly, Game.LastFrameTime * 1.3f);

            if (canFly)
            {
                Quaternion leftRightRotation = Quaternion.FromToRotation(entity.ForwardVector,
                    entity.RightVector * _leftRightFly);
                Quaternion upDownRotation = Quaternion.FromToRotation(entity.ForwardVector, entity.UpVector * _upDownFly);
                Quaternion rollRotation = Quaternion.FromToRotation(entity.RightVector, -entity.UpVector * _rollFly);
                Quaternion rotation = leftRightRotation * upDownRotation * rollRotation * entity.Quaternion;
                entity.Quaternion = Quaternion.Lerp(entity.Quaternion, rotation, Game.LastFrameTime * 1.3f);
            }

            if (fly > 0)
            {
                var playerVelocity = entity.ForwardVector.Normalized * flySpeed * _fly;
                entity.Velocity = playerVelocity;
            }
            else if (reverse > 0)
            {
                entity.Velocity = Vector3.Lerp(entity.Velocity, Vector3.Zero, Game.LastFrameTime);
            }
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

        private void DeleteFlyHelper()
        {
            if (_flyHelper != null)
            {
                if (PlayerPed.IsAttachedTo(_flyHelper)) PlayerPed.Detach();
                _flyHelper.Delete();
                _flyHelper = null;
            }
        }

        internal void Delete()
        {
            lock (_updateLock)
            {
                _flyHelper?.Delete();

                if (PlayerLastVehicle != null)
                {
                    PlayerPed.SetIntoVehicle(PlayerLastVehicle, VehicleSeat.Driver);

                    float heading = PlayerLastVehicle.Heading;
                    PlayerLastVehicle.Quaternion = Quaternion.Identity;
                    PlayerLastVehicle.Heading = heading;
                    PlayerLastVehicle.Velocity = Vector3.Zero;
                    PlayerLastVehicle.Health = PlayerLastVehicle.MaxHealth;
                    PlayerLastVehicle.IsInvincible = false;
                    PlayerLastVehicle.EngineRunning = true;
                    PlayerLastVehicle.IsPersistent = false;
                }

                OrbitalSystem?.Abort();

                while (IplCount > 0)
                {
                    var ipl = SceneData.Ipls[0];
                    RemoveIpl(ipl);
                    IplCount--;
                }

                while (_sceneLinkBlips.Count > 0)
                {
                    var blip = _sceneLinkBlips[0];
                    blip.Remove();
                    _sceneLinkBlips.RemoveAt(0);
                }

                SceneData.CurrentIplData = null;

                GameplayCamera.ShakeAmplitude = 0;
            }
        }

        private static void RemoveIpl(IplData iplData)
        {
            iplData.CurrentIpl?.Remove();
            iplData.Teleports?.ForEach(teleport =>
            {
                RemoveIpl(teleport.EndIpl);
            });
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
                if (distanceToWormHole <= escapeDistance)
                {
                    if (!GameplayCamera.IsShaking)
                    {
                        GameplayCamera.Shake(CameraShake.SkyDiving, 0);
                    }
                    else
                    {
                        GameplayCamera.ShakeAmplitude = 1.5f;
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
                        }

                        Exited?.Invoke(this, orbitalData.NextSceneFile, orbitalData.ExitRotation);
                    }
                }
            }
        }
    }
}
