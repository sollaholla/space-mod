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

// ReSharper disable PublicMembersMustHaveComments

namespace DefaultMissions
{
    /// <summary>
    /// </summary>
    public class Mars : Scenario
    {
        /// <summary>
        /// </summary>
        public const string SettingsMissionStepString = "mission_step";

        /// <summary>
        /// </summary>
        public const string SettingsGeneralSectionString = "general";

        /// <summary>
        /// </summary>
        public const PedHash ShapeShiftModel = PedHash.Scientist01SMM;

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
        private Vector3 _marsEngineerConvoPosition = new Vector3(-2015.388f, 3196.188f, 32.82997f);
        private float _marsEngineerConvoHeading = 358.7166f;
        private float _marsEngineerRoverHeading = 73.19151f;
        private float _marsBaseRadius = 50f;

        #endregion

        private readonly Random _random = new Random();
        private readonly List<OnFootCombatPed> _aliens = new List<OnFootCombatPed>();
        private Vehicle _ufo;
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
            Start_SetGameVariables();
            if (_missionStep <= 1)
                SpawnAliens();
        }

        public override void OnUpdate()
        {
            var playerCharacter = Game.Player.Character;
            Trigger t;

            switch (_missionStep)
            {
                case 0:
                    /////////////////////////////////////////////////////
                    // NOTE: We're gonna skip step 0 so that the moon 
                    // mission knows if we've been to mars already.
                    /////////////////////////////////////////////////////
                    _missionStep++;
                    GtsLibNet.ShowSubtitleWithGxt("DEFEND_AREA");
                    SaveSettings();
                    break;
                case 1:
                    ProcessEntities();
                    if (!AreAllAliensDead()) return;
                    _missionStep++;
                    break;
                case 2:
                    GtsLibNet.ShowSubtitleWithGxt("MARS_GO_TO");
                    _missionStep++;
                    SaveSettings();
                    break;
                case 3:
                    HelperFunctions.DrawWaypoint(CurrentScene, _marsBaseEnterPos);
                    t = new Trigger(_marsBasePos, _marsBaseRadius);
                    if (t.IsInTrigger(playerCharacter.Position))
                    {
                        GtsLibNet.ShowSubtitleWithGxt("CHECK_SCI");
                        _missionStep++;
                    }
                    break;
                case 4:
                    MarsBase_DoScientistDialogue();
                    break;
                case 5:
                    if (!Function.Call<bool>(Hash.IS_MISSION_COMPLETE_PLAYING))
                        return;
                    Effects.Start(ScreenEffect.SuccessNeutral, 5000);
                    ScaleFormMessages.Message.SHOW_MISSION_PASSED_MESSAGE(Game.GetGXTEntry("MARS_MISSION_PT1_PASSED"));
                    Script.Wait(4000);
                    _missionStep++;
                    break;
                case 6:
                    GtsLibNet.ShowSubtitleWithGxt("MARS_MISSION_PT2_FIND");
                    _missionStep++;
                    break;
                case 7:
                    HelperFunctions.DrawWaypoint(CurrentScene, _marsBaseExitPos);
                    t = new Trigger(_marsBasePos, _marsBaseRadius);
                    if (!t.IsInTrigger(playerCharacter.Position))
                    {
                        _missionStep++;
                        SaveSettings();
                    }
                    break;
                case 8:
                    DoExpCutscene();
                    break;
                case 9:
                    HelperFunctions.DrawWaypoint(CurrentScene, _marsEngineerSpawn);
                    t = new Trigger(_marsEngineerSpawn, 500);
                    if (t.IsInTrigger(playerCharacter.Position) && CreateEngineerFireFight())
                        _missionStep++;
                    break;
                case 10:
                    // NOTE: There used to be something here...
                    _missionStep++;
                    break;
                case 11:
                    HelperFunctions.DrawWaypoint(CurrentScene, _engineer.Position);
                    ProcessEntities();
                    if (!AreAllAliensDead()) return;
                    _missionStep++;
                    break;
                case 12:
                    Function.Call(Hash._PLAY_AMBIENT_SPEECH1, _engineer, "GENERIC_THANKS",
                        "Speech_Params_Force_Shouted_Critical");
                    _engineer.Task.TurnTo(playerCharacter, -1);
                    _missionStep++;
                    break;
                case 13:
                    t = new Trigger(_engineer.Position, 2.5f);
                    if (!t.IsInTrigger(playerCharacter.Position))
                        return;
                    GtsLibNet.DisplayHelpTextWithGxt("PRESS_E");
                    if (!Game.IsControlJustPressed(2, Control.Context))
                        return;
                    Function.Call(Hash._PLAY_AMBIENT_SPEECH1, playerCharacter, "Generic_Hi", "Speech_Params_Force");
                    playerCharacter.Task.TurnTo(_engineer);
                    _missionStep++;
                    break;
                case 14:
                    Game.FadeScreenOut(1000);
                    Script.Wait(1000);
                    playerCharacter.PositionNoOffset = _marsEngineerConvoPosition;
                    playerCharacter.Heading = _marsEngineerConvoHeading;
                    GameplayCamera.RelativeHeading = 0;
                    _engineer.PositionNoOffset = playerCharacter.Position + playerCharacter.ForwardVector * 2;
                    _missionStep++;
                    Game.FadeScreenIn(1000);
                    break;
                case 15:
                    EnginnerConvo_HaveConversation();
                    Function.Call(Hash.PLAY_MISSION_COMPLETE_AUDIO, "FRANKLIN_BIG_01");
                    _missionStep++;
                    break;
                case 16:
                    if (!Function.Call<bool>(Hash.IS_MISSION_COMPLETE_PLAYING))
                        return;
                    Effects.Start(ScreenEffect.SuccessNeutral, 5000);
                    ScaleFormMessages.Message.SHOW_MISSION_PASSED_MESSAGE(Game.GetGXTEntry("MARS_MISSION_PT2_PASSED"));
                    _missionStep++;
                    break;
                case 17:
                    Script.Wait(4500);
                    UI.ShowSubtitle(Game.GetGXTEntry("GO_TO") + " ~p~Europa~s~.", 7000);
                    EndScenario(true);
                    break;
            }
        }

        public override void OnEnded(bool success)
        {
            if (success)
            {
                CleanUp(false);
                return;
            }

            CleanUp(true);
        }

        public override void OnAborted()
        {
            CleanUp(true);
        }

        #endregion

        private void Start_SetGameVariables()
        {
            Game.Player.Character.CanRagdoll = false;
            SetAiWeaponDamage();
        }

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
            _marsEngineerConvoPosition =
                ParseVector3.Read(Settings.GetValue("engineer_recover", "mars_engineer_convo_position"),
                    _marsEngineerConvoPosition);
            _marsEngineerConvoHeading = Settings.GetValue("engineer_recover", "mars_engineer_convo_heading",
                _marsEngineerConvoHeading);
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
            Settings.SetValue("engineer_recover", "mars_engineer_convo_position", _marsEngineerConvoPosition);
            Settings.SetValue("engineer_recover", "mars_engineer_convo_heading", _marsEngineerConvoHeading);
            Settings.SetValue("flags", "no_slow_mo_flag", _noSlowMoFlag);
            Settings.Save();
        }

        private void SpawnAliens()
        {
            var center = CurrentScene.Info.GalaxyCenter;
            var spawn = center + Vector3.RelativeFront * 100;

            CreateAliens(spawn);
            CreateUfo(center, spawn);
        }

        private void SetAiWeaponDamage()
        {
            Function.Call(Hash.SET_AI_WEAPON_DAMAGE_MODIFIER, _aiWeaponDamage);
        }

        private void DoExpCutscene()
        {
            ExpCutScene_SpawnRover();

            ////////////////////////////////////////////
            // NOTE: Since we just left the mars base, 
            // it's possible the game may be loading.
            ////////////////////////////////////////////
            if (!Game.IsScreenFadedIn || Game.IsLoading)
                return;

            if (_engineerScene == null)
            {
                _engineerScene = new MarsEngineerExplosionScene(_marsEngineerSpawn);
                _engineerScene.Start();
                return;
            }
            _engineerScene.Update();
            if (_engineerScene.Complete)
                _missionStep++;
        }

        private void ExpCutScene_SpawnRover()
        {
            if (Entity.Exists(_rover))
                return;

            var vehicleSpawn = GtsLibNet.GetGroundHeightRay(_marsEngineerRoverSpawn);
            if (vehicleSpawn == Vector3.Zero) return;
            var roverModel = new Model("lunar");
            roverModel.Request(5000);
            _rover = World.CreateVehicle(roverModel, vehicleSpawn);
            if (!Entity.Exists(_rover)) return;
            _rover.Heading = _marsEngineerRoverHeading;
            roverModel.MarkAsNoLongerNeeded();
        }

        private void CreateAliens(Vector3 spawn)
        {
            for (var i = 0; i < _enemyCount; i++)
            {
                var position = spawn.Around(_random.Next(25, 35));
                var alien = GtsLibNet.CreateAlien(PedHash.Scientist01SMM, position, 0, WeaponHash.CombatPDW);
                if (!Entity.Exists(alien)) continue;
                alien.AddBlip().Scale = 0.5f;
                _aliens.Add(new OnFootCombatPed(alien) {Target = Game.Player.Character});
            }
        }

        private void CreateUfo(Vector3 center, Vector3 spawn)
        {
            var randomDistance = Function.Call<float>(Hash.GET_RANDOM_FLOAT_IN_RANGE, 25, 50);
            var spawnPoint = (spawn + Vector3.RelativeFront * 100).Around(randomDistance);
            var vehicle = HelperFunctions.SpawnUfo(spawnPoint);

            if (Entity.Exists(vehicle))
            {
                var pilotModel = new Model(GtsLibNet.GetAlienModel());
                pilotModel.Request(5000);
                var pilot = vehicle.CreatePedOnSeat(VehicleSeat.Driver, pilotModel);

                if (Entity.Exists(pilot))
                {
                    pilotModel.MarkAsNoLongerNeeded();
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

        private void ProcessEntities()
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
                        if (!_noSlowMoFlag)
                            Game.TimeScale = 0.1f;

                        var newAlien = ShapeShift_ReplacePed(ped);

                        ShapeShift_FinishSlomoKill(newAlien);

                        _aliens[_aliens.IndexOf(ped)] = new OnFootCombatPed(newAlien);
                    }
                    continue;
                }

                ped.Update();
            }

            if (!Entity.Exists(_ufo)) return;
            if (!_ufo.IsDead && (!Entity.Exists(_ufo.Driver) || !_ufo.Driver.IsDead)) return;
            if (Blip.Exists(_ufo.CurrentBlip))
                _ufo.CurrentBlip.Remove();
        }

        private Ped ShapeShift_ReplacePed(Entity ped)
        {
            var newAlien = GtsLibNet.CreateAlien(null, ped.Position, ped.Heading, WeaponHash.CombatPDW);
            newAlien.PositionNoOffset = ped.Position;
            ShapeShift_PlaySmokeEffect(newAlien.Position);
            newAlien.Heading = ped.Heading;
            ped.Delete();
            newAlien.Kill();
            return newAlien;
        }

        private void MarsBase_DoScientistDialogue()
        {
            CurrentScene.StopTile = true;

            var interior = CurrentScene.GetInterior("MarsInteriorPeds");
            var scientist = interior.Peds[0];

            HelperFunctions.DrawWaypoint(CurrentScene, scientist.Position);

            var distance = Game.Player.Character.Position.DistanceToSquared(scientist.Position);
            if (distance > 4) return;

            GtsLibNet.DisplayHelpTextWithGxt("PRESS_E");
            if (!Game.IsControlJustPressed(2, Control.Context))
                return;

            if (Blip.Exists(scientist.CurrentBlip))
                scientist.CurrentBlip.Remove();

            const string animDict = "gestures@f@standing@casual";
            Function.Call(Hash.REQUEST_ANIM_DICT, animDict);
            while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, animDict))
                Script.Yield();

            scientist.Task.ClearAllImmediately();
            scientist.Task.TurnTo(Game.Player.Character);
            scientist.Task.LookAt(Game.Player.Character);
            Game.Player.Character.Task.LookAt(scientist);

            GtsLibNet.ShowSubtitleWithGxt("MARS_LABEL_1");
            Script.Wait(5000);
            scientist.Task.PlayAnimation(animDict, "gesture_no_way");
            GtsLibNet.ShowSubtitleWithGxt("MARS_LABEL_2");
            Script.Wait(5000);
            scientist.Task.PlayAnimation(animDict, "gesture_shrug_soft");
            GtsLibNet.ShowSubtitleWithGxt("MARS_LABEL_3");
            Script.Wait(5000);
            scientist.Task.PlayAnimation(animDict, "gesture_point");
            GtsLibNet.ShowSubtitleWithGxt("MARS_LABEL_4", 1500);
            Script.Wait(1500);
            Function.Call(Hash.PLAY_MISSION_COMPLETE_AUDIO, "FRANKLIN_BIG_01");
            Function.Call(Hash.REMOVE_ANIM_DICT, animDict);
            Game.Player.Character.Task.ClearLookAt();
            _missionStep++;

            CurrentScene.StopTile = false;
        }

        private bool CreateEngineerFireFight()
        {
            return EngineerFight_CreateEngineer() && EngineerFight_CreateEngineerShuttle() &&
                   EngineerFight_CreateDecoyAlien();
            //EngineerFight_CreateEngineerShuttle();
            //EngineerFight_CreateDecoyAlien();
        }

        private bool EngineerFight_CreateEngineer()
        {
            if (GtsLibNet.GetGroundHeightRay(_marsEngineerSpawn) == Vector3.Zero)
                return false;
            var spawn = GtsLibNet.GetGroundHeightRay(_marsEngineerSpawn);
            var model = new Model(PedHash.Movspace01SMM);
            model.Request(5000);
            _engineer = World.CreatePed(model, spawn);
            _engineer.Weapons.Give(WeaponHash.AssaultSMG, 1000, true, true);
            _engineer.RelationshipGroup = Game.Player.Character.RelationshipGroup;
            _engineer.IsInvincible = true;
            return true;
        }

        private bool EngineerFight_CreateDecoyAlien()
        {
            var spawn = _engineer.Position + _engineer.ForwardVector * 15;
            if (GtsLibNet.GetGroundHeightRay(spawn) == Vector3.Zero)
                return false;
            spawn = GtsLibNet.GetGroundHeightRay(spawn);
            var alien = GtsLibNet.CreateAlien(null, spawn, -_engineer.Heading, WeaponHash.Railgun);
            alien.AddBlip().Scale = 0.5f;
            alien.IsOnlyDamagedByPlayer = true;
            alien.Heading = (_engineer.Position - alien.Position).ToHeading();
            _aliens.Add(new OnFootCombatPed(alien) {Target = _engineer});
            return true;
        }

        private bool EngineerFight_CreateEngineerShuttle()
        {
            var spawn = _engineer.Position + _engineer.RightVector * 25;
            if (GtsLibNet.GetGroundHeightRay(spawn) == Vector3.Zero)
                return false;
            spawn = GtsLibNet.GetGroundHeightRay(spawn);
            var model = new Model("shuttle");
            model.Request(5000);
            _engineerShuttle = World.CreateVehicle(model, spawn - Vector3.WorldUp);
            if (!Entity.Exists(_engineerShuttle)) return false;
            model.MarkAsNoLongerNeeded();
            _engineerShuttle.LandingGear = VehicleLandingGear.Retracted;
            _engineerShuttle.IsInvincible = true;
            _engineerShuttle.LockStatus = VehicleLockStatus.CannotBeTriedToEnter;
            Function.Call(Hash.SET_ENTITY_RENDER_SCORCHED, _engineerShuttle, true);
            Function.Call(Hash.SET_VEHICLE_LOD_MULTIPLIER, _engineerShuttle, 0.1f);
            Function.Call(Hash.SET_ENTITY_LOD_DIST, _engineerShuttle, (ushort) 140);
            return true;
        }

        private void EnginnerConvo_HaveConversation()
        {
            const string animDict = "gestures@f@standing@casual";
            Script.Wait(750);
            _engineer.Task.ChatTo(Game.Player.Character);
            Game.Player.Character.Task.LookAt(_engineer);
            Game.Player.Character.Task.StandStill(-1);

            GtsLibNet.ShowSubtitleWithGxt("MARS_LABEL_5");
            Script.Wait(5000);
            _engineer.Task.PlayAnimation(animDict, "gesture_no_way");
            GtsLibNet.ShowSubtitleWithGxt("MARS_LABEL_6");
            Script.Wait(5000);
            _engineer.Task.PlayAnimation(animDict, "gesture_shrug_soft");
            GtsLibNet.ShowSubtitleWithGxt("MARS_LABEL_7");
            Script.Wait(5000);
            _engineer.Task.PlayAnimation(animDict, "gesture_point");
            GtsLibNet.ShowSubtitleWithGxt("MARS_LABEL_8");
            Script.Wait(5000);
            Function.Call(Hash.REMOVE_ANIM_DICT, animDict);

            Game.FadeScreenOut(1000);
            Script.Wait(1000);
            _engineer.Delete();
            Game.Player.Character.Task.ClearAll();
            Script.Wait(500);
            Game.FadeScreenIn(1000);
        }

        private void ShapeShift_FinishSlomoKill(Ped pedKilled)
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

        private void ShapeShift_PlaySmokeEffect(Vector3 pos)
        {
            var ptfx = new PtfxNonLooped("scr_alien_teleport", "scr_rcbarry1");

            ptfx.Request();

            while (!ptfx.IsLoaded)
                Script.Yield();

            ptfx.Play(pos, Vector3.Zero, 1);
            ptfx.Remove();
        }

        private void CleanUp(bool delete)
        {
            if (CurrentScene != null) CurrentScene.StopTile = false;
            CleanUp_ResetGameChanges();
            CleanUpEntities(delete);
        }

        private void CleanUp_ResetGameChanges()
        {
            _engineerScene?.Stop();
            Effects.Stop();
            Game.Player.Character.CanRagdoll = true;
            Function.Call(Hash.RESET_PED_MOVEMENT_CLIPSET, Game.Player.Character, 0.25f);
            Game.Player.Character.Task.ClearAll();
            Game.TimeScale = 1.0f;
            Function.Call(Hash.RESET_AI_WEAPON_DAMAGE_MODIFIER);
            Function.Call(Hash.REMOVE_IPL, "imp_impexp_interior_placement_interior_3_impexp_int_02_milo_");
        }

        private void CleanUpEntities(bool delete)
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
            {
                if (delete) _ufo.Delete();
                else _ufo.MarkAsNoLongerNeeded();

                if (Entity.Exists(_ufo.Driver))
                    _ufo.Driver.Delete();
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