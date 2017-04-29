using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DefaultMissions.Scenes.SceneTypes;
using GTA;
using GTA.Native;
using GTA.Math;
using NativeUI;
using SpaceMod;
using SpaceMod.Lib;
using SpaceMod.Scenario;
using SpaceMod.Scenes;

namespace DefaultMissions
{
	public class MarsMission01 : CustomScenario
	{
		private readonly string _ufoModelName = "zanufo";

		private Model _ufoModel;

		private bool _didNotify;
		private bool _didSetTimeCycle;
		private bool _isCheckingSats;

		private readonly MarsMissionSatelliteScene _satelliteScene;

		//Position: X:-2013.115 Y:3198.238 Z:32.81007
		private readonly Vector3 _computerPos = new Vector3(-2013.115f, 3198.238f, 32.81007f);

		public MarsMission01()
		{

			Aliens = new List<Ped>();
			Ufos = new List<Vehicle>();
			OriginalCanPlayerRagdoll = PlayerPed.CanRagdoll;
			OriginalMaxHealth = PlayerPed.MaxHealth;
			PlayerPed.MaxHealth = 1500;
			PlayerPed.Health = PlayerPed.MaxHealth;
			PlayerPed.CanRagdoll = false;
			PlayerPed.IsExplosionProof = true;

			_satelliteScene = new MarsMissionSatelliteScene();

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

		public bool OriginalCanPlayerRagdoll { get; }

		public int OriginalMaxHealth { get; }

		public Prop EnterenceBlocker { get; private set; }

		public Vector3 PlayerPosition {
			get { return PlayerPed.Position; }
			set { PlayerPed.Position = value; }
		}

		public override void Start()
		{
			if (CurrentMissionStep == 0 && CurrentScene.SceneFile == "MarsSurface.space")
			{
				SpawnEntities();
			}
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
				ped.SetCombatAttributes(SpaceModLib.CombatAttributes.AlwaysFight, true);

				Blip blip = spaceCraft.AddBlip();
				blip.Name = "UFO";
				blip.Color = BlipColor.Green;

				Ufos.Add(spaceCraft);
			}
		}

		private static Vector3 TryToGetGroundHeight(Vector3 position)
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
				if (!Entity.Exists(EnterenceBlocker))
				{
					EnterenceBlocker =
						new Prop(
							World.CreateProp("lts_prop_lts_elecbox_24b", CurrentScene.SceneData.Ipls[0].Teleports[0].Start, Vector3.Zero,
								false,
								false).Handle)
						{ IsVisible = false };
				}
			}

			switch (CurrentMissionStep)
			{
				case 0:
					if (Entity.Exists(EnterenceBlocker) && PlayerPed.IsTouching(EnterenceBlocker))
					{
						if (!_didNotify)
						{
							SpaceModLib.DisplayHelpTextThisFrame("You need to eliminate the remaining enemies first!");
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
						BigMessageThread.MessageInstance.ShowMissionPassedMessage("~r~ enemies eliminated");
						CurrentMissionStep++;
					}
					break;
				case 1:
					UI.ShowSubtitle("Go into the ~b~base~s~ and make sure the satellite systems weren't destroyed.", 7000);
					if (Entity.Exists(EnterenceBlocker))
					{
						EnterenceBlocker.Delete();
					}
					CurrentMissionStep++;
					break;
				case 2:
					if (IsInInterior("MarsSurface.space", "Mars/mbi2"))
					{
						Vector3[] spawnPoints =
						{
							new Vector3(-2014.449f, 3216.207f, 32.81112f),
							new Vector3(-1989.808f, 3212.001f, 32.81171f),
							new Vector3(-1991.477f, 3205.936f, 32.81038f),
							new Vector3(-1997.719f, 3211.335f, 32.83896f)
						};
						for (var i = 0; i < spawnPoints.Length; i++)
						{
							var spawn = spawnPoints[i];
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
					Aliens.ForEach(a => UpdateAlien2(a, "MarsSurface.space", "Mars/mbi2"));
					if (Aliens.All(a => a.IsDead))
					{
						UI.ShowSubtitle("Check the satellite systems.", 7000);
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
							SpaceModLib.DisplayHelpTextThisFrame("Press ~INPUT_CONTEXT~ to check the satellites.");
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
						SpaceModLib.DisplayHelpTextThisFrame(
							"~b~You:~s~ Holy sh*t! This is on Europa. " +
							"These f**kers are in more places than I expected. " +
							"I better go check it out.~n~~n~Press ~INPUT_CONTEXT~ to continue.");
						Game.DisableControlThisFrame(2, Control.Talk);
						Game.DisableControlThisFrame(2, Control.Context);
						if (Game.IsDisabledControlJustPressed(2, Control.Context))
						{
							_satelliteScene.Remove();
							TimeCycleModifier.Clear();
							CurrentMissionStep++;
						}
						if (!_didSetTimeCycle)
						{
							TimeCycleModifier.Set("CAMERA_secuirity_FUZZ", 1f);
							_didSetTimeCycle = true;
						}
					}
					break;
				case 5:
					BigMessageThread.MessageInstance.ShowMissionPassedMessage("~y~mars secure");
					Function.Call(Hash.PLAY_MISSION_COMPLETE_AUDIO, "FRANKLIN_BIG_01");
					UI.ShowSubtitle("Go to ~b~Europa~s~ to investigate the ~r~mothership~s~.", 7000);
					SpaceModLib.DisplayHelpTextThisFrame("~b~You:~s~ This sh*t is getting out of hand.");
					CurrentMissionStep++;
					break;
				case 6:
					if (IsInInterior("EuropaSurface.space", "Europa/ufo_interior"))
					{
						Vector3[] spawnPoints =
						{
							new Vector3(-10015.6f, -9970.742f, 10041.6f),
							new Vector3(-10021.1f, -9975.481f, 10041.6f),
							new Vector3(-10024.99f, -9963.521f, 10041.6f),
							new Vector3(-10015.08f, -9968.357f, 10041.6f),
						};
						for (int i = 0; i < spawnPoints.Length; i++)
						{
							var spawn = spawnPoints[i];
							var alien = SpaceModLib.CreateAlien(spawn, WeaponHash.MicroSMG);
							alien.Heading = (PlayerPed.Position - alien.Position).ToHeading();
							alien.Task.FightAgainst(PlayerPed, -1);
							Aliens.Add(alien);
						}
						CurrentMissionStep++;
					}
					break;
				case 7:
					if (IsInInterior("EuropaSurface.space", "Europa/ufo_interior"))
					{
						Aliens.ForEach(a => UpdateAlien2(a, "EuropaSurface.space", "Europa/ufo_interior"));
						if (Aliens.All(a => a.IsDead))
						{
							BigMessageThread.MessageInstance.ShowMissionPassedMessage("~y~europa secure");
							Function.Call(Hash.PLAY_MISSION_COMPLETE_AUDIO, "FRANKLIN_BIG_01");
							Aliens.Clear();
							CurrentMissionStep++;
						}
					}
					break;
				case 8:
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
			{
				return;
			}

			if (!string.IsNullOrEmpty(CurrentScene.SceneData.CurrentIplData?.Name))
			{
				ufo.FreezePosition = CurrentScene.SceneData.CurrentIplData.Name == "Mars/mbi2";
			}

			if (ufo.IsDead || (Entity.Exists(ufo.Driver) && ufo.Driver.IsDead) || !ufo.IsDriveable)
			{
				if (Entity.Exists(ufo.Driver) && !ufo.Driver.IsDead)
				{
					ufo.Driver.Kill();
				}

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
			{
				alienPed.FreezePosition = CurrentScene.SceneData.CurrentIplData.Name == "Mars/mbi2";
			}

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
			{
				alienPed.Task.RunTo(PlayerPed.Position, true);
			}
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
			{
				MarkEntitesAsNotNeeded();
			}
			else
			{
				CleanUp();
			}

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
		}

	}
}
