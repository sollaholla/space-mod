using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using GTS;
using GTS.Extensions;
using GTS.Library;
using GTS.Particles;
using GTS.Scenarios;

namespace DefaultMissions
{
    public class Mars : Scenario
    {
        public const string SettingsMissionStepString = "mission_step";
        public const string SettingsGeneralSectionString = "general";
        public const PedHash ShapeShiftModel = PedHash.Scientist01SMM;

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
            private readonly Vector3 _explosionPosition;
            private int _step;

            public MarsEngineerExplosionScene(Vector3 explosionPosition)
            {
                _explosionPosition = explosionPosition;
            }

            public bool Complete { get; set; }

            public void Start()
            {
            }

            public void Stop()
            {
                Game.Player.CanControlCharacter = true;
            }

            public void Update()
            {
                switch (_step)
                {
                    case 0:
                        Game.Player.CanControlCharacter = false;
                        var position = _explosionPosition;
                        if (!Function.Call<bool>(Hash.IS_GAMEPLAY_HINT_ACTIVE))
                            Function.Call(Hash.SET_GAMEPLAY_COORD_HINT, position.X, position.Y, position.Z, -1, 1500,
                                1000, 0);
                        World.AddExplosion(position, ExplosionType.ShipDestroy, 1000, 10f);
                        Script.Wait(150);
                        Function.Call(Hash._PLAY_AMBIENT_SPEECH1, Game.Player.Character, "GENERIC_SHOCKED_MED",
                            "SPEECH_PARAMS_FORCE");
                        Game.Player.Character.Heading = (position - Game.Player.Character.Position).ToHeading();
                        Game.Player.Character.Task.PlayAnimation("reaction@back_away@m", "0", 8.0f, -4.0f, 700,
                            AnimationFlags.None, 0.0f);
                        Script.Wait(2500);
                        Function.Call(Hash.STOP_GAMEPLAY_HINT, false);
                        Game.Player.CanControlCharacter = true;
                        _step++;
                        break;
                    case 1:
                        Complete = true;
                        break;
                }
            }
        }


        #region Fields

        #region Settings

        private int _missionStep;
        private int _enemyCount = 15;
        private float _aiWeaponDamage = 0.05f;
        private bool _noSlowMoFlag;
        private Vector3 _marsBaseEnterPos = new Vector3(-10000.83f, -9997.221f, 10001.71f);
        private Vector3 _marsBaseExitPos = new Vector3(-1966.821f, 3197.156f, 33.30999f);
        private Vector3 _marsBasePos = new Vector3(-1993.502f, 3206.331f, 32.81033f);
        private Vector3 _marsEngineerSpawn = new Vector3(-10047.26f, -8959.026f, 10000.87f);
        private Vector3 _marsEngineerRoverSpawn = new Vector3(-10000.36f, -9985.202f, 10001f);
        private float _marsEngineerRoverHeading = 73.19151f;
        private float _marsBaseRadius = 50f;

        #endregion

        private readonly Random _random = new Random();
        private readonly List<OnFootCombatPed> _aliens = new List<OnFootCombatPed>();
        private Vehicle _ufo;
        private Ped _scientist;
        private ICutScene _engineerScene;
        private Vehicle _rover;
        private Ped _engineer;
        private Vehicle _engineerShuttle;

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
            Function.Call(Hash.SET_AI_WEAPON_DAMAGE_MODIFIER, _aiWeaponDamage);

            if (_missionStep <= 1)
                SpawnAliens();
        }

        public override void OnUpdate()
        {
            switch (_missionStep)
            {
                case 0:
                    /////////////////////////////////////////////////////
                    // NOTE: We're gonna skip step 0 so that the moon 
                    // mission knows if we've been to mars already.
                    /////////////////////////////////////////////////////
                    _missionStep++;
                    Utils.ShowSubtitleWithGxt("DEFEND");
                    SaveSettings();
                    break;
                case 1:
                    ProcessAliens();
                    if (!AreAllAliensDead()) return;
                    _missionStep++;
                    break;
                case 2:
                    Utils.ShowSubtitleWithGxt("MARS_GO_TO");
                    _missionStep++;
                    SaveSettings();
                    break;
                case 3:
                    HelperFunctions.DrawWaypoint(CurrentScene, _marsBaseEnterPos);
                    if (new Trigger(_marsBasePos, _marsBaseRadius).IsInTrigger(Game.Player.Character.Position))
                    {
                        Utils.ShowSubtitleWithGxt("CHECK_SCI");
                        _missionStep++;
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
                    _missionStep++;
                    break;
                case 6:
                    Utils.ShowSubtitleWithGxt("MARS_MISSION_PT2_FIND");
                    _missionStep++;
                    break;
                case 7:
                    HelperFunctions.DrawWaypoint(CurrentScene, _marsBaseExitPos);
                    if (!new Trigger(_marsBasePos, _marsBaseRadius).IsInTrigger(Game.Player.Character.Position))
                    {
                        _missionStep++;
                        SaveSettings();
                    }
                    break;
                case 8:
                    if (!Entity.Exists(_rover))
                    {
                        var vehicleSpawn = _marsEngineerRoverSpawn.MoveToGroundArtificial();
                        if (vehicleSpawn != Vector3.Zero)
                        {
                            _rover = World.CreateVehicle("lunar", vehicleSpawn);
                            if (Entity.Exists(_rover))
                            {
                                _rover.Model.MarkAsNoLongerNeeded();
                                _rover.RadioStation = RadioStation.RadioOff;
                                _rover.Heading = _marsEngineerRoverHeading;
                            }
                        }
                    }

                    if (!Game.IsScreenFadedIn || Game.IsLoading)
                        return;

                    if (_engineerScene == null)
                    {
                        _engineerScene = new MarsEngineerExplosionScene(_marsEngineerSpawn);
                        _engineerScene.Start();
                        return;
                    }
                    _engineerScene.Update();
                    if (_engineerScene.Complete) _missionStep++;
                    break;
                case 9:
                    HelperFunctions.DrawWaypoint(CurrentScene, _marsEngineerSpawn);
                    var t = new Trigger(_marsEngineerSpawn, 500);
                    if (t.IsInTrigger(Game.Player.Character.Position))
                        _missionStep++;
                    break;
                case 10:
                    CreateEngineerFireFight();
                    break;
                case 11:
                    t = new Trigger(_marsEngineerSpawn, 200);
                    if (!t.IsInTrigger(Game.Player.Character.Position))
                        HelperFunctions.DrawWaypoint(CurrentScene, _marsEngineerSpawn);
                    ProcessAliens();
                    if (!AreAllAliensDead()) return;
                    _missionStep++;
                    break;
                case 12:
                    Function.Call(Hash._PLAY_AMBIENT_SPEECH1, _engineer, "GENERIC_THANKS",
                        "Speech_Params_Force_Shouted_Critical");
                    _engineer.Task.TurnTo(Game.Player.Character, -1);
                    _missionStep++;
                    break;
                case 13:
                    t = new Trigger(_engineer.Position, 2.5f);
                    if (!t.IsInTrigger(Game.Player.Character.Position))
                        return;
                    Utils.DisplayHelpTextWithGxt("PRESS_E");
                    if (!Game.IsControlJustPressed(2, Control.Context))
                        return;
                    var m = new Model(PedHash.MovAlien01);
                    m.Request();
                    while (!m.IsLoaded)
                        Script.Yield();

                    var newAlien = HelperFunctions.SpawnAlien(_engineer.Position - Vector3.WorldUp, checkRadius: 0,
                        weaponHash: WeaponHash.AdvancedRifle);
                    PlaySmokeEffect(newAlien.Position);
                    newAlien.Heading = _engineer.Heading;
                    _engineer.Delete();
                    _engineer = new Ped(newAlien.Handle);
                    Function.Call(Hash._PLAY_AMBIENT_SPEECH1, _engineer, "Generic_Insult_Med", "Speech_Params_Force");
                    _engineer.BlockPermanentEvents = true;
                    _missionStep++;
                    break;
                case 14:
                    _engineer.Task.AimAt(Game.Player.Character, -1);
                    Function.Call(Hash._PLAY_AMBIENT_SPEECH1, Game.Player.Character, "Generic_Shocked_High",
                        "Speech_Params_Force");
                    Game.Player.Character.Task.HandsUp(-1);
                    Script.Wait(2500);
                    _missionStep++;
                    break;
                case 15:
                    WakeUpUnderground();
                    _missionStep++;
                    break;
                case 16:
                    Function.Call(Hash.HIDE_HUD_AND_RADAR_THIS_FRAME);
                    ////////////////////////////////////////////////////
                    // TODO: Add shootout with aliens.
                    ////////////////////////////////////////////////////

                    //DEBUG
                    _missionStep++;
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
            _missionStep = Settings.GetValue(SettingsGeneralSectionString, SettingsMissionStepString, _missionStep);
            _enemyCount = Settings.GetValue(SettingsGeneralSectionString, "enemy_count", _enemyCount);
            _marsBaseEnterPos =
                ParseVector3.Read(Settings.GetValue(SettingsGeneralSectionString, "mars_base_enter_pos"),
                    _marsBaseEnterPos);
            _marsBaseExitPos = ParseVector3.Read(Settings.GetValue(SettingsGeneralSectionString, "mars_base_exit_pos"),
                _marsBaseExitPos);
            _marsBasePos = ParseVector3.Read(Settings.GetValue(SettingsGeneralSectionString, "mars_base_pos"),
                _marsBasePos);
            _marsBaseRadius = Settings.GetValue(SettingsGeneralSectionString, "mars_base_radius", _marsBaseRadius);
            _marsEngineerSpawn = ParseVector3.Read(Settings.GetValue("engineer_recover", "mars_engineer_spawn"),
                _marsEngineerSpawn);
            _marsEngineerRoverSpawn =
                ParseVector3.Read(Settings.GetValue("engineer_recover", "mars_engineer_rover_spawn"),
                    _marsEngineerRoverSpawn);
            _marsEngineerRoverHeading = Settings.GetValue("engineer_recover", "mars_engineer_rover_heading",
                _marsEngineerRoverHeading);
            _aiWeaponDamage = Settings.GetValue(SettingsGeneralSectionString, "ai_weapon_damage", _aiWeaponDamage);
            _noSlowMoFlag = Settings.GetValue("flags", "no_slow_mo_flag", _noSlowMoFlag);
        }

        private void SaveSettings()
        {
            Settings.SetValue(SettingsGeneralSectionString, SettingsMissionStepString, _missionStep);
            Settings.SetValue(SettingsGeneralSectionString, "enemy_count", _enemyCount);
            Settings.SetValue(SettingsGeneralSectionString, "ai_weapon_damage", _aiWeaponDamage);
            Settings.SetValue(SettingsGeneralSectionString, "mars_base_enter_pos", _marsBaseEnterPos);
            Settings.SetValue(SettingsGeneralSectionString, "mars_base_exit_pos", _marsBaseExitPos);
            Settings.SetValue(SettingsGeneralSectionString, "mars_base_pos", _marsBasePos);
            Settings.SetValue(SettingsGeneralSectionString, "mars_base_radius", _marsBaseRadius);
            Settings.SetValue("engineer_recover", "mars_engineer_spawn", _marsEngineerSpawn);
            Settings.SetValue("engineer_recover", "mars_engineer_rover_spawn", _marsEngineerRoverSpawn);
            Settings.SetValue("engineer_recover", "mars_engineer_rover_heading", _marsEngineerRoverHeading);
            Settings.SetValue("flags", "no_slow_mo_flag", _noSlowMoFlag);
            Settings.Save();
        }

        private void SpawnAliens()
        {
            Function.Call(Hash.SET_AI_WEAPON_DAMAGE_MODIFIER, _aiWeaponDamage);

            var center = CurrentScene.Info.GalaxyCenter;
            var spawn = center + Vector3.RelativeFront * 100;

            for (var i = 0; i < _enemyCount; i++)
            {
                var alien =
                    HelperFunctions.SpawnAlien(spawn.Around(_random.Next(25, 35)),
                        ShapeShiftModel, 5, WeaponHash.AdvancedRifle, moveToGround: false);

                if (Entity.Exists(alien))
                {
                    alien.AddBlip().Scale = 0.5f;
                    _aliens.Add(new OnFootCombatPed(alien) {Target = Game.Player.Character});
                }
            }

            var randomDistance = Function.Call<float>(Hash.GET_RANDOM_FLOAT_IN_RANGE, 25, 50);
            var spawnPoint = (spawn + Vector3.RelativeFront * 100).Around(randomDistance);
            var vehicle = HelperFunctions.SpawnUfo(spawnPoint);

            if (Entity.Exists(vehicle))
            {
                var pilot = vehicle.CreatePedOnSeat(VehicleSeat.Driver, PedHash.MovAlien01);

                if (Entity.Exists(pilot))
                {
                    pilot.SetDefaultClothes();
                    pilot.RelationshipGroup = Database.AlienRelationshipGroup;

                    Function.Call(Hash.TASK_PLANE_MISSION, pilot, vehicle, 0, Game.Player.Character, 0, 0, 0, 6, 0f, 0f,
                        0f, 0f, center.Z + 150f);
                    Function.Call(Hash._SET_PLANE_MIN_HEIGHT_ABOVE_TERRAIN, vehicle, center.Z + 150f);

                    pilot.AlwaysKeepTask = true;
                    pilot.SetCombatAttributes(CombatAttributes.AlwaysFight, true);

                    vehicle.AddBlip().Scale = 0.8f;
                    vehicle.MarkAsNoLongerNeeded();
                    _ufo = vehicle;
                    return;
                }

                vehicle.Delete();
            }
        }

        private void ProcessAliens()
        {
            var pedCopy = _aliens.ToList();

            foreach (var ped in pedCopy)
            {
                if (ped.IsDead)
                {
                    if (Blip.Exists(ped.CurrentBlip))
                        ped.CurrentBlip.Remove();

                    if (ped.Model == ShapeShiftModel)
                    {
                        if (!_noSlowMoFlag) Game.TimeScale = 0.1f;
                        var newAlien =
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
                        _aliens[_aliens.IndexOf(ped)] = new OnFootCombatPed(newAlien);
                    }

                    continue;
                }

                ped.Update();
            }

            if (Entity.Exists(_ufo))
                if (_ufo.IsDead || Entity.Exists(_ufo.Driver) && _ufo.Driver.IsDead)
                    if (Blip.Exists(_ufo.CurrentBlip))
                        _ufo.CurrentBlip.Remove();
        }

        private void DoScientistDialogue()
        {
            var interior = CurrentScene.GetInterior("MarsBaseInterior");
            if (interior == null)
            {
                Function.Call(Hash.PLAY_MISSION_COMPLETE_AUDIO, "FRANKLIN_BIG_01");
                _missionStep++;
                return;
            }

            var peds = interior.Peds.ToArray();
            if (peds.Length <= 0)
            {
                Function.Call(Hash.PLAY_MISSION_COMPLETE_AUDIO, "FRANKLIN_BIG_01");
                _missionStep++;
                return;
            }

            _scientist = _scientist ?? peds[0];

            if (!Entity.Exists(_scientist))
                return;

            HelperFunctions.DrawWaypoint(CurrentScene, _scientist.Position);

            if (!Blip.Exists(_scientist.CurrentBlip))
                _scientist.AddBlip().Color = BlipColor.Yellow;

            var t = new Trigger(_scientist.Position, 2.5f);
            if (!t.IsInTrigger(Game.Player.Character.Position))
                return;

            Utils.DisplayHelpTextWithGxt("PRESS_E");
            if (!Game.IsControlJustPressed(2, Control.Context))
                return;

            if (Blip.Exists(_scientist.CurrentBlip))
                _scientist.CurrentBlip.Remove();

            const string animDict = "gestures@f@standing@casual";
            Function.Call(Hash.REQUEST_ANIM_DICT, animDict);
            while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, animDict))
                Script.Yield();

            _scientist.Task.ClearAllImmediately();
            _scientist.Task.ChatTo(Game.Player.Character);
            Game.Player.Character.Task.LookAt(_scientist);

            Utils.ShowSubtitleWithGxt("MARS_LABEL_1");
            Script.Wait(5000);
            _scientist.Task.PlayAnimation(animDict, "gesture_no_way");
            Utils.ShowSubtitleWithGxt("MARS_LABEL_2");
            Script.Wait(5000);
            _scientist.Task.PlayAnimation(animDict, "gesture_shrug_soft");
            Utils.ShowSubtitleWithGxt("MARS_LABEL_3");
            Script.Wait(5000);
            _scientist.Task.PlayAnimation(animDict, "gesture_point");
            Utils.ShowSubtitleWithGxt("MARS_LABEL_4", 1500);
            Script.Wait(1500);
            Function.Call(Hash.PLAY_MISSION_COMPLETE_AUDIO, "FRANKLIN_BIG_01");
            Function.Call(Hash.REMOVE_ANIM_DICT, animDict);
            _missionStep++;
        }

        private void WakeUpUnderground()
        {
            Game.FadeScreenOut(1500);
            Script.Wait(1500);
            Function.Call(Hash.REQUEST_IPL, "imp_impexp_interior_placement_interior_3_impexp_int_02_milo_");
            while (!Function.Call<bool>(Hash.IS_IPL_ACTIVE,
                "imp_impexp_interior_placement_interior_3_impexp_int_02_milo_"))
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
            Game.Player.Character.Task.PlayAnimation(animDict, "ig_8_wake_up_right_player", 8.0f, -4.0f, -1,
                AnimationFlags.None, 0.0f);
            Game.Player.Character.Weapons.Select(WeaponHash.Unarmed);
            Function.Call(Hash.REMOVE_ANIM_DICT, animDict);
            Effects.Start(ScreenEffect.DrugsMichaelAliensFight, looped: true);
            Game.FadeScreenIn(1500);
            Function.Call(Hash._PLAY_AMBIENT_SPEECH1, Game.Player.Character, "Generic_Shocked_Med",
                "Speech_Params_Force_Shouted_Critical");
        }

        private void CreateEngineerFireFight()
        {
            Vector3 spawn;
            while ((spawn = _marsEngineerSpawn.MoveToGroundArtificial()) == Vector3.Zero)
                Script.Yield();

            var m = new Model(PedHash.Movspace01SMM);
            m.Request();
            while (!m.IsLoaded)
                Script.Yield();

            _engineer = World.CreatePed(m, spawn);
            _engineer.Weapons.Give(WeaponHash.AssaultSMG, 1000, true, true);
            _engineer.RelationshipGroup = Game.Player.Character.RelationshipGroup;
            _engineer.IsInvincible = true;

            m = new Model("shuttle");
            m.Request();
            while (!m.IsLoaded)
                Script.Yield();

            while ((spawn = (_engineer.Position + _engineer.RightVector * 25).MoveToGroundArtificial()) == Vector3.Zero)
                Script.Yield();

            _engineerShuttle = World.CreateVehicle(m, spawn - Vector3.WorldUp);
            _engineerShuttle.LandingGear = VehicleLandingGear.Retracted;
            _engineerShuttle.IsInvincible = true;
            _engineerShuttle.LockStatus = VehicleLockStatus.CannotBeTriedToEnter;
            _engineerShuttle.Model.MarkAsNoLongerNeeded();
            Function.Call(Hash.SET_ENTITY_RENDER_SCORCHED, _engineerShuttle, true);
            Function.Call(Hash.SET_VEHICLE_LOD_MULTIPLIER, _engineerShuttle, 0.1f);
            Function.Call(Hash.SET_ENTITY_LOD_DIST, _engineerShuttle, (ushort) 140);

            while ((spawn = (_engineer.Position + _engineer.ForwardVector * 15).MoveToGroundArtificial()) == Vector3.Zero)
                Script.Yield();

            m = new Model(PedHash.MovAlien01);
            m.Request();
            while (!m.IsLoaded)
                Script.Yield();

            var alien = HelperFunctions.SpawnAlien(spawn);
            alien.AddBlip().Scale = 0.5f;
            alien.IsOnlyDamagedByPlayer = true;
            alien.Heading = (_engineer.Position - alien.Position).ToHeading();
            _aliens.Add(new OnFootCombatPed(alien) {Target = _engineer});
            _missionStep++;
        }

        private void FinishSlomoKill(Ped pedKilled)
        {
            if (!_noSlowMoFlag)
            {
                var cam = World.CreateCamera(pedKilled.Position + pedKilled.ForwardVector * 2, Vector3.Zero, 60f);
                cam.PointAt(pedKilled);
                World.RenderingCamera = cam;

                var timeout = DateTime.UtcNow + new TimeSpan(0, 0, 2);
                while (DateTime.UtcNow < timeout)
                    Script.Yield();

                Game.TimeScale = 1.0f;
                Effects.Start(ScreenEffect.FocusOut);
                World.RenderingCamera = null;
                cam.Destroy();

                Function.Call(Hash._PLAY_AMBIENT_SPEECH1, Game.Player.Character, "GENERIC_SHOCKED_MED",
                    "SPEECH_PARAMS_FORCE");
                _noSlowMoFlag = true;
            }
        }

        private void PlaySmokeEffect(Vector3 pos)
        {
            var ptfx = new PtfxNonLooped("scr_alien_teleport", "scr_rcbarry1");

            ptfx.Request();

            while (!ptfx.IsLoaded)
                Script.Yield();

            ptfx.Play(pos, Vector3.Zero, 1);
            ptfx.Remove();
        }

        private void DeleteAliens(bool delete)
        {
            _engineerScene?.Stop();

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
            foreach (var alien in _aliens)
            {
                if (delete)
                {
                    alien.Delete();
                    continue;
                }

                alien.MarkAsNoLongerNeeded();
            }

            if (Entity.Exists(_engineerShuttle))
                if (delete) _engineerShuttle.Delete();
                else _engineerShuttle.MarkAsNoLongerNeeded();

            if (Entity.Exists(_engineer))
                if (delete) _engineer.Delete();
                else _engineer.MarkAsNoLongerNeeded();

            if (Entity.Exists(_ufo))
                if (delete)
                {
                    _ufo.Delete();
                    if (Entity.Exists(_ufo.Driver))
                        _ufo.Driver.Delete();
                }
                else
                {
                    if (Entity.Exists(_ufo.Driver))
                        _ufo.Driver.MarkAsNoLongerNeeded();
                    _ufo.MarkAsNoLongerNeeded();
                }

            if (Entity.Exists(_rover))
                if (delete && !Game.Player.Character.IsInVehicle(_rover))
                    _rover.Delete();
                else _rover.MarkAsNoLongerNeeded();
        }

        private bool AreAllAliensDead()
        {
            return _aliens.TrueForAll(x => x.IsDead) && (_ufo?.IsDead ?? true);
        }

        #endregion
    }
}