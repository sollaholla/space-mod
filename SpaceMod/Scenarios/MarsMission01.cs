using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DefaultMissions.Scenes.SceneTypes;
using GTA;
using GTA.Native;
using GTA.Math;
using SpaceMod;
using SpaceMod.Lib;
using SpaceMod.Scenario;

namespace DefaultMissions
{
	public class MarsMission01 : CustomScenario
	{
		private const string MarsBaseInteriorName = "Mars/mars_base_int_01";
		private readonly string _ufoModelName = "zanufo";

		private Model _ufoModel;
		private bool _didNotify;
		private bool _didSetTimeCycle;
		private bool _isCheckingSats;
		private Prop _alienEggProp;

		private readonly MarsMissionSatelliteScene _satelliteScene;
		private readonly Vector3 _computerPos = new Vector3(-2013.115f, 3198.238f, 32.81007f);

		public MarsMission01()
		{
			Aliens = new List<Ped>();
			Ufos = new List<Vehicle>();
			_satelliteScene = new MarsMissionSatelliteScene();

			OriginalCanPlayerRagdoll = PlayerPed.CanRagdoll;
			OriginalMaxHealth = PlayerPed.MaxHealth;
			PlayerPed.MaxHealth = 1500;
			PlayerPed.Health = PlayerPed.MaxHealth;

			_ufoModelName = Settings.GetValue("settings", "ufo_model", _ufoModelName);
			CurrentMissionStep = Settings.GetValue("mission", "current_mission_step", 0);
			Settings.SetValue("settings", "ufo_model", _ufoModelName);
			Settings.SetValue("mission", "current_mission_step", CurrentMissionStep);
			Settings.Save();
		}

		public int CurrentMissionStep { get; protected set; }
		public List<Ped> Aliens { get; }
		public List<Vehicle> Ufos { get; }
		public Ped PlayerPed => Game.Player.Character;
		public bool OriginalCanPlayerRagdoll { get; set; }
		public int OriginalMaxHealth { get; set;  }
		public Prop EnterenceBlocker { get; private set; }

		public Vector3 PlayerPosition {
			get { return PlayerPed.Position; }
			set { PlayerPed.Position = value; }
		}

		public override void Start()
		{
			PlayerPed.CanRagdoll = false;
			PlayerPed.IsExplosionProof = true;
			if (CurrentMissionStep == 0 && CurrentScene.SceneFile == "MarsSurface.space")
				SpawnEntities();
		}

		public void SpawnEntities()
		{
			var origin = PlayerPosition.Around(100f);

			for (var i = 0; i < 15; i++)
			{
				Vector3 position = origin.Around(new Random().Next(50, 75));
				Vector3 artificial = TryToGetGroundHeight(position);
				if (artificial != Vector3.Zero) position = artificial;

				Ped ped = SpaceModLib.CreateAlien(position, WeaponHash.Railgun);
				ped.Health = 3750;
				ped.AlwaysDiesOnLowHealth = false;
				ped.CanRagdoll = false;
				ped.IsOnlyDamagedByPlayer = true;

				Blip blip = ped.AddBlip();
				blip.Name = "Alien";
				blip.Scale = 0.7f;

				Aliens.Add(ped);
			}

			_ufoModel = new Model(_ufoModelName);
			_ufoModel.Request();
			DateTime timout = DateTime.UtcNow + new TimeSpan(0, 0, 0, 10);
			while (!_ufoModel.IsLoaded)
			{
				Script.Yield();
				if (DateTime.UtcNow > timout)
					break;
			}

			if (!_ufoModel.IsLoaded)
			{
				UI.Notify($"{_ufoModelName} model failed to load! Make sure you have a valid model in the .ini file.");
				EndScenario(false);
				return;
			}

			for (var i = 0; i < 1; i++)
			{
				Vector3 position = origin.Around(75);
				position.Z = SpaceModDatabase.PlanetSurfaceGalaxyCenter.Z + 45;

				Vehicle spaceCraft = World.CreateVehicle(_ufoModel, position,
					0);
				spaceCraft.IsOnlyDamagedByPlayer = true;

				Ped ped = spaceCraft.CreatePedOnSeat(VehicleSeat.Driver, PedHash.MovAlien01);
				ped.RelationshipGroup = SpaceModDatabase.AlienRelationship;
				ped.IsOnlyDamagedByPlayer = true;
				ped.SetDefaultClothes();

				spaceCraft.MaxHealth = 2500;
				spaceCraft.Health = spaceCraft.MaxHealth;
				spaceCraft.Heading = (PlayerPed.Position - spaceCraft.Position).ToHeading();

				ped.Task.FightAgainst(PlayerPed);
				ped.SetCombatAttributes(CombatAttributes.AlwaysFight, true);

				Blip blip = spaceCraft.AddBlip();
				blip.Name = "UFO";
				blip.Color = BlipColor.Green;

				Ufos.Add(spaceCraft);
			}
		}

		private Vector3 TryToGetGroundHeight(Vector3 position)
		{
			var artificial = position.MoveToGroundArtificial();
			var timeout = DateTime.UtcNow + new TimeSpan(0, 0, 0, 0, 1500);

			while (artificial == Vector3.Zero)
			{
				artificial = position.MoveToGroundArtificial();

				Script.Yield();

				if (DateTime.UtcNow > timeout)
					break;
			}

			return artificial;
		}

		public override void OnUpdate()
		{
			if (CurrentMissionStep < 6 && CurrentScene.SceneFile != "MarsSurface.space")
				return;
			if (CurrentMissionStep >= 6 && CurrentScene.SceneFile != "EuropaSurface.space")
				return;
			if (CurrentMissionStep < 1)
			{
				if (!Entity.Exists(EnterenceBlocker) && CurrentScene.SceneData.Ipls.Any() && CurrentScene.SceneData.Ipls.Any())
				{
                    Vector3 position = CurrentScene.SceneData.Ipls[0]?.Teleports[0]?.Start ?? Vector3.Zero;
                    EnterenceBlocker = new Prop(World.CreateProp("lts_prop_lts_elecbox_24b", position, Vector3.Zero, false, false).Handle)
                    {
                        IsVisible = false
                    };
				}
			}

			var isInUfo = IsInInterior("EuropaSurface.space", "Europa/ufo_interior");
			Vector3 spawnAlienEgg = new Vector3(-10018.03f, -9976.996f, 10042.64f) + Vector3.WorldDown;
			switch (CurrentMissionStep)
			{
				case 0:
					if (Entity.Exists(EnterenceBlocker) && PlayerPed.IsTouching(EnterenceBlocker))
					{
						if (!_didNotify)
						{
							SpaceModLib.DisplayHelpTextWithGXT("GTS_LABEL_1");
							_didNotify = true;
						}
					}
					else
					{
						_didNotify = false;
					}

					Aliens.ForEach(UpdateAlien);
					Ufos.ForEach(UpdateUfo);
					if (Aliens.All(a => a.IsDead) && Ufos.All(u => Entity.Exists(u.Driver) && u.Driver.IsDead))
					{
						CurrentMissionStep++;
					}
					break;
				case 1:
					SpaceModLib.ShowSubtitleWithGXT("GTS_LABEL_15");
					if (Entity.Exists(EnterenceBlocker))
					{
						EnterenceBlocker.Delete();
					}
					CurrentMissionStep++;
					break;
				case 2:
					if (IsInInterior("MarsSurface.space", MarsBaseInteriorName))
					{
						Vector3[] spawnPoints =
						{
							new Vector3(-2014.449f, 3216.207f, 32.81112f),
							new Vector3(-1989.808f, 3212.001f, 32.81171f),
							new Vector3(-1991.477f, 3205.936f, 32.81038f),
							new Vector3(-1997.719f, 3211.335f, 32.83896f)
						};
						foreach (var spawn in spawnPoints)
						{
							var alien = SpaceModLib.CreateAlien(spawn, WeaponHash.MicroSMG);
							alien.Heading = (PlayerPed.Position - alien.Position).ToHeading();
							alien.Task.FightAgainst(PlayerPed, -1);
							//alien.AddBlip().Scale = 0.7f;
							Aliens.Add(alien);
						}
						CurrentMissionStep++;
					}
					break;
				case 3:
					Aliens.ForEach(a => UpdateAlien2(a, "MarsSurface.space", MarsBaseInteriorName));
					if (Aliens.All(a => a.IsDead))
					{
						SpaceModLib.ShowSubtitleWithGXT("GTS_LABEL_16");
						Aliens.Clear();
						CurrentMissionStep++;
					}
					break;
				case 4:
					if (!_isCheckingSats)
					{
						const float markerSize = 0.4f;
						World.DrawMarker(MarkerType.VerticalCylinder, _computerPos + Vector3.WorldDown, Vector3.Zero, Vector3.Zero,
							new Vector3(markerSize, markerSize, markerSize), Color.Gold);
						float distance = Vector3.DistanceSquared(PlayerPosition, _computerPos);
						const float interactDist = 1.5f * 2;

						if (distance < interactDist)
						{
							SpaceModLib.DisplayHelpTextWithGXT("GTS_LABEL_5");
							Game.DisableControlThisFrame(2, Control.Talk);
							Game.DisableControlThisFrame(2, Control.Context);

							if (Game.IsDisabledControlJustPressed(2, Control.Context))
							{
								_isCheckingSats = true;
								Game.FadeScreenOut(0);
								_satelliteScene.Spawn();
								Game.FadeScreenIn(100);
							}
						}
					}
					else
					{
						if (_satelliteScene.Failed)
						{
							_satelliteScene.Remove();
							CurrentMissionStep++;
							return;
						}
						_satelliteScene.Update();
						SpaceModLib.DisplayHelpTextWithGXT("GTS_LABEL_6");
						Game.DisableControlThisFrame(2, Control.Talk);
						Game.DisableControlThisFrame(2, Control.Context);
						if (Game.IsDisabledControlJustPressed(2, Control.Context))
						{
							_satelliteScene.Remove();
							TimeCycleModifier.Clear();
							UI.ShowSubtitle(string.Empty); // just to clear the subtitle.
							CurrentMissionStep++;
						}
						if (!_didSetTimeCycle)
						{
							SpaceModLib.ShowSubtitleWithGXT("GTS_LABEL_17");
							TimeCycleModifier.Set("CAMERA_secuirity_FUZZ", 1f);
							_didSetTimeCycle = true;
						}
					}
					break;
				case 5:
					Function.Call(Hash.PLAY_MISSION_COMPLETE_AUDIO, "FRANKLIN_BIG_01");
					ScaleFormMessages.Message.SHOW_MISSION_PASSED_MESSAGE(Game.GetGXTEntry("BM_LABEL_0"));
					Script.Wait(1000);
					SpaceModLib.ShowSubtitleWithGXT("GTS_LABEL_18");
					CurrentMissionStep++;
					break;
				case 6:
					if (CurrentScene.SceneFile == "EuropaSurface.space")
					{
						SpaceModLib.ShowSubtitleWithGXT("GTS_LABEL_19");
						CurrentMissionStep++;
					}
					break;
				case 7:
					if (isInUfo)
					{
						Vector3[] spawnPoints =
						{
							new Vector3(-10015.6f, -9970.742f, 10041.6f),
							new Vector3(-10021.1f, -9975.481f, 10041.6f),
							new Vector3(-10024.99f, -9963.521f, 10041.6f),
							new Vector3(-10015.08f, -9968.357f, 10041.6f),
						};
						foreach (var spawn in spawnPoints)
						{
							var alien = SpaceModLib.CreateAlien(spawn, WeaponHash.MicroSMG);
							alien.Heading = (PlayerPed.Position - alien.Position).ToHeading();
							alien.Task.FightAgainst(PlayerPed, -1);
							Aliens.Add(alien);
						}
						_alienEggProp = World.CreateProp("sm_alien_egg_w_container", spawnAlienEgg, false, false);
						_alienEggProp.FreezePosition = true;
						_alienEggProp.Heading = (PlayerPosition - spawnAlienEgg).ToHeading();

						CurrentMissionStep++;
					}
					break;
				case 8:
					if (_alienEggProp == null)
					{
						CurrentMissionStep = 7;
						return;
					}

					if (isInUfo)
					{
						Aliens.ForEach(a => UpdateAlien2(a, "EuropaSurface.space", "Europa/ufo_interior"));
						if (Aliens.All(a => a.IsDead))
						{
							Aliens.Clear();
							SpaceModLib.ShowSubtitleWithGXT("GTS_LABEL_20");
							CurrentMissionStep++;
						}
					}
					break;
				case 9:
					if (_alienEggProp == null)
					{
						CurrentMissionStep = 7;
						return;
					}

					if (isInUfo)
					{
						float distance = PlayerPosition.DistanceTo(_alienEggProp.Position);
						if (distance < 1.3f)
						{
							SpaceModLib.DisplayHelpTextWithGXT("GTS_LABEL_7");
							Game.DisableControlThisFrame(2, Control.Context);

							if (Game.IsDisabledControlJustPressed(2, Control.Context))
							{
								SpaceModLib.ShowSubtitleWithGXT("GTS_LABEL_21");
								ScaleFormMessages.Message.SHOW_MISSION_PASSED_MESSAGE(Game.GetGXTEntry("BM_LABEL_1"));
								Function.Call(Hash.PLAY_MISSION_COMPLETE_AUDIO, "FRANKLIN_BIG_01");
								_alienEggProp.Delete();
								EndScenario(true);
							}
						}
					}
					break;
			}
		}

		private bool IsInInterior(string scene, string interior)
		{
			return CurrentScene.SceneFile == scene &&
				   CurrentScene.SceneData.CurrentIplData?.Name == interior;
		}

		private void UpdateUfo(Vehicle ufo)
		{
			if (ufo.IsDead)
				return;

			if (!string.IsNullOrEmpty(CurrentScene.SceneData.CurrentIplData?.Name))
				ufo.FreezePosition = CurrentScene.SceneData.CurrentIplData.Name == MarsBaseInteriorName;

			if (Entity.Exists(ufo.Driver))
				SpaceModLib.ArtificialDamage(ufo.Driver, PlayerPed, 150, 150);

			if (ufo.IsDead || (Entity.Exists(ufo.Driver) && ufo.Driver.IsDead) || !ufo.IsDriveable)
			{
				if (Entity.Exists(ufo.Driver) && !ufo.Driver.IsDead)
					ufo.Driver.Kill();
				ufo.CurrentBlip.Remove();
				ufo.Explode();
			}

			ufo.Rotation = new Vector3(0, 0, ufo.Rotation.Z);

			if (ufo.Position.DistanceToSquared2D(PlayerPosition) > 70000)
			{
				const float rotationSpeed = 2.5f;
				ufo.Heading = Mathf.Lerp(ufo.Heading, (PlayerPosition - ufo.Position).ToHeading(), Game.LastFrameTime * rotationSpeed);

				var dir = PlayerPosition - ufo.Position;
				dir.Z = 0;
				bool inAngle = Vector3.Angle(ufo.ForwardVector, dir) < 5;

				if (!inAngle)
					ufo.Velocity = Vector3.Zero;

				ufo.Speed = inAngle ? 5 : 0;
				return;
			}

			ufo.Speed = 30;

		}

		private void UpdateAlien(Ped alienPed)
		{
			if (!string.IsNullOrEmpty(CurrentScene.SceneData.CurrentIplData?.Name))
				alienPed.FreezePosition = CurrentScene.SceneData.CurrentIplData.Name == MarsBaseInteriorName;

			if (alienPed.IsDead)
			{
				if (alienPed.CurrentBlip.Exists())
				{
					alienPed.MarkAsNoLongerNeeded();
					alienPed.CurrentBlip.Remove();
					alienPed.CanRagdoll = true;
				}

				return;
			}

			SpaceModLib.ArtificialDamage(alienPed, PlayerPed, 1.5f, 75);
			float distance = Vector3.Distance(PlayerPosition, alienPed.Position);

			if (distance > 25)
				alienPed.Task.RunTo(PlayerPed.Position, true);
		}

		private void UpdateAlien2(Ped ped, string scene, string interior)
		{
			var isMoveable = IsInInterior(scene, interior);
			ped.FreezePosition = !isMoveable;
			ped.IsVisible = isMoveable;

			if (ped.IsDead && ped.IsPersistent)
			{
				ped.MarkAsNoLongerNeeded();
			}
		}

		public override void OnAborted()
		{
			CleanUp();
			Settings.SetValue("mission", "current_mission_step", CurrentMissionStep);
			Settings.Save();

			TimeCycleModifier.Clear();
			_satelliteScene?.Remove();
		}

		public override void OnEnded(bool success)
		{
			if (success)
				MarkEntitesAsNotNeeded();
			else CleanUp();

			PlayerPed.MaxHealth = OriginalMaxHealth;
			PlayerPed.Health = PlayerPed.MaxHealth;
			PlayerPed.CanRagdoll = OriginalCanPlayerRagdoll;
			PlayerPed.IsExplosionProof = false;
			Settings.SetValue("mission", "current_mission_step", CurrentMissionStep);
			Settings.Save();

			TimeCycleModifier.Clear();
			_satelliteScene?.Remove();
		}

		private void MarkEntitesAsNotNeeded()
		{
			while (Aliens.Count > 0)
			{
				Ped alien = Aliens[0];
				alien.MarkAsNoLongerNeeded();
				Aliens.RemoveAt(0);
			}

			while (Ufos.Count > 0)
			{
				Vehicle craft = Ufos[0];
				if (Entity.Exists(craft.Driver))
					craft.Driver.Delete();
				craft.MarkAsNoLongerNeeded();
				Ufos.RemoveAt(0);
			}
			if (Entity.Exists(EnterenceBlocker))
			{
				EnterenceBlocker.Delete();
			}
			if (Entity.Exists(_alienEggProp))
			{
				_alienEggProp.Delete();
			}
		}

		private void CleanUp()
		{
			while (Aliens.Count > 0)
			{
				Ped alien = Aliens[0];
				alien.Delete();
				Aliens.RemoveAt(0);
			}

			while (Ufos.Count > 0)
			{
				Vehicle craft = Ufos[0];
				if (Entity.Exists(craft.Driver))
					craft.Driver.Delete();
				craft.Delete();
				Ufos.RemoveAt(0);
			}
			if (Entity.Exists(EnterenceBlocker))
			{
				EnterenceBlocker.Delete();
			}
			if (Entity.Exists(_alienEggProp))
			{
				_alienEggProp.Delete();
			}
		}
	}
}
