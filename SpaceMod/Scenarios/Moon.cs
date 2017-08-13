using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using GTS;
using GTS.Extensions;
using GTS.Library;
using GTS.Scenarios;

namespace DefaultMissions
{
    public class Moon : Scenario
    {
        private class MoonSatelliteCutscene : ICutScene
        {
            private const string TextureDict = "securitycam";
            private const string TextureName = "securitycam_box";
            private readonly string _planetModel;
            private readonly Random _random = new Random();
            private Camera _camera;
            private Prop _planetProp;
            private Vector3 _pos;
            private float _seconds;
            private int _step;
            private Vehicle _ufo;

            public MoonSatelliteCutscene(string planetModel)
            {
                _planetModel = planetModel;
            }

            public bool Complete { get; set; }

            public void Start()
            {
                var spawn = new Vector3(10000, 10000, 50000);
                _camera = World.CreateCamera(spawn, new Vector3(270, 0, 0), 60);
                World.RenderingCamera = _camera;
                Game.FadeScreenOut(0);
                Function.Call(Hash.REQUEST_STREAMED_TEXTURE_DICT, TextureDict, 0);
                while (!Function.Call<bool>(Hash.HAS_STREAMED_TEXTURE_DICT_LOADED, TextureDict))
                    Script.Yield();
                Game.FadeScreenIn(1000);

                var planet = World.CreateProp(_planetModel,
                    spawn + Vector3.RelativeFront * 5000 + Vector3.WorldDown * 100, false, false);

                if (Entity.Exists(planet))
                {
                    planet.FreezePosition = true;
                    _planetProp = planet;
                }

                var v = World.CreateVehicle("zanufo", spawn + Vector3.WorldDown * 100, 0);
                if (Entity.Exists(v))
                {
                    v.FreezePosition = true;
                    _ufo = v;
                    _pos = v.Position;
                }
            }

            public void Stop()
            {
                if (World.RenderingCamera == _camera)
                    World.RenderingCamera = null;

                _camera.Destroy();

                if (Entity.Exists(_ufo))
                    _ufo.Delete();

                if (Entity.Exists(_planetProp))
                    _planetProp.Delete();

                Function.Call(Hash.SET_STREAMED_TEXTURE_DICT_AS_NO_LONGER_NEEDED, TextureDict);
            }

            public void Update()
            {
                Function.Call(Hash.HIDE_HUD_AND_RADAR_THIS_FRAME);

                if (_ufo == null)
                {
                    Complete = true;
                    return;
                }

                switch (_step)
                {
                    case 0:
                        //TimeCycleModifier.Set("heliGunCam", 1.0f);
                        _camera.FarClip = 100000;
                        _camera.PointAt(_ufo);
                        _step++;
                        break;
                    case 1:
                        _pos += Vector3.RelativeFront * 100 * Game.LastFrameTime;
                        _ufo.PositionNoOffset = _pos;
                        _seconds += Game.LastFrameTime;
                        DrawUi();
                        if (_seconds < 7f) return;
                        Complete = true;
                        break;
                }
            }

            private void DrawUi()
            {
                const float imgWidth = 1536f;
                const float imgHeight = 1024f;
                const float width = 1f / 1920 / (1f / imgWidth);
                const float height = 1f / 1080 / (1f / imgHeight);

                Function.Call(Hash.DRAW_SPRITE, TextureDict, TextureName, 0.5f, 0.5f, width, height, 0f, 255, 255, 255,
                    255);

                /////////////////////////////////////////////////////////////
                Function.Call(Hash.SET_TEXT_FONT, (int) Font.Monospace);
                Function.Call(Hash.SET_TEXT_SCALE, 0.3f, 0.3f);
                Function.Call(Hash.SET_TEXT_COLOUR, 255, 255, 255, 255);
                Function.Call(Hash.SET_TEXT_DROPSHADOW, 1, 1, 1, 1, 1);
                Function.Call(Hash.SET_TEXT_EDGE, 1, 1, 1, 1, 205);
                Function.Call(Hash.SET_TEXT_JUSTIFICATION, 1);
                Function.Call(Hash.SET_TEXT_WRAP, 0, width);
                Function.Call(Hash._SET_TEXT_ENTRY, "CELL_EMAIL_BCON");
                Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, "UAC~n~VEHICLE IDENTIFICATION " + RandomString(5));
                Function.Call(Hash._DRAW_TEXT, 0.5f, 0.5f);
                /////////////////////////////////////////////////////////////

                /////////////////////////////////////////////////////////////
                Function.Call(Hash.SET_TEXT_FONT, (int) Font.ChaletComprimeCologne);
                Function.Call(Hash.SET_TEXT_SCALE, 0.3f, 0.3f);
                Function.Call(Hash.SET_TEXT_COLOUR, 255, 255, 255, 255);
                Function.Call(Hash.SET_TEXT_DROPSHADOW, 1, 1, 1, 1, 1);
                Function.Call(Hash.SET_TEXT_EDGE, 1, 1, 1, 1, 205);
                Function.Call(Hash.SET_TEXT_JUSTIFICATION, 1);
                Function.Call(Hash.SET_TEXT_WRAP, 0, width);
                Function.Call(Hash._SET_TEXT_ENTRY, "CELL_EMAIL_BCON");
                Function.Call(Hash._ADD_TEXT_COMPONENT_STRING,
                    "MARS SAT CAM 11~n~" + World.CurrentDate.ToShortDateString());
                Function.Call(Hash._DRAW_TEXT, 0.15f, 0.075f);
                /////////////////////////////////////////////////////////////

                if (Entity.Exists(_planetProp))
                {
                    Function.Call(Hash.SET_DRAW_ORIGIN, _planetProp.Position.X, _planetProp.Position.Y,
                        _planetProp.Position.Z, 0);

                    /////////////////////////////////////////////////////////////
                    Function.Call(Hash.SET_TEXT_FONT, (int) Font.ChaletComprimeCologne);
                    Function.Call(Hash.SET_TEXT_SCALE, 0.3f, 0.3f);
                    Function.Call(Hash.SET_TEXT_COLOUR, 255, 255, 255, 255);
                    Function.Call(Hash.SET_TEXT_DROPSHADOW, 1, 1, 1, 1, 1);
                    Function.Call(Hash.SET_TEXT_EDGE, 1, 1, 1, 1, 205);
                    Function.Call(Hash.SET_TEXT_JUSTIFICATION, 1);
                    Function.Call(Hash.SET_TEXT_WRAP, 0, width);
                    Function.Call(Hash._SET_TEXT_ENTRY, "CELL_EMAIL_BCON");
                    Function.Call(Hash._ADD_TEXT_COMPONENT_STRING,
                        $"ORBITAL ID: MARS~n~AVG TEMP: 125c~n~CUR ORBITAL VEL: 24.{_random.Next(0, 10)} km/s");
                    Function.Call(Hash._DRAW_TEXT, 0f, 0f);
                    /////////////////////////////////////////////////////////////

                    Function.Call(Hash.CLEAR_DRAW_ORIGIN);
                }
            }

            private string RandomString(int length)
            {
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                return new string(Enumerable.Repeat(chars, length)
                    .Select(s => s[_random.Next(s.Length)]).ToArray());
            }
        }

        #region Fields

        private readonly List<OnFootCombatPed> _aliens = new List<OnFootCombatPed>();
        private readonly List<Ped> _pilots = new List<Ped>();
        private readonly List<Vehicle> _vehicles = new List<Vehicle>();
        private Vehicle _carrierShip;
        private Vector3 _carrierSpawn;
        private Prop _laptop;
        private ICutScene _cutscene;
        private readonly Random _random = new Random();
        private Vector3 _laptopSpawnPosition = new Vector3(-10002.10f, -10004.52f, 10001.463f);
        private Vector3 _laptopSpawnRotation = new Vector3(0, 0, 0.2404f);

        #region Settings

        private int _missionStep;
        private int _enemyCount = 15;
        private int _pilotCount = 3;
        private float _aiWeaponDamage = 0.01f;
        private string _flagModel = "ind_prop_dlc_flag_02";
        private string _cutscenePlanetModel = "mars_large";
        private Vector3 _lastFlagPos = Vector3.Zero;

        #endregion

        #endregion

        #region Functions

        #region Implemented

        public override void OnAwake()
        {
            ReadSettings();

            ///////////////////////////////////////////////
            // NOTE: Doing this to create the ini if it doesn't exist.
            ///////////////////////////////////////////////
            SaveSettings();

            SpawnFlag();
        }

        public override void OnStart()
        {
            if (_missionStep == 0)
            {
                SpawnAliens();

                Function.Call(Hash.SET_AI_WEAPON_DAMAGE_MODIFIER, _aiWeaponDamage);
            }

            SpawnLaptop();

            Game.Player.Character.CanRagdoll = false;
        }

        public override void OnUpdate()
        {
            switch (_missionStep)
            {
                case 0:
                    ProcessAliens();
                    UpdateCarierShip();
                    if (!AreAllAliensDead()) return;
                    _missionStep++;
                    break;
                case 1:
                    if (Entity.Exists(_carrierShip))
                        _carrierShip.Delete();
                    Function.Call(Hash._PLAY_AMBIENT_SPEECH1, Game.Player.Character, "Generic_Insult_Med",
                        "Speech_Params_Force");
                    Function.Call(Hash.PLAY_MISSION_COMPLETE_AUDIO, "FRANKLIN_BIG_01");
                    _missionStep++;
                    break;
                case 2:
                    if (!Function.Call<bool>(Hash.IS_MISSION_COMPLETE_PLAYING))
                        return;
                    Effects.Start(ScreenEffect.SuccessNeutral, 5000);
                    ScaleFormMessages.Message.SHOW_MISSION_PASSED_MESSAGE(Game.GetGXTEntry("ENEMIES_ELIM"));
                    Script.Wait(4000);
                    _missionStep++;
                    break;
                case 3:
                    if (Game.Player.Character.IsInVehicle()) return;
                    GtsLibNet.DisplayHelpTextWithGxt("PLANT_FLAG");
                    if (!Game.IsControlJustPressed(2, Control.Context))
                        return;
                    var spawn = Game.Player.Character.Position + Game.Player.Character.ForwardVector * 2;
                    var ground = GtsLibNet.GetGroundHeightRay(spawn, Game.Player.Character);
                    if (ground != Vector3.Zero) spawn = ground;
                    Game.Player.Character.Task.PlayAnimation("pickup_object", "pickup_low");
                    _lastFlagPos = spawn;
                    SpawnFlag();
                    CheckMarsMission();
                    break;
                case 4:
                    Script.Wait(1000);
                    if (!Entity.Exists(_laptop))
                    {
                        SpawnLaptop();
                        return;
                    }
                    UI.ShowSubtitle(Game.GetGXTEntry("SAT_CHECK"), 7500);
                    _laptop.AddBlip().Scale = 0.6f;
                    _laptop.CurrentBlip.Color = BlipColor.Yellow;
                    _missionStep++;
                    break;
                case 5:
                    var distance = Vector3.DistanceSquared2D(Game.Player.Character.Position, _laptop.Position);
                    HelperFunctions.DrawWaypoint(CurrentScene, _laptop.Position);
                    if (distance > 3)
                        return;
                    GtsLibNet.DisplayHelpTextWithGxt("PRESS_E");
                    if (!Game.IsControlJustPressed(2, Control.Context))
                        return;
                    _cutscene = new MoonSatelliteCutscene(_cutscenePlanetModel);
                    _cutscene.Start();
                    _laptop.CurrentBlip.Remove();
                    _missionStep++;
                    break;
                case 6:
                    if (!_cutscene.Complete)
                    {
                        _cutscene.Update();
                        return;
                    }
                    _cutscene.Stop();
                    Effects.Start(ScreenEffect.FocusOut);
                    _missionStep++;
                    break;
                case 7:
                    Script.Wait(500);
                    Game.Player.Character.Task.PlayAnimation("gestures@f@standing@casual", "gesture_no_way");
                    Function.Call(Hash._PLAY_AMBIENT_SPEECH1, Game.Player.Character, "Generic_Shocked_High",
                        "Speech_Params_Force");
                    Script.Wait(1000);
                    UI.ShowSubtitle(Game.GetGXTEntry("GO_TO") + " ~p~Mars~s~.", 7500);
                    EndScenario(true);
                    break;
            }
        }

        public override void OnEnded(bool success)
        {
            Game.Player.Character.CanRagdoll = true;

            SaveSettings();

            if (!success)
            {
                DeleteAliens(true);

                return;
            }

            DeleteAliens(false);
        }

        public override void OnAborted()
        {
            DeleteAliens(true);
        }

        #endregion

        private void ReadSettings()
        {
            _missionStep = Settings.GetValue("general", "mission_step", _missionStep);
            _enemyCount = Settings.GetValue("general", "enemy_count", _enemyCount);
            _pilotCount = Settings.GetValue("general", "pilot_count", _pilotCount);
            _flagModel = Settings.GetValue("general", "flag_model", _flagModel);
            _aiWeaponDamage = Settings.GetValue("general", "ai_weapon_damage", _aiWeaponDamage);
            _lastFlagPos = ParseVector3.Read(Settings.GetValue("general", "last_flag_pos"), Vector3.Zero);
            _cutscenePlanetModel = Settings.GetValue("cutscene", "cutscene_planet_model", _cutscenePlanetModel);
            _laptopSpawnPosition = ParseVector3.Read(Settings.GetValue("cutscene", "laptop_spawn_position"),
                _laptopSpawnPosition);
            _laptopSpawnRotation = ParseVector3.Read(Settings.GetValue("cutscene", "laptop_spawn_rotation"),
                _laptopSpawnRotation);
        }

        private void SaveSettings()
        {
            Settings.SetValue("general", "mission_step", _missionStep);
            Settings.SetValue("general", "enemy_count", _enemyCount);
            Settings.SetValue("general", "pilot_count", _pilotCount);
            Settings.SetValue("general", "ai_weapon_damage", _aiWeaponDamage);
            Settings.SetValue("general", "last_flag_pos", _lastFlagPos);
            Settings.SetValue("general", "flag_model", _flagModel);
            Settings.SetValue("cutscene", "cutscene_planet_model", _cutscenePlanetModel);
            Settings.SetValue("cutscene", "laptop_spawn_position", _laptopSpawnPosition);
            Settings.SetValue("cutscene", "laptop_spawn_rotation", _laptopSpawnRotation);
            Settings.Save();
        }

        private void DeleteAliens(bool delete)
        {
            _cutscene?.Stop();

            Function.Call(Hash.RESET_AI_WEAPON_DAMAGE_MODIFIER);
            Function.Call(Hash.STOP_GAMEPLAY_HINT, 0);
            Function.Call(Hash.REMOVE_ANIM_DICT, "move_avoidance@generic_m");
            Function.Call(Hash.REMOVE_ANIM_DICT, "get_up@standard");
            Game.Player.Character.Task.ClearAll();
            Game.Player.CanControlCharacter = true;

            foreach (var alien in _aliens)
            {
                if (delete)
                {
                    alien.Delete();
                    continue;
                }

                alien.MarkAsNoLongerNeeded();
            }

            foreach (var pilot in _pilots)
            {
                if (delete)
                {
                    pilot.Delete();
                    continue;
                }

                pilot.MarkAsNoLongerNeeded();
            }

            foreach (var vehicle in _vehicles)
            {
                if (delete)
                {
                    vehicle.Delete();
                    continue;
                }

                vehicle.MarkAsNoLongerNeeded();
            }

            if (Entity.Exists(_laptop))
                if (delete)
                {
                    _laptop.Delete();
                }
                else
                {
                    _laptop.MarkAsNoLongerNeeded();
                    _laptop.CurrentBlip.Remove();
                }

            if (Entity.Exists(_carrierShip))
                _carrierShip.Delete();
        }

        private void SpawnAliens()
        {
            var pedSpawn = CurrentScene.Info.GalaxyCenter + Vector3.RelativeLeft * 150;

            if (!HelperFunctions.DidGoToMars())
            {
                _carrierShip = World.CreateVehicle("zanufo", pedSpawn + Vector3.WorldUp * 15);
                if (Entity.Exists(_carrierShip))
                {
                    _carrierSpawn = _carrierShip.Position;
                    _carrierShip.FreezePosition = true;
                    _carrierShip.IsInvincible = true;
                    _carrierShip.HasCollision = false;
                }
            }

            for (var i = 0; i < _enemyCount; i++)
            {
                var spawn = pedSpawn.Around(_random.Next(5, 15));
                var alien = GtsLibNet.CreateAlien(null, spawn, 90, WeaponHash.Railgun);
                if (!Entity.Exists(alien)) continue;
                alien.AddBlip().Scale = 0.5f;
                _aliens.Add(new OnFootCombatPed(alien) {Target = Game.Player.Character});
            }

            var vehicleSpawnArea = CurrentScene.Info.GalaxyCenter + Vector3.RelativeLeft * 250f;
            var maxZ = CurrentScene.Info.GalaxyCenter.Z;

            for (var i = 0; i < _pilotCount; i++)
            {
                var randomDistance = Function.Call<float>(Hash.GET_RANDOM_FLOAT_IN_RANGE, 25, 50);

                var spawnPoint = vehicleSpawnArea.Around(randomDistance);

                var vehicle = HelperFunctions.SpawnUfo(spawnPoint);

                if (Entity.Exists(vehicle))
                {
                    var pilot = vehicle.CreatePedOnSeat(VehicleSeat.Driver, PedHash.MovAlien01);

                    if (Entity.Exists(pilot))
                    {
                        pilot.SetDefaultClothes();
                        pilot.RelationshipGroup = Database.AlienRelationshipGroup;

                        Function.Call(Hash.TASK_PLANE_MISSION, pilot, vehicle, 0, Game.Player.Character, 0, 0, 0, 6, 0f,
                            0f, 0f, 0f, maxZ + 150f);
                        Function.Call(Hash._SET_PLANE_MIN_HEIGHT_ABOVE_TERRAIN, vehicle, maxZ + 150f);

                        pilot.AlwaysKeepTask = true;
                        pilot.SetCombatAttributes(CombatAttributes.AlwaysFight, true);
                        _pilots.Add(pilot);

                        vehicle.AddBlip().Scale = 0.8f;
                        vehicle.MarkAsNoLongerNeeded();
                        vehicle.Heading = Vector3.RelativeLeft.ToHeading();
                        _vehicles.Add(vehicle);

                        continue;
                    }

                    vehicle.Delete();
                }
            }
        }

        private void SpawnLaptop()
        {
            var prop = World.CreateProp("bkr_prop_clubhouse_laptop_01a", _laptopSpawnPosition, _laptopSpawnRotation,
                false, false);

            if (!Entity.Exists(prop))
                return;

            prop.FreezePosition = true;
            prop.IsInvincible = true;
            _laptop = prop;
        }

        private void SpawnFlag()
        {
            if (string.IsNullOrEmpty(_flagModel) && _lastFlagPos != Vector3.Zero)
                return;

            var flag = World.CreateProp(_flagModel, _lastFlagPos, false, false);

            if (!Entity.Exists(flag))
                return;

            flag.FreezePosition = true;

            ///////////////////////////
            // NOTE: Just in case the 
            // flag moved.
            ///////////////////////////
            flag.Position = _lastFlagPos;
        }

        private void ProcessAliens()
        {
            var playerPosition = Game.Player.Character.Position;

            foreach (var alien in _aliens)
                alien.Update();

            foreach (var vehicle in _vehicles)
            {
                if (vehicle.IsDead || Entity.Exists(vehicle.Driver) && vehicle.Driver.IsDead)
                {
                    if (Blip.Exists(vehicle.CurrentBlip))
                        vehicle.CurrentBlip.Remove();

                    continue;
                }

                if (vehicle.IsPersistent)
                {
                    var dist = Vector3.DistanceSquared2D(playerPosition, vehicle.Position);

                    const float maxDist = 1024 * 1024;

                    if (dist > maxDist)
                        vehicle.MarkAsNoLongerNeeded();
                }
            }
        }

        private void UpdateCarierShip()
        {
            if (Entity.Exists(_carrierShip))
            {
                if (_carrierSpawn.Z > CurrentScene.Info.GalaxyCenter.Z + 75)
                {
                    _carrierSpawn += Vector3.RelativeRight * Game.LastFrameTime * 500;

                    if (Function.Call<bool>(Hash.IS_GAMEPLAY_HINT_ACTIVE))
                    {
                        Function.Call(Hash.STOP_GAMEPLAY_HINT, 0);

                        if (!Game.Player.CanControlCharacter)
                        {
                            World.AddExplosion(_carrierSpawn, ExplosionType.Grenade, 1000f, 1.5f, true, true);
                            Game.Player.CanControlCharacter = true;
                            Game.Player.Character.Task.ClearAllImmediately();
                            var seq = new TaskSequence();
                            seq.AddTask.PlayAnimation("move_avoidance@generic_m", "react_front_dive_right", 11.0f,
                                -4.0f, -1, AnimationFlags.AllowRotation, 0.0f);
                            seq.AddTask.PlayAnimation("get_up@standard", "right", 8.0f, -4.0f, -1,
                                AnimationFlags.AllowRotation, 0.0f);
                            seq.Close(false);
                            Game.Player.Character.Task.PerformSequence(seq);
                            seq.Dispose();
                            Function.Call(Hash._PLAY_AMBIENT_SPEECH1, Game.Player.Character, "GENERIC_FRIGHTENED_HIGH",
                                "SPEECH_PARAMS_FORCE_SHOUTED_CRITICAL");
                        }
                    }
                }
                else
                {
                    _carrierSpawn += Vector3.WorldUp * Game.LastFrameTime * 10;

                    if (!Function.Call<bool>(Hash.IS_GAMEPLAY_HINT_ACTIVE))
                    {
                        Game.Player.Character.Task.StandStill(-1);
                        Game.Player.Character.Task.LookAt(_carrierShip);
                        Game.Player.Character.Heading = (_carrierShip.Position - Game.Player.Character.Position)
                            .ToHeading();
                        Game.Player.CanControlCharacter = false;
                        GameplayCamera.RelativeHeading = 0;
                        Function.Call(Hash._PLAY_AMBIENT_SPEECH1, Game.Player.Character, "Generic_Shocked_Med",
                            "Speech_Params_Force");
                        Function.Call(Hash.SET_GAMEPLAY_ENTITY_HINT, _carrierShip, 0, 0, 0, 1, 5000, 5000, 5000, 0);

                        Function.Call(Hash.REQUEST_ANIM_DICT, "move_avoidance@generic_m");
                        while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, "move_avoidance@generic_m"))
                            Script.Yield();
                        Function.Call(Hash.REQUEST_ANIM_DICT, "get_up@standard");
                        while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, "get_up@standard"))
                            Script.Yield();
                    }
                }

                _carrierShip.Position = _carrierSpawn;
                _carrierShip.EngineRunning = true;
                _carrierShip.LightsOn = true;
                _carrierShip.InteriorLightOn = true;
                _carrierShip.BrakeLightsOn = true;

                const int maxDist = 100000;
                var d = Vector3.DistanceSquared(Game.Player.Character.Position, _carrierSpawn);

                if (d > maxDist)
                    _carrierShip.Delete();
            }
        }

        private void CheckMarsMission()
        {
            if (HelperFunctions.DidGoToMars())
            {
                EndScenario(true);
                return;
            }
            _missionStep++;
        }

        private bool AreAllAliensDead()
        {
            return _aliens.TrueForAll(alien => alien.IsDead) && _vehicles.TrueForAll(vehicle => vehicle.IsDead);
        }

        #endregion
    }
}