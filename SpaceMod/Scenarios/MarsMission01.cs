using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Native;
using GTA.Math;
using NativeUI;
using SpaceMod;
using SpaceMod.Extensions;
using SpaceMod.Scenario;
using SpaceMod.Scenes;

namespace DefaultMissions
{
	public class MarsMission01 : CustomScenario
	{
		private readonly string _ufoModelName = "zanufo";
		private Model _ufoModel;

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
			if (CurrentMissionStep == 0)
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
			if (CurrentMissionStep < 1)
			{
				if (!Entity.Exists(EnterenceBlocker))
				{
					EnterenceBlocker =
						new Prop(
							World.CreateProp("lts_prop_lts_elecbox_24b", CurrentScene.SceneData.Ipls[0].Teleports[0].Start, Vector3.Zero,
								false,
								false).Handle) {IsVisible = false};

				}
			}

			switch (CurrentMissionStep)
			{
				case 0:
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
					IplData currentIplData = CurrentScene.SceneData.CurrentIplData;

					if (currentIplData?.Name == "Mars/mbi2" && currentIplData.CurrentIpl.IsActive)
					{
						Vector3[] spawnPoints = {
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
							alien.AddBlip().Scale = 0.7f;
							Aliens.Add(alien);
						}

						CurrentMissionStep++;
					}
					break;
				case 3:
					Aliens.ForEach(UpdateAlien2);

					if (Aliens.All(a => a.IsDead))
					{
						UI.ShowSubtitle("Check the satellite systems.", 7000);
						CurrentMissionStep++;
					}
					break;
				case 4:

					break;
			}
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
				ufo.Speed = Vector3.Angle(ufo.ForwardVector, dir) < 5 ? 5 : 0;

				return;
			}

			ufo.Speed = 30;

		}

		public void UpdateAlien(Ped alienPed)
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

		private void UpdateAlien2(Ped ped)
		{
			IplData currentIplData = CurrentScene.SceneData.CurrentIplData;
			ped.FreezePosition = currentIplData?.CurrentIpl != null &&
			                     currentIplData.Name == "mbi2_enter" &&
			                     currentIplData.CurrentIpl.IsActive;

			if (ped.IsDead && Blip.Exists(ped.CurrentBlip))
			{
				ped.CurrentBlip.Remove();
			}
		}

		public override void OnAborted()
		{
			CleanUp();
			Settings.SetValue("mission", "current_mission_step", CurrentMissionStep);
			Settings.Save();

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
