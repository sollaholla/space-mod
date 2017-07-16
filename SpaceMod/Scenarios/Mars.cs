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
        private bool noSlowMoFlag = false;
        private Vector3 marsBaseEnterPos = new Vector3(-10000.83f, -9997.221f, 10001.71f);
        private Vector3 marsBaseExitPos = new Vector3(-1966.821f, 3197.156f, 33.30999f);
        private Vector3 marsBasePos = new Vector3(-1993.502f, 3206.331f, 32.81033f);
        private Vector3 marsEngineerSpawn = new Vector3(-10047.26f, -8959.026f, 10000.87f);
        private Vector3 marsEngineerRoverSpawn = new Vector3(-10000.36f, -9985.202f, 10001f);
        private float marsEngineerRoverHeading = 73.19151f;
        private float marsBaseRadius = 50f;
        #endregion

        private Random random = new Random();
        private List<OnFootCombatPed> aliens = new List<OnFootCombatPed>();
        private Vehicle ufo;
        private Ped scientist;
        private ICutScene engineerScene;
        private Vehicle rover;
        private Ped engineer;
        private Vehicle engineerShuttle;
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
                    missionStep++;
                    break;
                case 7:
                    HelperFunctions.DrawWaypoint(CurrentScene, marsBaseExitPos);
                    if (!new Trigger(marsBasePos, marsBaseRadius).IsInTrigger(Game.Player.Character.Position))
                    {
                        missionStep++;
                        SaveSettings();
                    }
                    break;
                case 8:
                    if (!Entity.Exists(rover))
                    {
                        Vector3 vehicleSpawn = marsEngineerRoverSpawn.MoveToGroundArtificial();
                        if (vehicleSpawn != Vector3.Zero)
                        {
                            rover = World.CreateVehicle("lunar", vehicleSpawn);
                            if (Entity.Exists(rover))
                            {
                                rover.Model.MarkAsNoLongerNeeded();
                                rover.RadioStation = RadioStation.RadioOff;
                                rover.Heading = marsEngineerRoverHeading;
                            }
                        }
                    }

                    if (!Game.IsScreenFadedIn || Game.IsLoading)
                        return;

                    if (engineerScene == null)
                    {
                        engineerScene = new MarsEngineerExplosionScene(marsEngineerSpawn);
                        engineerScene.Start();
                        return;
                    }
                    engineerScene.Update();
                    if (engineerScene.Complete) missionStep++;
                    break;
                case 9:
                    HelperFunctions.DrawWaypoint(CurrentScene, marsEngineerSpawn);
                    Trigger t = new Trigger(marsEngineerSpawn, 500);
                    if (t.IsInTrigger(Game.Player.Character.Position))
                        missionStep++;
                    break;
                case 10:
                    CreateEngineerFireFight();
                    break;
                case 11:
                    t = new Trigger(marsEngineerSpawn, 200);
                    if (!t.IsInTrigger(Game.Player.Character.Position))
                        HelperFunctions.DrawWaypoint(CurrentScene, marsEngineerSpawn);
                    ProcessAliens();
                    if (!AreAllAliensDead()) return;
                    missionStep++;
                    break;
                case 12:
                    Function.Call(Hash._PLAY_AMBIENT_SPEECH1, engineer, "GENERIC_THANKS", "Speech_Params_Force_Shouted_Critical");
                    engineer.Task.TurnTo(Game.Player.Character, -1);
                    missionStep++;
                    break;
                case 13:
                    t = new Trigger(engineer.Position, 2.5f);
                    if (!t.IsInTrigger(Game.Player.Character.Position))
                        return;
                    Utils.DisplayHelpTextWithGXT("PRESS_E");
                    if (!Game.IsControlJustPressed(2, Control.Context))
                        return;
                    Model m = new Model(PedHash.MovAlien01);
                    m.Request();
                    while (!m.IsLoaded)
                        Script.Yield();

                    Ped newAlien = HelperFunctions.SpawnAlien(engineer.Position - Vector3.WorldUp, checkRadius: 0, weaponHash: WeaponHash.AdvancedRifle);
                    PlaySmokeEffect(newAlien.Position);
                    newAlien.Heading = engineer.Heading;
                    engineer.Delete();
                    engineer = new Ped(newAlien.Handle);
                    Function.Call(Hash._PLAY_AMBIENT_SPEECH1, engineer, "Generic_Insult_Med", "Speech_Params_Force");
                    engineer.BlockPermanentEvents = true;
                    missionStep++;
                    break;
                case 14:
                    engineer.Task.AimAt(Game.Player.Character, -1);
                    Function.Call(Hash._PLAY_AMBIENT_SPEECH1, Game.Player.Character, "Generic_Shocked_High", "Speech_Params_Force");
                    Game.Player.Character.Task.HandsUp(-1);
                    Script.Wait(2500);
                    missionStep++;
                    break;
                case 15:
                    WakeUpUnderground();
                    missionStep++;
                    break;
                case 16:
                    Function.Call(Hash.HIDE_HUD_AND_RADAR_THIS_FRAME);
                    ////////////////////////////////////////////////////
                    // TODO: Add shootout with aliens.
                    ////////////////////////////////////////////////////

                    //DEBUG
                    missionStep++;
                    UI.Notify("Still need something to put here... If you wanna go back to mars, use the menu.");
                    SaveSettings();
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
            missionStep = Settings.GetValue(settings_GeneralSectionString, settings_MissionStepString, missionStep);
            enemyCount = Settings.GetValue(settings_GeneralSectionString, "enemy_count", enemyCount);
            marsBaseEnterPos = ParseVector3.Read(Settings.GetValue(settings_GeneralSectionString, "mars_base_enter_pos"), marsBaseEnterPos);
            marsBaseExitPos = ParseVector3.Read(Settings.GetValue(settings_GeneralSectionString, "mars_base_exit_pos"), marsBaseExitPos);
            marsBasePos = ParseVector3.Read(Settings.GetValue(settings_GeneralSectionString, "mars_base_pos"), marsBasePos);
            marsBaseRadius = Settings.GetValue(settings_GeneralSectionString, "mars_base_radius", marsBaseRadius);
            marsEngineerSpawn = ParseVector3.Read(Settings.GetValue("engineer_recover", "mars_engineer_spawn"), marsEngineerSpawn);
            marsEngineerRoverSpawn = ParseVector3.Read(Settings.GetValue("engineer_recover", "mars_engineer_rover_spawn"), marsEngineerRoverSpawn);
            marsEngineerRoverHeading = Settings.GetValue("engineer_recover", "mars_engineer_rover_heading", marsEngineerRoverHeading);
            aiWeaponDamage = Settings.GetValue(settings_GeneralSectionString, "ai_weapon_damage", aiWeaponDamage);
            noSlowMoFlag = Settings.GetValue("flags", "no_slow_mo_flag", noSlowMoFlag);
        }

        private void SaveSettings()
        {
            Settings.SetValue(settings_GeneralSectionString, settings_MissionStepString, missionStep);
            Settings.SetValue(settings_GeneralSectionString, "enemy_count", enemyCount);
            Settings.SetValue(settings_GeneralSectionString, "ai_weapon_damage", aiWeaponDamage);
            Settings.SetValue(settings_GeneralSectionString, "mars_base_enter_pos", marsBaseEnterPos);
            Settings.SetValue(settings_GeneralSectionString, "mars_base_exit_pos", marsBaseExitPos);
            Settings.SetValue(settings_GeneralSectionString, "mars_base_pos", marsBasePos);
            Settings.SetValue(settings_GeneralSectionString, "mars_base_radius", marsBaseRadius);
            Settings.SetValue("engineer_recover", "mars_engineer_spawn", marsEngineerSpawn);
            Settings.SetValue("engineer_recover", "mars_engineer_rover_spawn", marsEngineerRoverSpawn);
            Settings.SetValue("engineer_recover", "mars_engineer_rover_heading", marsEngineerRoverHeading);
            Settings.SetValue("flags", "no_slow_mo_flag", noSlowMoFlag);
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
                        if (!noSlowMoFlag) Game.TimeScale = 0.1f;
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

            const string animDict = "gestures@f@standing@casual";
            Function.Call(Hash.REQUEST_ANIM_DICT, animDict);
            while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, animDict))
                Script.Yield();

            scientist.Task.ClearAllImmediately();
            scientist.Task.ChatTo(Game.Player.Character);
            Game.Player.Character.Task.LookAt(scientist);

            Utils.ShowSubtitleWithGXT("MARS_LABEL_1");
            Script.Wait(5000);
            scientist.Task.PlayAnimation(animDict, "gesture_no_way");
            Utils.ShowSubtitleWithGXT("MARS_LABEL_2");
            Script.Wait(5000);
            scientist.Task.PlayAnimation(animDict, "gesture_shrug_soft");
            Utils.ShowSubtitleWithGXT("MARS_LABEL_3");
            Script.Wait(5000);
            scientist.Task.PlayAnimation(animDict, "gesture_point");
            Utils.ShowSubtitleWithGXT("MARS_LABEL_4", 1500);
            Script.Wait(1500);
            Function.Call(Hash.PLAY_MISSION_COMPLETE_AUDIO, "FRANKLIN_BIG_01");
            Function.Call(Hash.REMOVE_ANIM_DICT, animDict);
            missionStep++;
        }

        private void WakeUpUnderground()
        {
            Game.FadeScreenOut(1500);
            Script.Wait(1500);
            Function.Call(Hash.REQUEST_IPL, "imp_impexp_interior_placement_interior_3_impexp_int_02_milo_");
            while (!Function.Call<bool>(Hash.IS_IPL_ACTIVE, "imp_impexp_interior_placement_interior_3_impexp_int_02_milo_"))
                Script.Yield();
            Game.Player.Character.Position = new Vector3(929.5417f, -3006.569f, -48.8378f);
            const string animDict = "safe@trevor@ig_8";
            const string clipSet = "move_m@drunk@verydrunk";
            Function.Call(Hash.REQUEST_ANIM_DICT, animDict);
            while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, animDict))
                Script.Yield();
            Function.Call(Hash.REQUEST_CLIP_SET, clipSet);
            while (!Function.Call<bool>(Hash.HAS_CLIP_SET_LOADED, clipSet))
                Script.Yield();
            Function.Call(Hash.SET_PED_MOVEMENT_CLIPSET, Game.Player.Character, clipSet, 0.25f);
            Script.Wait(1500);
            Game.Player.Character.Task.PlayAnimation(animDict, "ig_8_wake_up_right_player", 8.0f, -4.0f, -1, AnimationFlags.None, 0.0f);
            Game.Player.Character.Weapons.Select(WeaponHash.Unarmed);
            Function.Call(Hash.REMOVE_ANIM_DICT, animDict);
            Effects.Start(ScreenEffect.DrugsMichaelAliensFight, looped: true);
            Game.FadeScreenIn(1500);
            Function.Call(Hash._PLAY_AMBIENT_SPEECH1, Game.Player.Character, "Generic_Shocked_Med", "Speech_Params_Force_Shouted_Critical");
        }

        private void CreateEngineerFireFight()
        {
            Vector3 spawn;
            while ((spawn = marsEngineerSpawn.MoveToGroundArtificial()) == Vector3.Zero)
                Script.Yield();

            Model m = new Model(PedHash.Movspace01SMM);
            m.Request();
            while (!m.IsLoaded)
                Script.Yield();

            engineer = World.CreatePed(m, spawn);
            engineer.Weapons.Give(WeaponHash.AssaultSMG, 1000, true, true);
            engineer.RelationshipGroup = Game.Player.Character.RelationshipGroup;
            engineer.IsInvincible = true;

            m = new Model("shuttle");
            m.Request();
            while (!m.IsLoaded)
                Script.Yield();

            while ((spawn = (engineer.Position + engineer.RightVector * 25).MoveToGroundArtificial()) == Vector3.Zero)
                Script.Yield();

            engineerShuttle = World.CreateVehicle(m, spawn - Vector3.WorldUp);
            engineerShuttle.LandingGear = VehicleLandingGear.Retracted;
            engineerShuttle.IsInvincible = true;
            engineerShuttle.LockStatus = VehicleLockStatus.CannotBeTriedToEnter;
            engineerShuttle.Model.MarkAsNoLongerNeeded();
            Function.Call(Hash.SET_ENTITY_RENDER_SCORCHED, engineerShuttle, true);
            Function.Call(Hash.SET_VEHICLE_LOD_MULTIPLIER, engineerShuttle, 0.1f);
            Function.Call(Hash.SET_ENTITY_LOD_DIST, engineerShuttle, (UInt16)140);

            while ((spawn = (engineer.Position + engineer.ForwardVector * 15).MoveToGroundArtificial()) == Vector3.Zero)
                Script.Yield();

            m = new Model(PedHash.MovAlien01);
            m.Request();
            while (!m.IsLoaded)
                Script.Yield();

            Ped alien = HelperFunctions.SpawnAlien(spawn);
            alien.AddBlip().Scale = 0.5f;
            alien.IsOnlyDamagedByPlayer = true;
            alien.Heading = (engineer.Position - alien.Position).ToHeading();
            aliens.Add(new OnFootCombatPed(alien) { Target = engineer });
            missionStep++;
        }

        private void FinishSlomoKill(Ped pedKilled)
        {
            if (!noSlowMoFlag)
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
                noSlowMoFlag = true;
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
            engineerScene?.Stop();

            Effects.Stop();

            Game.Player.Character.CanRagdoll = true;

            Function.Call(Hash.RESET_PED_MOVEMENT_CLIPSET, Game.Player.Character, 0.25f);

            Game.Player.Character.Task.ClearAll();

            Game.TimeScale = 1.0f;

            Function.Call(Hash.RESET_AI_WEAPON_DAMAGE_MODIFIER);

            Function.Call(Hash.REMOVE_IPL, "imp_impexp_interior_placement_interior_3_impexp_int_02_milo_");

            DeleteEntities(delete);
        }

        private void DeleteEntities(bool delete)
        {
            foreach (OnFootCombatPed alien in aliens)
            {
                if (delete)
                {
                    alien.Delete();
                    continue;
                }

                alien.MarkAsNoLongerNeeded();
            }

            if (Entity.Exists(engineerShuttle))
            {
                if (delete) engineerShuttle.Delete();
                else engineerShuttle.MarkAsNoLongerNeeded();
            }

            if (Entity.Exists(engineer))
            {
                if (delete) engineer.Delete();
                else engineer.MarkAsNoLongerNeeded();
            }

            if (Entity.Exists(ufo))
            {
                if (delete)
                {
                    ufo.Delete();
                    if (Entity.Exists(ufo.Driver))
                        ufo.Driver.Delete();
                }
                else
                {
                    if (Entity.Exists(ufo.Driver))
                        ufo.Driver.MarkAsNoLongerNeeded();
                    ufo.MarkAsNoLongerNeeded();
                }
            }

            if (Entity.Exists(rover))
            {
                if (delete && !Game.Player.Character.IsInVehicle(rover))
                {
                    rover.Delete();
                }
                else rover.MarkAsNoLongerNeeded();
            }
        }

        private bool AreAllAliensDead()
        {
            return aliens.TrueForAll(x => x.IsDead) && (ufo?.IsDead ?? true);
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

        private class MarsEngineerExplosionScene : ICutScene
        {
            private int step = 0;
            private Vector3 explosionPosition;

            public MarsEngineerExplosionScene(Vector3 explosionPosition)
            {
                this.explosionPosition = explosionPosition;
            }

            public bool Complete { get; set; }

            public void Start() { }

            public void Stop()
            {
                Game.Player.CanControlCharacter = true;
            }

            public void Update()
            {
                switch (step)
                {
                    case 0:
                        Game.Player.CanControlCharacter = false;
                        Vector3 position = explosionPosition;
                        if (!Function.Call<bool>(Hash.IS_GAMEPLAY_HINT_ACTIVE))
                            Function.Call(Hash.SET_GAMEPLAY_COORD_HINT, position.X, position.Y, position.Z, -1, 1500, 1000, 0);
                        World.AddExplosion(position, ExplosionType.ShipDestroy, 1000, 10f);
                        Script.Wait(150);
                        Function.Call(Hash._PLAY_AMBIENT_SPEECH1, Game.Player.Character, "GENERIC_SHOCKED_MED", "SPEECH_PARAMS_FORCE");
                        Game.Player.Character.Heading = (position - Game.Player.Character.Position).ToHeading();
                        Game.Player.Character.Task.PlayAnimation("reaction@back_away@m", "0", 8.0f, -4.0f, 700, AnimationFlags.None, 0.0f);
                        Script.Wait(2500);
                        Function.Call(Hash.STOP_GAMEPLAY_HINT, false);
                        Game.Player.CanControlCharacter = true;
                        step++;
                        break;
                    case 1:
                        Complete = true;
                        break;
                }
            }
        }
    }
}
