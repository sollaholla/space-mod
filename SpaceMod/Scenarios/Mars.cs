using System;
using GTS.Scenarios;
using GTA.Native;
using GTA;
using System.Collections.Generic;
using GTS.Particles;
using GTA.Math;
using System.Linq;
using GTS.Library;
using GTS.Extensions;
using GTS.Scenes.Interiors;

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
        private bool slowMoFlag = false;
        private Vector3 marsBaseEnterPos = new Vector3(-10000.83f, -9997.221f, 10001.71f);
        private Vector3 marsBasePos = new Vector3(-1993.502f, 3206.331f, 32.81033f);
        private float marsBaseRadius = 50f;
        #endregion

        private Random random = new Random();
        private List<OnFootCombatPed> aliens = new List<OnFootCombatPed>();
        private Vehicle ufo;
        private Ped scientist;
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
                    Utils.ShowSubtitleWithGXT("DEFEND");
                    SaveSettings();
                    break;
                case 1:
                    ProcessAliens();
                    if (!AreAllAliensDead()) return;
                    missionStep++;
                    break;
                case 2:
                    Utils.ShowSubtitleWithGXT("MARS_GO_TO");
                    missionStep++;
                    SaveSettings();
                    break;
                case 3:
                    HelperFunctions.DrawWaypoint(CurrentScene, marsBaseEnterPos);
                    if (new Trigger(marsBasePos, marsBaseRadius).IsInTrigger(Game.Player.Character.Position))
                    {
                        Utils.ShowSubtitleWithGXT("CHECK_SCI");
                        missionStep++;
                    }
                    break;
                case 4:
                    if (!Game.IsScreenFadedIn || Game.IsLoading)
                        return;
                    DoScientistDialogue();
                    break;
                case 5:
                    if (!Function.Call<bool>(Hash.IS_MISSION_COMPLETE_PLAYING))
                        return;
                    Effects.Start(ScreenEffect.SuccessNeutral, 5000);
                    ScaleFormMessages.Message.SHOW_MISSION_PASSED_MESSAGE(Game.GetGXTEntry("MARS_MISSION_PT1_PASS"));
                    Script.Wait(4000);
                    missionStep++;
                    break;
                case 6:
                    Utils.ShowSubtitleWithGXT("MARS_MISSION_PT2_FIND");
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
            missionStep = Settings.GetValue         (settings_GeneralSectionString, settings_MissionStepString, missionStep);
            enemyCount = Settings.GetValue          (settings_GeneralSectionString, "enemy_count", enemyCount);
            marsBaseEnterPos = ParseVector3.Read    (Settings.GetValue(settings_GeneralSectionString, "mars_base_enter_pos"), marsBaseEnterPos);
            marsBasePos = ParseVector3.Read         (Settings.GetValue(settings_GeneralSectionString, "mars_base_pos"), marsBasePos);
            marsBaseRadius = Settings.GetValue      (settings_GeneralSectionString, "mars_base_radius", marsBaseRadius);
            aiWeaponDamage = Settings.GetValue      (settings_GeneralSectionString, "ai_weapon_damage", aiWeaponDamage);
            slowMoFlag = Settings.GetValue          ("flags", "slow_mo_flag", slowMoFlag);
        }

        private void SaveSettings()
        {
            Settings.SetValue(settings_GeneralSectionString, settings_MissionStepString, missionStep);
            Settings.SetValue(settings_GeneralSectionString, "enemy_count", enemyCount);
            Settings.SetValue(settings_GeneralSectionString, "ai_weapon_damage", aiWeaponDamage);
            Settings.SetValue(settings_GeneralSectionString, "mars_base_enter_pos", marsBaseEnterPos);
            Settings.SetValue(settings_GeneralSectionString, "mars_base_pos", marsBasePos);
            Settings.SetValue(settings_GeneralSectionString, "mars_base_radius", marsBaseRadius);
            Settings.SetValue("flags", "slow_mo_flag", slowMoFlag);
            Settings.Save();
        }

        private void DoScientistDialogue()
        {
            Interior interior = CurrentScene.GetInterior("MarsBaseInterior");
            if (interior == null)
            {
                Function.Call(Hash.PLAY_MISSION_COMPLETE_AUDIO, "FRANKLIN_BIG_01");
                missionStep++;
                return;
            }

            Ped[] peds = interior.Peds.ToArray();
            if (peds.Length <= 0)
            {
                Function.Call(Hash.PLAY_MISSION_COMPLETE_AUDIO, "FRANKLIN_BIG_01");
                missionStep++;
                return;
            }

            scientist = scientist ?? peds[0];

            if (!Entity.Exists(scientist))
                return;

            HelperFunctions.DrawWaypoint(CurrentScene, scientist.Position);

            if (!Blip.Exists(scientist.CurrentBlip))
                scientist.AddBlip().Color = BlipColor.Yellow;

            Trigger t = new Trigger(scientist.Position, 2.5f);
            if (!t.IsInTrigger(Game.Player.Character.Position))
                return;

            Utils.DisplayHelpTextWithGXT("PRESS_E");
            if (!Game.IsControlJustPressed(2, Control.Context))
                return;

            if (Blip.Exists(scientist.CurrentBlip))
                scientist.CurrentBlip.Remove();

            Utils.ShowSubtitleWithGXT("MARS_LABEL_1");
            Script.Wait(5000);
            Utils.ShowSubtitleWithGXT("MARS_LABEL_2");
            Script.Wait(5000);
            Utils.ShowSubtitleWithGXT("MARS_LABEL_3");
            Script.Wait(5000);
            Utils.ShowSubtitleWithGXT("MARS_LABEL_4", 1500);
            Script.Wait(1500);
            Function.Call(Hash.PLAY_MISSION_COMPLETE_AUDIO, "FRANKLIN_BIG_01");
            missionStep++;
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
                        if (!slowMoFlag) Game.TimeScale = 0.1f;
                        Ped newAlien =
                            HelperFunctions.SpawnAlien(ped.Position - Vector3.WorldUp,
                            checkRadius: 0, weaponHash: WeaponHash.AdvancedRifle);

                        if (!Entity.Exists(newAlien))
                        {
                            Game.TimeScale = 1.0f;
                            continue;
                        }
                        PlaySmokeEffect(newAlien.Position);
                        newAlien.Heading = ped.Heading;
                        ped.Delete();
                        newAlien.Kill();
                        FinishSlomoKill(newAlien);
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

        private void FinishSlomoKill(Ped pedKilled)
        {
            if (!slowMoFlag)
            {
                Camera cam = World.CreateCamera(pedKilled.Position + pedKilled.ForwardVector * 2, Vector3.Zero, 60f);
                cam.PointAt(pedKilled);
                World.RenderingCamera = cam;

                DateTime timeout = DateTime.UtcNow + new TimeSpan(0, 0, 2);
                while (DateTime.UtcNow < timeout)
                {
                    Script.Yield();
                }

                Game.TimeScale = 1.0f;
                Effects.Start(ScreenEffect.FocusOut);
                World.RenderingCamera = null;
                cam.Destroy();

                Function.Call(Hash._PLAY_AMBIENT_SPEECH1, Game.Player.Character, "GENERIC_SHOCKED_MED", "SPEECH_PARAMS_FORCE");
                slowMoFlag = true;
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
            Game.TimeScale = 1.0f;

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

        private class MarsGreenhouseEvent : IEvent
        {
            public bool Complete { get; set; }

            public void Start()
            {
            }

            public void Stop()
            {
            }

            public void Update()
            {
            }
        }
    }
}
