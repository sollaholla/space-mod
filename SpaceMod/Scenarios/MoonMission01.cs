﻿using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;
using SpaceMod;
using SpaceMod.DataClasses;

namespace SollahollaMissions
{
    public class MoonMission01 : CustomScenario
    {
        public MoonMission01()
        {
            Aliens = new List<Ped>();
            SpaceCrafts = new List<Entity>();
            Random = new Random();
            OriginalPlayerHealth = PlayerPed.MaxHealth;
            PlayerPed.MaxHealth = 1500;
            PlayerPed.Health = PlayerPed.MaxHealth;
            OriginalCanRagdollState = PlayerPed.CanRagdoll;
            PlayerPed.CanRagdoll = false;
        }

        public int MissionStep { get; private set; }

        public List<Ped> Aliens { get; }

        public List<Entity> SpaceCrafts { get; }

        public int OriginalPlayerHealth { get; }

        public bool OriginalCanRagdollState { get; }

        public Ped PlayerPed => Game.Player.Character;

        public Random Random { get; }

        public Vector3 PlayerPosition {
            get { return PlayerPed.Position; }
            set { PlayerPed.Position = value; }
        }

        public override void Start()
        {
            SpawnEnemies();
            PlayerPed.Weapons.Give(WeaponHash.MicroSMG, 750, true, true);
            UI.Notify("You grabbed a ~b~weapon~s~ out of your space-craft.");
            Utilities.DisplayHelpTextThisFrame("Your space suit has been equipped with heavy armor to withstand the alien weapons.");
        }

        private void SpawnEnemies()
        {
            var origin = PlayerPosition.Around(100f);

            for (var i = 0; i < 15; i++)
            {
                Vector3 position = origin.Around(Random.Next(50, 75));
                Vector3 artificial = TryToGetGroundHeight(position);
                if (artificial != Vector3.Zero) position = artificial;

                Ped ped = Utilities.CreateAlien(position, WeaponHash.Railgun);
                ped.SetDefaultClothes();
                ped.Health = 3750;
                ped.AlwaysDiesOnLowHealth = false;
                ped.CanRagdoll = false;
                ped.IsOnlyDamagedByPlayer = true;

                Blip blip = ped.AddBlip();
                blip.Name = "Alien";
                blip.Scale = 0.7f;

                Aliens.Add(ped);
            }

            for (var i = 0; i < 5; i++)
            {
                Vector3 position = origin.Around(75);
                Vector3 artifical = TryToGetGroundHeight(position);
                if (artifical != Vector3.Zero) position = artifical;

                Prop spaceCraft = World.CreateProp("ufo_zancudo", position + new Vector3(0, 0, 7.5f), Vector3.Zero,
                    false, false);

                spaceCraft.FreezePosition = true;
                spaceCraft.MaxHealth = 1000;
                spaceCraft.Health = spaceCraft.MaxHealth;

                Blip blip = spaceCraft.AddBlip();
                blip.Sprite = BlipSprite.SonicWave;
                blip.Scale = 0.7f;
                blip.Name = "UFO";

                SpaceCrafts.Add(spaceCraft);
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
                    SpaceCrafts.ForEach(UpdateSpaceCraft);

                    List<Entity> concatList = Aliens.Concat(SpaceCrafts).ToList();
                    if (!concatList.All(entity => entity.IsDead)) return;
                    BigMessageThread.MessageInstance.ShowMissionPassedMessage("~r~enemies eliminated");
                    MissionStep++;
                    break;
                case 1:
                    Utilities.DisplayHelpTextThisFrame("Press ~INPUT_SPECIAL_ABILITY_SECONDARY~ to plant your flag!");
                    Game.DisableControlThisFrame(2, Control.SpecialAbilitySecondary);
                    if (!Game.IsDisabledControlJustPressed(2, Control.SpecialAbilitySecondary)) return;
                    PlayerPed.Task.PlayAnimation("pickup_object", "pickup_low");
                    Prop prop = World.CreateProp("ind_prop_dlc_flag_01", PlayerPosition + PlayerPed.ForwardVector - PlayerPed.UpVector,
                        Vector3.Zero, false, false);
                    if (prop != null)
                    {
                        prop.FreezePosition = true;
                        prop.MarkAsNoLongerNeeded();
                    }
                    MissionStep++;
                    break;
                case 2:
                    BigMessageThread.MessageInstance.ShowMissionPassedMessage("~y~scenario complete!");
                    EndScenario(true);
                    break;
            }
        }

        private void UpdateAlien(Ped alienPed)
        {
            if (alienPed.IsDead)
            {
                if (alienPed.CurrentBlip.Exists())
                {
                    alienPed.MarkAsNoLongerNeeded();
                    alienPed.CurrentBlip.Remove();
                    alienPed.CanRagdoll = true;

                    if (Aliens.All(alien => alien.IsDead))
                    {
                        Utilities.DisplayHelpTextThisFrame("You can use the alien's rifles to eliminate their motherships.");
                    }
                }

                return;
            }

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
        }

        public override void OnAborted()
        {
            CleanUp();
        }

        private void MarkEntitesAsNotNeeded()
        {
            while (Aliens.Count > 0)
            {
                Ped Alien = Aliens[0];
                Alien.MarkAsNoLongerNeeded();
                Aliens.RemoveAt(0);
            }

            while (SpaceCrafts.Count > 0)
            {
                Entity Craft = SpaceCrafts[0];
                Craft.MarkAsNoLongerNeeded();
                SpaceCrafts.RemoveAt(0);
            }
        }

        private void CleanUp()
        {
            while (Aliens.Count > 0)
            {
                Ped Alien = Aliens[0];
                Alien.Delete();
                Aliens.RemoveAt(0);
            }

            while (SpaceCrafts.Count > 0)
            {
                Entity Craft = SpaceCrafts[0];
                Craft.Delete();
                SpaceCrafts.RemoveAt(0);
            }
        }
    }
}
