﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using GTA;
using GTA.Math;
using GTA.Native;
using SpaceMod.Extensions;
using SpaceMod.Lib;
using SpaceMod.OrbitalSystems;
using SpaceMod.Scenes.Interiors;
using Font = GTA.Font;

namespace SpaceMod.Scenes
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
		public delegate void OnMinedObjectEvent(CustomScene scene, Prop mineableObject);

		public event OnExitEvent Exited;
		public event OnMinedObjectEvent Mined;

		private readonly object _startLock = new object();
		private readonly object _updateLock = new object();

		private readonly List<Blip> _sceneLinkBlips = new List<Blip>();

		private float _leftRightFly;
		private float _upDownFly;
		private float _rollFly;
		private float _fly;

		private PlayerState _playerState = PlayerState.Floating;
		private Entity _flyHelper;
		private bool _enteringVehicle;

		private Vector3 _vehicleRepairPos;
		private Vector3 _vehicleRepairNormal;
		private DateTime _vehicleRepairTimeout;

		private Prop _currentMineableObject;
		private Vector3 _lastMinePos;
		private DateTime _mineTime;
		private bool _startMining;

		private readonly List<Prop> _registeredMineableObjects = new List<Prop>();

		public CustomScene(CustomXmlScene sceneData)
		{
			SceneData = sceneData;
			OldVehicles = new List<Vehicle>();
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

		public Weather OverrideWeather { get; internal set; }

		internal List<Tuple<UIText, UIText, Link>> DistanceText { get; private set; }

		internal IplData LastIpl { get; private set; }

		internal Vehicle PlayerLastVehicle { get; private set; }

		internal int IplCount { get; private set; }

		internal List<Vehicle> OldVehicles { get; }

		internal void Start()
		{
			lock (_startLock)
			{
				Function.Call(Hash.START_AUDIO_SCENE, "CREATOR_SCENES_AMBIENCE");

				Prop spaceDome = CreateProp(Vector3.Zero, SceneData.SpaceDomeModel);

				List<Orbital> orbitals =
					SceneData.Orbitals?.Select(x => CreateOrbital(x, SceneData.SurfaceFlag))
						.Where(o => o != default(Orbital))
						.ToList();

				List<LockedOrbital> lockedOrbitals =
					SceneData.LockedOrbitals?.Select(CreateLockedOrbital).Where(o => o != default(LockedOrbital)).ToList();

				WormHoles = orbitals?.Where(x => x.IsWormHole).ToList();
				OrbitalSystem = new OrbitalSystem(spaceDome.Handle, orbitals, lockedOrbitals, -0.3f);
				DistanceText = new List<Tuple<UIText, UIText, Link>>();

				ScriptSettings settings = ScriptSettings.Load(SpaceModDatabase.PathToScenes + "/" + "ExtraSettings.ini");
				var section = Path.GetFileNameWithoutExtension(SceneFile);
				OverrideWeather = (Weather)settings.GetValue(section, "weather", 0);
				Vector3 vehicleSpawn = V3Parse.Read(settings.GetValue(section, "vehicle_surface_spawn"),
					StaticSettings.VehicleSurfaceSpawn);

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
											 ? SpaceModDatabase.PlanetSurfaceGalaxyCenter
											 : SpaceModDatabase.GalaxyCenter) + link.OriginOffset);
					blip.Sprite = BlipSprite.Crosshair2;
					blip.Color = BlipColor.Blue;
					blip.Name = link.Name;
					_sceneLinkBlips.Add(blip);
				});

				Vehicle vehicle = PlayerPed.CurrentVehicle;

				if (Entity.Exists(vehicle))
				{
					PlayerLastVehicle = vehicle;
					PlayerLastVehicle.IsInvincible = true;
					PlayerLastVehicle.IsPersistent = true;

					if (SceneData.SurfaceFlag)
					{
						vehicle.LandingGear = VehicleLandingGear.Deployed;
						PlayerPed.Task.ClearAllImmediately();
						vehicle.Quaternion = Quaternion.Identity;
						vehicle.Rotation = Vector3.Zero;
						vehicle.PositionNoOffset = vehicleSpawn + Vector3.WorldUp;
						vehicle.PlaceOnGround();
						vehicle.Velocity = Vector3.Zero;
						vehicle.EngineRunning = false;
					}
					else
					{
						PlayerPed.CanRagdoll = false;
					}

					Script.Wait(1000);
				}

				MovePlayerToGalaxy();

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
				Debug.Log("CreateLockedOrbital::Entity model was not set in the xml.", DebugMessageType.Error);
				return default(LockedOrbital);
			}

			Model model = RequestModel(data.Model);
			if (!model.IsLoaded)
			{
				Debug.Log($"CreateLockedOrbital::Failed to load model: {data.Model}", DebugMessageType.Error);
				return default(LockedOrbital);
			}
			Debug.Log($"CreateLockedOrbital::Successfully loaded model: {data.Model}");
			Prop prop = World.CreateProp(model, Vector3.Zero, Vector3.Zero, false, false);
			prop.FreezePosition = true;
			LockedOrbital orbital = new LockedOrbital(prop.Handle, data.OriginOffset, data.EmitLight, data.Scale);
			//prop.Scale(new Vector3(data.Scale, data.Scale, data.Scale));
			return orbital;
		}

		public static Orbital CreateOrbital(OrbitalData data, bool surface)
		{
			if (string.IsNullOrEmpty(data.Model))
			{
				Debug.Log("CreateOrbital::Entity model was not set in the xml.", DebugMessageType.Error);
				return default(Orbital);
			}

			Model model = RequestModel(data.Model);
			if (!model.IsLoaded)
			{
				Debug.Log($"CreateOrbital::Failed to load model: {data.Model}", DebugMessageType.Error);
				return default(Orbital);
			}
			Debug.Log($"CreateOrbital::Successfully loaded model: {data.Model}");
			var prop = SpaceModLib.CreatePropNoOffset(model, (surface ? SpaceModDatabase.PlanetSurfaceGalaxyCenter : SpaceModDatabase.GalaxyCenter) + data.OriginOffset, false);
			prop.FreezePosition = true;
			Orbital orbital = new Orbital(prop.Handle, data.Name, data.RotationSpeed,
				data.EmitLight, data.Scale)
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
			//prop.Scale(new Vector3(data.Scale, data.Scale, data.Scale));

			return orbital;
		}

		public static Prop CreateProp(Vector3 position, string modelName)
		{
			if (string.IsNullOrEmpty(modelName)) return default(Prop);
			Model model = RequestModel(modelName);
			if (!model.IsLoaded)
			{
				Debug.Log($"CreateProp::Failed to load model: {modelName}", DebugMessageType.Error);
				return default(Prop);
			}
			Debug.Log($"CreateProp::Successfully loaded model: {modelName}");
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

		public static void RemoveIpl(IplData iplData)
		{
			iplData.CurrentIpl?.Remove();
			iplData.Teleports?.ForEach(teleport => RemoveIpl(teleport.EndIpl));
		}

		internal void Update()
		{
			if (!Monitor.TryEnter(_updateLock)) return;

			try
			{
				UI.HideHudComponentThisFrame(HudComponent.AreaName);
				VehicleFly();
				PlayerFly();
				HandleDeath();
				OrbitalSystem?.Process(SpaceModDatabase.GetValidGalaxyDomePosition(PlayerPed));
				ShowDistanceText();
				TryToStartNextScene();
				HandlePlayerVehicle();
				SceneData.Ipls?.ForEach(UpdateTeleports);
				WormHoles?.ForEach(UpdateWormHole);
			}
			finally
			{
				Monitor.Exit(_updateLock);
			}
		}

		private void HandlePlayerVehicle()
		{
			if (!Entity.Exists(PlayerLastVehicle)) return;
			if (!SceneData.SurfaceFlag)
			{
				PlayerLastVehicle.LandingGear = VehicleLandingGear.Retracted;
				return;
			}
			PlayerLastVehicle.LandingGear = VehicleLandingGear.Deployed;

			if (string.IsNullOrEmpty(SceneData.NextSceneOffSurface)) return;
			const float distance = 15 * 2;

			if (PlayerPosition.DistanceToSquared(PlayerLastVehicle.Position) < distance && !PlayerPed.IsInVehicle())
			{
				SpaceModLib.DisplayHelpTextWithGXT("GTS_LABEL_8");

				Game.DisableControlThisFrame(2, Control.Enter);

				if (Game.IsDisabledControlJustPressed(2, Control.Enter))
				{
					PlayerPed.SetIntoVehicle(PlayerLastVehicle, VehicleSeat.Driver);
				}
			}

			if (PlayerPed.IsInVehicle(PlayerLastVehicle))
			{
				Exited?.Invoke(this, SceneData.NextSceneOffSurface, SceneData.SurfaceExitRotation);
			}
			else if (PlayerPed.IsInVehicle())
			{
				SpaceModLib.DisplayHelpTextWithGXT("GTS_LABEL_9");

				Game.DisableControlThisFrame(2, Control.Context);

				if (!Game.IsDisabledControlJustPressed(2, Control.Context)) return;
				OldVehicles.Add(PlayerLastVehicle);
				PlayerLastVehicle = PlayerPed.CurrentVehicle;
			}
		}

		private void ShowDistanceText()
		{
			DistanceText?.ForEach(text =>
			{
				var position = SpaceModDatabase.GalaxyCenter + text.Item3.OriginOffset;
				if (StaticSettings.ShowCustomUi)
				{
					SpaceModLib.ShowUIPosition(null, DistanceText.IndexOf(text) + OrbitalSystem.Orbitals.Count,
							position, SpaceModDatabase.PathToSprites, text.Item3.Name, text.Item2);
				}

				float distance = Vector3.Distance(position, PlayerPosition);
				float targetDistance = text.Item3.ExitDistance;
				if (distance > targetDistance) return;
				Exited?.Invoke(this, text.Item3.NextSceneFile, text.Item3.ExitRotation);
			});
		}

		private void HandleDeath()
		{
			if (PlayerPed.IsDead)
			{
				Game.TimeScale = 0.3f;
				Function.Call(Hash.START_AUDIO_SCENE, "DEATH_SCENE");
				BigMessageThread.MessageInstance.ShowMissionPassedMessage(Game.GetGXTEntry("BM_LABEL_2"), 1000);
				Game.PlaySound("ScreenFlash", "WastedSounds");
				Script.Wait(1500);
				Game.FadeScreenOut(1000);
				Script.Wait(1000);
				Vector3 spawn = SceneData.SurfaceFlag ? SpaceModDatabase.PlanetSurfaceGalaxyCenter : SpaceModDatabase.GalaxyCenter;
				Function.Call(Hash.NETWORK_RESURRECT_LOCAL_PLAYER, spawn.X, spawn.Y, spawn.Z, 0, false, false);
				Function.Call(Hash._RESET_LOCALPLAYER_STATE);
				Function.Call(Hash.STOP_AUDIO_SCENE, "DEATH_SCENE");
				Exited?.Invoke(this, SceneFile, Vector3.Zero);
				Script.Wait(500);
				Game.FadeScreenIn(1000);
				Game.TimeScale = 1.0f;
				return;
			}

			SpaceModLib.TerminateScriptByName("respawn_controller");
			Game.Globals[4].SetInt(1);
			Function.Call(Hash._DISABLE_AUTOMATIC_RESPAWN, true);
			Function.Call(Hash.SET_FADE_IN_AFTER_DEATH_ARREST, false);
			Function.Call(Hash.SET_FADE_OUT_AFTER_ARREST, false);
			Function.Call(Hash.SET_FADE_OUT_AFTER_DEATH, false);
			Function.Call(Hash.IGNORE_NEXT_RESTART, true);
		}

		private void VehicleFly()
		{
			if (!Entity.Exists(PlayerLastVehicle))
				return;

			if (!PlayerPed.IsInVehicle(PlayerLastVehicle))
				return;

			FlyEntity(PlayerLastVehicle, StaticSettings.VehicleFlySpeed, StaticSettings.MouseControlFlySensitivity,
				!PlayerLastVehicle.IsOnAllWheels);
		}

		private void PlayerFly()
		{
			if (SceneData.SurfaceFlag) return;

			// here's when we're flying around and stuff.
			if (PlayerPed.IsInVehicle())
			{
				if (PlayerLastVehicle.Velocity.Length() > 0.15f)
				{
					PlayerLastVehicle.LockStatus = VehicleLockStatus.StickPlayerInside;

					if (Game.IsControlJustPressed(2, Control.Enter))
					{
						SpaceModLib.DisplayHelpTextWithGXT("GTS_LABEL_10");
					}
				}
				else
				{
					if (PlayerLastVehicle.LockStatus == VehicleLockStatus.StickPlayerInside)
					{
						PlayerLastVehicle.LockStatus = VehicleLockStatus.None;
					}
				}

				PlayerPed.Task.ClearAnimation("swimming@base", "idle");
				_enteringVehicle = false;
			}
			// here's where we're in space without a vehicle.
			else if (!PlayerPed.IsRagdoll && !PlayerPed.IsJumpingOutOfVehicle)
			{
				switch (_playerState)
				{
					// this let's us float
					case PlayerState.Floating:
						if (StaticSettings.UseFloating)
						{
							// make sure that we're floating first!
							if (!_enteringVehicle)
							{
								ToggleFloat();
							}

							// if the last vehicle is null, then there's nothing to do here.
							if (PlayerLastVehicle != null)
							{
								// since we're floating already or, "not in a vehicle" technically, we want to stop our vehicle
								// from moving and allow the payer to re-enter it.
								PlayerLastVehicle.LockStatus = VehicleLockStatus.None;
								PlayerLastVehicle.Velocity = Vector3.Zero;
								TryReenterVehicle(PlayerPed, PlayerLastVehicle);

								// we also want to let the player mine stuff, repair stuff, etc.
								if (!_enteringVehicle)
								{
									// the vehicle is damaged so let's allow the player to repair it.
									if (PlayerLastVehicle.IsDamaged || PlayerLastVehicle.EngineHealth < 1000)
									{
										StartVehicleRepair(PlayerPed, PlayerLastVehicle, 8f);
									}

									// we also want to allow the player to mine asteroids!
									StartMiningAsteroids(PlayerPed, PlayerLastVehicle, 5f);
								}
							}
						}
						break;
					// this let's us mine asteroids.
					case PlayerState.Mining:
						{
							if (_currentMineableObject == null || !Entity.Exists(_flyHelper) || _lastMinePos == Vector3.Zero)
							{
								if (Entity.Exists(_flyHelper))
								{
									_flyHelper.Detach();
								}

								_playerState = PlayerState.Floating;
								return;
							}

							// attach the player to the mineable object.
							if (!_startMining)
							{
								var dir = _lastMinePos - _flyHelper.Position;
								dir.Normalize();
								_flyHelper.Quaternion = Quaternion.FromToRotation(_flyHelper.ForwardVector, dir) * _flyHelper.Quaternion;
								_mineTime = DateTime.UtcNow + new TimeSpan(0, 0, 0, 5);
								_flyHelper.Position = _lastMinePos - dir;
								_startMining = true;
							}
							else
							{
								if (!PlayerPed.IsPlayingAnim("amb@world_human_welding@male@base", "base"))
								{
									PlayerPed.Task.PlayAnimation("amb@world_human_welding@male@base", "base", 4.0f,
										-4.0f, -1, (AnimationFlags)49, 0.0f);
									return;
								}

								if (DateTime.UtcNow > _mineTime)
								{
									PlayerPed.Task.ClearAnimation("amb@world_human_welding@male@base", "base");
									_flyHelper.Detach();
									_flyHelper.HasCollision = false;
									_flyHelper.IsVisible = false;
									_flyHelper.HasGravity = false;
									PlayerPed.IsVisible = true;
									Function.Call(Hash.SET_VEHICLE_GRAVITY, _flyHelper, false);
									SpaceModLib.NotifyWithGXT("GTS_LABEL_26");
									Mined?.Invoke(this, _currentMineableObject);
									_lastMinePos = Vector3.Zero;
									_currentMineableObject = null;
									_startMining = false;
									_playerState = PlayerState.Floating;
								}
							}
						}
						break;
					// this lets us repair stuff.
					case PlayerState.Repairing:
						{
							// the vehicle repair failed somehow and we need to fallback to the first switch case.
							if (_vehicleRepairPos == Vector3.Zero || _vehicleRepairNormal == Vector3.Zero || _flyHelper == null ||
								!_flyHelper.Exists())
							{
								_playerState = PlayerState.Floating;
								return;
							}

							// If we decide to move in another direction, let's cancel.
							if (Game.IsControlJustPressed(2, Control.VehicleAccelerate) ||
								Game.IsControlJustPressed(2, Control.MoveLeft) ||
								Game.IsControlJustPressed(2, Control.MoveRight) ||
								Game.IsControlJustPressed(2, Control.VehicleBrake))
							{
								_playerState = PlayerState.Floating;
								return;
							}

							// get some params for this sequence.
							float distance = PlayerPosition.DistanceTo(_vehicleRepairPos);
							Vector3 min, max, min2, max2;
							float radius;
							GetDimensions(PlayerPed, out min, out max, out min2, out max2, out radius);

							// make sure we're within distance of the vehicle.
							if (distance > radius)
							{
								// make sure to rotate the fly helper towards the repair point.
								Vector3 dir = _vehicleRepairPos - _flyHelper.Position;
								dir.Normalize();
								Quaternion lookRotation = Quaternion.FromToRotation(_flyHelper.ForwardVector, dir) * _flyHelper.Quaternion;
								_flyHelper.Quaternion = Quaternion.Lerp(_flyHelper.Quaternion, lookRotation, Game.LastFrameTime * 5);

								// now move the fly helper towards the direction of the repair point.
								_flyHelper.Velocity = dir * 1.5f;

								// make sure that we update the timer so that if the time runs out, we will fallback to the floating case.
								_vehicleRepairTimeout = DateTime.UtcNow + new TimeSpan(0, 0, 0, 5);
							}
							else
							{
								// since we're in tange of the vehicle we want to start the repair sequence.
								// we're going to stop the movement of the player, and play the repairing animation.
								Quaternion lookRotation = Quaternion.FromToRotation(_flyHelper.ForwardVector, -_vehicleRepairNormal) *
														  _flyHelper.Quaternion;
								_flyHelper.Quaternion = Quaternion.Lerp(_flyHelper.Quaternion, lookRotation, Game.LastFrameTime * 15);
								_flyHelper.Velocity = Vector3.Zero;

								// we're returning in this if, so that if we're for some reason not yet playing the animation, we
								// want to wait for it to start.
								if (!PlayerPed.IsPlayingAnim("amb@world_human_welding@male@base", "base"))
								{
									PlayerPed.Task.PlayAnimation("amb@world_human_welding@male@base", "base", 4.0f,
										-4.0f, -1, (AnimationFlags)49, 0.0f);
									return;
								}

								// if we've reached the end of the timer, then we're done repairing.
								if (DateTime.UtcNow > _vehicleRepairTimeout)
								{
									// repair the vehicle.
									PlayerLastVehicle.Repair();

									// let the player know what he/she's done.
									//SpaceModLib.NotifyWithGXT("Vehicle ~b~repaired~s~.", true);

									// clear the repairing animation.
									PlayerPed.Task.ClearAnimation("amb@world_human_welding@male@base", "base");

									// reset the player to the floating sate.
									_playerState = PlayerState.Floating;
								}
							}
						}
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		private void StartMiningAsteroids(Ped ped, Vehicle playerVehicle, float maxDistanceFromObject)
		{
			// make sure the ped isn't in a vehicle. which he shouldn't be, but just in case.
			if (ped.IsInVehicle(playerVehicle)) return;

			// let's start our raycast.
			RaycastResult ray = World.Raycast(ped.Position, ped.ForwardVector, maxDistanceFromObject, IntersectOptions.Everything, ped);
			if (!ray.DitHitEntity) return;

			// now that we have the hit entity, lets check to see if it's a designated mineable object.
			Entity entHit = ray.HitEntity;

			// this is a registered mineable object.
			if (_registeredMineableObjects.Contains(entHit))
			{
				// let's start mining!
				SpaceModLib.DisplayHelpTextWithGXT("GTS_LABEL_27");
				Game.DisableControlThisFrame(2, Control.Context);
				if (Game.IsDisabledControlJustPressed(2, Control.Context))
				{
					_currentMineableObject = entHit as Prop;
					_lastMinePos = ray.HitCoords;
					_playerState = PlayerState.Mining;
				}
			}
		}

		private void ToggleFloat()
		{
			// so this is when we're not floating
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
					FlyEntity(_flyHelper, 1.5f, 1.5f, !ArtificialCollision(PlayerPed, _flyHelper));
				}
			}
		}

		private void StartVehicleRepair(Ped ped, Vehicle vehicle, float maxDistFromVehicle)
		{
			if (ped.IsInVehicle(vehicle)) return;

			RaycastResult ray = World.Raycast(ped.Position, ped.ForwardVector, maxDistFromVehicle, IntersectOptions.Everything, ped);

			if (!ray.DitHitEntity) return;
			Entity entHit = ray.HitEntity;
			if (entHit.GetType() != typeof(Vehicle))
				return;

			Vehicle entVeh = (Vehicle)entHit;
			if (entVeh != vehicle) return;

			SpaceModLib.DisplayHelpTextWithGXT("GTS_LABEL_11");
			Game.DisableControlThisFrame(2, Control.Context);

			if (Game.IsDisabledControlJustPressed(2, Control.Context))
			{
				_vehicleRepairTimeout = DateTime.UtcNow + new TimeSpan(0, 0, 0, 0, 5000);
				_vehicleRepairPos = ray.HitCoords;
				_vehicleRepairNormal = ray.SurfaceNormal;
				_playerState = PlayerState.Repairing;
			}
		}

		private void TryReenterVehicle(Ped ped, Vehicle vehicle)
		{
			if (ped.IsInVehicle(vehicle)) return;

			Vector3 doorPos = vehicle.HasBone("door_dside_f") ? vehicle.GetBoneCoord("door_dside_f") : vehicle.Position;

			float dist = ped.Position.DistanceTo(doorPos);

			Vector3 dir = doorPos - _flyHelper.Position;

			if (!_enteringVehicle)
			{
				bool dot = !vehicle.HasBone("door_dside_f") ||
						   // This tells us if we're not "behind" the door so we're not trying to go through the vehicle 
						   // to enter.
						   Vector3.Dot((_flyHelper.Position - doorPos).Normalized, -vehicle.RightVector) > 0.2f;

				if (dist < 5f && dot)
				{
					Game.DisableControlThisFrame(2, Control.Enter);

					if (Game.IsDisabledControlJustPressed(2, Control.Enter))
					{
						_enteringVehicle = true;
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
					_enteringVehicle = false;
					return;
				}

				// I removed your DateTime code since there is no point for it
				// since that code is never run if you are in a vehicle and 
				// _enteringVehicle is false.

				Quaternion lookRotation = Quaternion.FromToRotation(_flyHelper.ForwardVector, dir.Normalized) * _flyHelper.Quaternion;
				_flyHelper.Quaternion = Quaternion.Lerp(_flyHelper.Quaternion, lookRotation, Game.LastFrameTime * 15);
				_flyHelper.Velocity = dir.Normalized * 1.5f;

				if (ped.Position.DistanceTo(doorPos) < 1.5f || !vehicle.HasBone("door_dside_f"))
				{
					DeleteFlyHelper();
					PlayerPed.Detach();
					PlayerPed.Task.ClearAllImmediately();
					PlayerPed.SetIntoVehicle(vehicle, VehicleSeat.Driver);

					_enteringVehicle = false;
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

			Quaternion leftRightRotation = Quaternion.FromToRotation(entity.ForwardVector, entity.RightVector * _leftRightFly);
			Quaternion upDownRotation = Quaternion.FromToRotation(entity.ForwardVector, entity.UpVector * _upDownFly);
			Quaternion rollRotation = Quaternion.FromToRotation(entity.RightVector, -entity.UpVector * _rollFly);
			Quaternion rotation = leftRightRotation * upDownRotation * rollRotation * entity.Quaternion;
			entity.Quaternion = Quaternion.Lerp(entity.Quaternion, rotation, Game.LastFrameTime * 1.3f);

			if (canFly)
			{
				if (fly > 0)
				{
					var targetVelocity = entity.ForwardVector.Normalized * flySpeed * _fly;
					entity.Velocity = Vector3.Lerp(entity.Velocity, targetVelocity, Game.LastFrameTime * 5);
				}
				else if (reverse > 0)
				{
					entity.Velocity = Vector3.Lerp(entity.Velocity, Vector3.Zero, Game.LastFrameTime * 2.5f);
				}
			}
		}

		private void TryToStartNextScene()
		{
			SceneData.Orbitals?.ForEach(orbital =>
			{
				if (orbital == null) return;
				if (orbital.NextSceneFile == string.Empty) return;
				Vector3 position = SpaceModDatabase.GalaxyCenter + orbital.OriginOffset;
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

				World.DrawMarker(MarkerType.VerticalCylinder, start - Vector3.WorldUp, Vector3.RelativeRight, Vector3.Zero, new Vector3(0.5f, 0.5f, 0.5f), Color.Gold);
				World.DrawMarker(MarkerType.VerticalCylinder, end - Vector3.WorldUp, Vector3.RelativeRight, Vector3.Zero, new Vector3(0.5f, 0.5f, 0.5f), Color.Gold);

				float distanceToStart = Vector3.Distance(PlayerPosition, start);
				float distanceToEnd = Vector3.Distance(PlayerPosition, end);
				if (teleport.EndIpl.CurrentIpl == null)
				{
					if (distanceToStart < 1.5f && teleport.EndIpl != null)
					{
						SpaceModLib.DisplayHelpTextWithGXT("GTS_LABEL_12");
						Game.DisableControlThisFrame(2, Control.Context);
						if (Game.IsDisabledControlJustPressed(2, Control.Context))
						{
							Game.FadeScreenOut(1000);
							Script.Wait(1000);

							Ipl endIpl = new Ipl(teleport.EndIpl.Name, teleport.EndIpl.Type);
							endIpl.Request();
							teleport.EndIpl.CurrentIpl = endIpl;

							LastIpl = SceneData.CurrentIplData;
							SceneData.CurrentIplData = teleport.EndIpl;

							PlayerPosition = end;

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
						SpaceModLib.DisplayHelpTextWithGXT("GTS_LABEL_13");
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

		internal void Delete()
		{
			lock (_updateLock)
			{
				_flyHelper?.Delete();

				while (OldVehicles.Count > 0)
				{
					var v = OldVehicles[0];
					v.Delete();
					OldVehicles.RemoveAt(0);
				}

				if (PlayerLastVehicle != null)
				{
					PlayerPed.SetIntoVehicle(PlayerLastVehicle, VehicleSeat.Driver);
					PlayerLastVehicle.LockStatus = VehicleLockStatus.None;

					float heading = PlayerLastVehicle.Heading;
					PlayerLastVehicle.Quaternion = Quaternion.Identity;
					PlayerLastVehicle.Heading = heading;
					PlayerLastVehicle.Velocity = Vector3.Zero;
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


				Function.Call(Hash.STOP_AUDIO_SCENE, "CREATOR_SCENES_AMBIENCE");
			}
		}

		private void MovePlayerToGalaxy()
		{
			var position = SceneData.SurfaceFlag ? SpaceModDatabase.PlanetSurfaceGalaxyCenter : SpaceModDatabase.GalaxyCenter;
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
						Vector3 targetDir = wormHolePosition - PlayerPosition;

						// FIXME: Something wrong here with velocity...
						Vector3 targetVelocity = targetDir * 50;

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

							Vector3 targetPos = SpaceModLib.RotatePointAroundPivot(PlayerPosition, wormHolePosition,
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

		public void RegisterObjectForMining(Prop prop)
		{
			if (_registeredMineableObjects.Contains(prop))
				return;
			_registeredMineableObjects.Add(prop);
		}

		public void UnregisterObjectForMining(Prop prop)
		{
			_registeredMineableObjects.Remove(prop);
		}
	}
}
