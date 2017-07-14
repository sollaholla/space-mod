using System;
using GTS.Scenarios;
using GTA.Native;
using GTA;
using System.Collections.Generic;
using GTS.Particles;
using GTA.Math;
using System.Linq;
using GTS.Library;

namespace DefaultMissions
{
    public class Mars : Scenario
    {
        public const string settings_MissionStepString = "mission_step";
        public const string settings_GeneralSectionString = "general";
        public const PedHash shapeShiftModel = PedHash.Scientist01SMM;


        #region Fields
        #region Settings
        private int missionStep;
        private int enemyCount = 15;
        private float aiWeaponDamage = 0.05f;
        #endregion

        private Random random = new Random();
        private List<OnFootCombatPed> aliens = new List<OnFootCombatPed>();
        private Vehicle ufo;
        #endregion

        #region Functions
        #region Implemented
        public override void OnAwake()
        {
            ReadSettings();
            SaveSettings();
        }

        public override void OnStart()
        {
            Game.Player.Character.CanRagdoll = false;
            Function.Call(Hash.SET_AI_WEAPON_DAMAGE_MODIFIER, aiWeaponDamage);

            if (missionStep <= 1)
            {
                SpawnAliens();
            }
        }

        public override void OnUpdate()
        {
            switch (missionStep)
            {
                case 0:
                    /////////////////////////////////////////////////////
                    // NOTE: We're gonna skip step 0 so that the moon 
                    // mission knows if we've been to mars already.
                    /////////////////////////////////////////////////////
                    missionStep++;
                    SaveSettings();
                    break;
                case 1:
                    ProcessAliens();
                    if (!AreAllAliensDead())
                        return;
                    missionStep++;
                    break;
                case 2:
                    break;
            }
        }

        public override void OnEnded(bool success)
        {
            if (success)
            {
                DeleteAliens(false);
                return;
            }

            DeleteAliens(true);
        }

        public override void OnAborted()
        {
            DeleteAliens(true);
        }
        #endregion

        private void ReadSettings()
        {
            missionStep = Settings.GetValue("general", "mission_step", missionStep);
            enemyCount = Settings.GetValue("general", "enemy_count", enemyCount);
            aiWeaponDamage = Settings.GetValue("general", "ai_weapon_damage", aiWeaponDamage);
        }

        private void SaveSettings()
        {
            Settings.SetValue("general", "mission_step", missionStep);
            Settings.SetValue("general", "enemy_count", enemyCount);
            Settings.SetValue("general", "ai_weapon_damage", aiWeaponDamage);
            Settings.Save();
        }

        private void SpawnAliens()
        {
            Function.Call(Hash.SET_AI_WEAPON_DAMAGE_MODIFIER, aiWeaponDamage);

            Vector3 center = CurrentScene.Info.GalaxyCenter;
            Vector3 spawn = center + Vector3.RelativeFront * 100;

            for (int i = 0; i < enemyCount; i++)
            {
                Ped alien =
                    HelperFunctions.SpawnAlien(spawn.Around(random.Next(25, 35)),
                    shapeShiftModel, 5, WeaponHash.AdvancedRifle, moveToGround: false);

                if (Entity.Exists(alien))
                {
                    alien.AddBlip().Scale = 0.5f;

                    aliens.Add(new OnFootCombatPed(alien) { Target = Game.Player.Character });
                }
            }

            float randomDistance = Function.Call<float>(Hash.GET_RANDOM_FLOAT_IN_RANGE, 25, 50);
            Vector3 spawnPoint = (spawn + Vector3.RelativeFront * 100).Around(randomDistance);
            Vehicle vehicle = HelperFunctions.SpawnUfo(spawnPoint);

            if (Entity.Exists(vehicle))
            {
                Ped pilot = vehicle.CreatePedOnSeat(VehicleSeat.Driver, PedHash.MovAlien01);

                if (Entity.Exists(pilot))
                {
                    pilot.SetDefaultClothes();
                    pilot.RelationshipGroup = GTS.Database.AlienRelationshipGroup;

                    Function.Call(Hash.TASK_PLANE_MISSION, pilot, vehicle, 0, Game.Player.Character, 0, 0, 0, 6, 0f, 0f, 0f, 0f, center.Z + 150f);
                    Function.Call(Hash._SET_PLANE_MIN_HEIGHT_ABOVE_TERRAIN, vehicle, center.Z + 150f);

                    pilot.AlwaysKeepTask = true;
                    pilot.SetCombatAttributes(CombatAttributes.AlwaysFight, true);

                    vehicle.AddBlip().Scale = 0.8f;
                    vehicle.MarkAsNoLongerNeeded();
                    ufo = vehicle;
                    return;
                }

                vehicle.Delete();
            }
        }

        private void ProcessAliens()
        {
            List<OnFootCombatPed> pedCopy = aliens.ToList();

            foreach (OnFootCombatPed ped in pedCopy)
            {
                if (ped.IsDead)
                {
                    if (Blip.Exists(ped.CurrentBlip))
                    {
                        ped.CurrentBlip.Remove();
                    }

                    if (ped.Model == shapeShiftModel)
                    {
                        Ped newAlien =
                            HelperFunctions.SpawnAlien(ped.Position - Vector3.WorldUp,
                            checkRadius: 0, weaponHash: WeaponHash.AdvancedRifle);

                        if (!Entity.Exists(newAlien))
                            continue;

                        PlaySmokeEffect(newAlien.Position);

                        newAlien.Heading = ped.Heading;

                        ped.Delete();

                        newAlien.Kill();

                        aliens[aliens.IndexOf(ped)] = new OnFootCombatPed(newAlien);
                    }

                    continue;
                }

                ped.Update();
            }

            if (Entity.Exists(ufo))
            {
                if (ufo.IsDead || (Entity.Exists(ufo.Driver) && ufo.Driver.IsDead))
                {
                    if (Blip.Exists(ufo.CurrentBlip))
                    {
                        ufo.CurrentBlip.Remove();
                    }
                }
            }
        }

        private void PlaySmokeEffect(Vector3 pos)
        {
            PtfxNonLooped ptfx = new PtfxNonLooped("scr_alien_teleport", "scr_rcbarry1");

            ptfx.Request();

            while (!ptfx.IsLoaded)
                Script.Yield();

            ptfx.Play(pos, Vector3.Zero, 1);
            ptfx.Remove();
        }

        private void DeleteAliens(bool delete)
        {
            Game.Player.Character.CanRagdoll = true;

            Function.Call(Hash.RESET_AI_WEAPON_DAMAGE_MODIFIER);

            foreach (OnFootCombatPed alien in aliens)
            {
                if (delete)
                {
                    alien.Delete();
                    continue;
                }

                alien.MarkAsNoLongerNeeded();
            }

            if (Entity.Exists(ufo))
            {
                if (delete)
                {
                    ufo.Delete();

                    if (Entity.Exists(ufo.Driver))
                    {
                        ufo.Driver.Delete();
                    }

                    return;
                }

                if (Entity.Exists(ufo.Driver))
                {
                    ufo.Driver.MarkAsNoLongerNeeded();
                }

                ufo.MarkAsNoLongerNeeded();
            }
        }

        private bool AreAllAliensDead()
        {
            return aliens.TrueForAll(x => x.IsDead) && ufo.IsDead;
        }
        #endregion
    }
}
