using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using SpaceMod.Lib;
using SpaceMod.Scenario;

namespace DefaultMissions
{
    public class MoonMission01 : CustomScenario
    {
        private readonly string _ufoModelName = "zanufo";
        private bool isFlagPlaced = false;
        private Model _ufoModel;
        private Vector3 flagPosition;
        private Prop flag;

        public MoonMission01()
        {
            Aliens = new List<AlienData>();
            Ufos = new List<Entity>();
            Random = new Random();
            OriginalPlayerHealth = PlayerPed.MaxHealth;
            PlayerPed.MaxHealth = 1500;
            PlayerPed.Health = PlayerPed.MaxHealth;
            OriginalCanRagdollState = PlayerPed.CanRagdoll;
            PlayerPed.CanRagdoll = false;
            PlayerPed.IsExplosionProof = true;

            _ufoModelName = Settings.GetValue("settings", "ufo_model", _ufoModelName);
            flagPosition = V3Parse.Read(Settings.GetValue("settings", "flag_position"), Vector3.Zero);
            Settings.SetValue("settings", "ufo_model", _ufoModelName);
            Settings.Save();
        }

        public int MissionStep { get; private set; }

        public List<AlienData> Aliens { get; }

        public List<Entity> Ufos { get; }

        public int OriginalPlayerHealth { get; }

        public bool OriginalCanRagdollState { get; }

        public Ped PlayerPed => Game.Player.Character;

        public Random Random { get; }

        public Vector3 PlayerPosition {
            get { return PlayerPed.Position; }
            set { PlayerPed.Position = value; }
        }

        public override void OnEnterScene()
        {
            if(!isFlagPlaced && Settings.GetValue("scenario_config", "complete", false) == true)
            {
                flag = World.CreateProp("ind_prop_dlc_flag_01", flagPosition, Vector3.Zero, false, false);

                if (flag != null)
                {
                    flag.FreezePosition = true;
                    flag.MarkAsNoLongerNeeded();
                }
            }
        }

        public override void Start()
        {
            SpawnEnemies();
            PlayerPed.Weapons.Give(WeaponHash.MicroSMG, 750, true, true);
            SpaceModLib.NotifyWithGXT("GTS_LABEL_23");
            SpaceModLib.DisplayHelpTextWithGXT("GTS_LABEL_2");
        }

        private void SpawnEnemies()
        {
            var origin = PlayerPosition.Around(100f);

            for (var i = 0; i < 15; i++)
            {
                Vector3 position = origin.Around(Random.Next(50, 75));
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

                Aliens.Add(new AlienData { Ped = ped, StoppingDistance = new Random().Next(25, 28) });
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

            for (var i = 0; i < 5; i++)
            {
                Vector3 position = origin.Around(75);
                Vector3 artifical = TryToGetGroundHeight(position);
                if (artifical != Vector3.Zero) position = artifical;
	            position = position + new Vector3(0, 0, 7.5f);

				if (World.GetNearbyVehicles(position, 50).Any())
		            continue;
				if (World.GetNearbyProps(position, 25).Any())
					continue;

				Vehicle spaceCraft = World.CreateVehicle(_ufoModel, position);
	            spaceCraft.Heading = (spaceCraft.Position - PlayerPosition).ToHeading();
				spaceCraft.FreezePosition = true;
                spaceCraft.MaxHealth = 1000;
                spaceCraft.Health = spaceCraft.MaxHealth;

                Blip blip = spaceCraft.AddBlip();
                blip.Sprite = BlipSprite.SonicWave;
                blip.Scale = 0.7f;
                blip.Name = "UFO";

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
            switch (MissionStep)
            {
                case 0:
                    Aliens.ForEach(UpdateAlien);
                    Ufos.ForEach(UpdateSpaceCraft);

                    List<Entity> concatList = Aliens.Select(x => x.Ped).Concat(Ufos).ToList();
                    if (!concatList.All(entity => entity.IsDead)) return;
                    ScaleFormMessages.Message.SHOW_MISSION_PASSED_MESSAGE(Game.GetGXTEntry("BM_LABEL_3"));
                    MissionStep++;
                    break;
                case 1:
                    SpaceModLib.DisplayHelpTextWithGXT("GTS_LABEL_3");
                    Game.DisableControlThisFrame(2, Control.SpecialAbilitySecondary);
                    if (!Game.IsDisabledControlJustPressed(2, Control.SpecialAbilitySecondary)) return;
                    PlayerPed.Task.PlayAnimation("pickup_object", "pickup_low");
                    flagPosition = PlayerPosition + PlayerPed.ForwardVector - PlayerPed.UpVector;
                    flag = World.CreateProp("ind_prop_dlc_flag_01", flagPosition, Vector3.Zero, false, false);
                    isFlagPlaced = true;

                    Settings.SetValue("settings", "flag_position", flagPosition);
                    Settings.Save();

                    if (flag != null)
                    {
                        flag.FreezePosition = true;
                        flag.MarkAsNoLongerNeeded();
                    }
                    MissionStep++;
                    break;
                case 2:
                    ScaleFormMessages.Message.SHOW_MISSION_PASSED_MESSAGE(Game.GetGXTEntry("BM_LABEL_4"));
                    EndScenario(true);
                    break;
            }
        }

        private void UpdateAlien(AlienData alien)
        {
            var alienPed = alien.Ped;
            if (alienPed.IsDead)
            {
                if (alienPed.CurrentBlip.Exists())
                {
                    alienPed.MarkAsNoLongerNeeded();
                    alienPed.CurrentBlip.Remove();
                    alienPed.CanRagdoll = true;

                    if (Aliens.All(a => a.Ped.IsDead) 
						&& Ufos.Count > 0 && Ufos.Any(u => !u.IsDead))
                    {
                        SpaceModLib.DisplayHelpTextWithGXT("GTS_LABEL_4");
                    }
                }

                return;
            }

            SpaceModLib.ArtificialDamage(alienPed, PlayerPed, 1.5f, 75);

            float distance = Vector3.Distance(PlayerPosition, alienPed.Position);

            if (distance > 25)
            {
                alienPed.Task.RunTo(PlayerPed.Position, true);
            }
            else
            {
                alienPed.Task.FightAgainst(PlayerPed);
            }
        }

        private static void UpdateSpaceCraft(Entity spaceCraft)
        {
            if (spaceCraft.IsDead)
            {
                if (spaceCraft.CurrentBlip.Exists())
                {
                    Vector3 pos = spaceCraft.Position;
                    Function.Call(Hash._SET_PTFX_ASSET_NEXT_CALL, "core");
                    Function.Call(Hash.START_PARTICLE_FX_NON_LOOPED_AT_COORD, "exp_grd_grenade_lod", pos.X, pos.Y, pos.Z, 0, 0, 0, 10.0f, false, false, false);
                    World.AddExplosion(pos, ExplosionType.Car, 0, 1.5f, true, false);
                    spaceCraft.FreezePosition = false;
                    spaceCraft.CurrentBlip.Remove();
                }
            }
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

            PlayerPed.MaxHealth = OriginalPlayerHealth;
            PlayerPed.Health = PlayerPed.MaxHealth;
            PlayerPed.CanRagdoll = OriginalCanRagdollState;
            PlayerPed.IsExplosionProof = false;
        }

        public override void OnAborted()
        {
            PlayerPed.MaxHealth = OriginalPlayerHealth;
            PlayerPed.Health = PlayerPed.MaxHealth;
            PlayerPed.CanRagdoll = OriginalCanRagdollState;
            PlayerPed.IsExplosionProof = false;
            flag?.Delete();

            CleanUp();
        }

        private void MarkEntitesAsNotNeeded()
        {
            while (Aliens.Count > 0)
            {
                var alien = Aliens[0];
                alien.Ped.MarkAsNoLongerNeeded();
                Aliens.RemoveAt(0);
            }

            while (Ufos.Count > 0)
            {
                Entity craft = Ufos[0];
                craft.MarkAsNoLongerNeeded();
                Ufos.RemoveAt(0);
            }
        }

        private void CleanUp()
        {
            while (Aliens.Count > 0)
            {
                var alien = Aliens[0];
                alien.Ped.Delete();
                Aliens.RemoveAt(0);
            }

            while (Ufos.Count > 0)
            {
                Entity craft = Ufos[0];
                craft.Delete();
                Ufos.RemoveAt(0);
            }
        }
    }
}
