using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using GTS.Extensions;
using GTS.Library;
using GTS.Particles;
using GTS.Scenarios;

// mp_sleep => sleep_loop

namespace DefaultMissions
{
    public class Europa : Scenario
    {
        public Europa()
        {
            _entities = new List<Entity>();
        }

        private class EuropaMissileCutScene : ICutScene
        {
            private Vector3 _firePos;
            private Vector3 _spawn;
            private int _step;

            public EuropaMissileCutScene(Vector3 spawn, Vector3 firePos)
            {
                _spawn = spawn;
                _firePos = firePos;
                Entities = new List<Entity>();
            }

            public List<Entity> Entities { get; }

            public bool Complete { get; set; }

            public void Start()
            {
                var ufo = HelperFunctions.SpawnUfo(_spawn, 0);
                var alien = Utils.CreateAlien(PedHash.MovAlien01, ufo.Position, 0, WeaponHash.Unarmed);
                alien.SetIntoVehicle(ufo, VehicleSeat.Driver);
                alien.Task.Wait(-1);
                ufo.Heading = (Game.Player.Character.Position - ufo.Position).ToHeading();
                Entities.AddRange(new Entity[] {ufo.Driver, ufo});
                PlaySmoke(ufo.Position - Vector3.WorldUp, 70f);
                ufo.AddBlip().Scale = 0.8f;
            }

            public void Update()
            {
                switch (_step)
                {
                    case 0:
                        var end = _firePos - Vector3.WorldUp;
                        var start = _spawn + Vector3.WorldUp * 5;
                        // _SHOOT_SINGLE_VEHICLE_BULLET_BETWEEN_COORDS
                        Function.Call(Hash._0xE3A7742E0B7A2F8B, start.X, start.Y, start.Z, end.X, end.Y, end.Z, 200,
                            true,
                            Game.GenerateHash("VEHICLE_WEAPON_SPACE_ROCKET"), Entities[0], true, false, 125f,
                            Entities[1], true, true, false, false);
                        Game.Player.CanControlCharacter = false;
                        Function.Call(Hash.SET_GAMEPLAY_ENTITY_HINT, Entities[1], 0.0, 0.0, 0.0, 1, 2000, 2000, 2000,
                            0);
                        Script.Wait(2000);
                        Game.Player.CanControlCharacter = true;
                        Game.TimeScale = 0.4f;
                        _step++;
                        break;
                    case 1:
                        Game.DisableControlThisFrame(2, Control.Aim);
                        if (Function.Call<bool>(Hash.IS_EXPLOSION_IN_SPHERE, (int) ExplosionType.PlaneRocket,
                            _firePos.X, _firePos.Y, _firePos.Z, 500f))
                        {
                            Script.Wait(1500);
                            Game.TimeScale = 1;
                            _step++;
                        }
                        break;
                    case 2:
                        var maxZ = _spawn.Z + 500f;
                        Function.Call(Hash.TASK_PLANE_CHASE, Game.Player.Character, 0, 0, 0);
                        Function.Call(Hash._SET_PLANE_MIN_HEIGHT_ABOVE_TERRAIN, Entities[1], maxZ);

                        var spawnPoint = _spawn + Vector3.RelativeRight * 70;
                        for (var i = 0; i < 15; i++)
                        {
                            var ped = Utils.CreateAlien(PedHash.MovAlien01, spawnPoint, 0, WeaponHash.CombatPDW);
                            if (!Entity.Exists(ped)) continue;
                            PlaySmoke(ped.Position - Vector3.WorldUp, 1.0f);
                            ped.AddBlip().Scale = 0.5f;
                            Entities.Add(ped);
                        }
                        _step++;
                        break;
                    case 3:
                        Complete = true;
                        break;
                }
            }

            public void Stop()
            {
                Game.TimeScale = 1;
                Game.Player.CanControlCharacter = true;
            }

            private void PlaySmoke(Vector3 pos, float scale)
            {
                var ptfx = new PtfxNonLooped("scr_alien_teleport", "scr_rcbarry1");

                ptfx.Request();

                while (!ptfx.IsLoaded)
                    Script.Yield();

                ptfx.Play(pos, Vector3.Zero, scale);
                ptfx.Remove();
            }
        }

        #region Fields

        #region Readonly

        private readonly Model[] _models = { };
        private readonly List<Entity> _entities;

        #endregion

        #region Scenario

        private int _missionStep;
        private Fire _fire;
        private Prop _extinguisher;
        private EuropaMissileCutScene _europaMissileCutScene;

        #endregion

        #region Settings

        private Vector3 _explosionCoord = new Vector3(-9878.711f, -10012.36f, 9998.317f);
        private Vector3 _fireCoord = new Vector3(-9879.087f, -10014.16f, 10001.34f);
        private Vector3 _extinguisherPickupCoord = new Vector3(-9881.714f, -10016.63f, 9998.117f);
        private Vector3 _alienSpawn = new Vector3(-9979.049f, -10014.52f, 9998.44f);
        private float _aiWeaponDamage = 0.04f;

        #endregion

        #endregion

        #region Properties

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
            RequestModels();
            SpawnAliens();
        }

        public override void OnUpdate()
        {
            // NOTE: Make sure we didn't already go to mars, 
            // so we don't mess up the story sequence.
            if (!HelperFunctions.DidGoToMars())
            {
                EndScenario(false);
                return;
            }

            switch (_missionStep)
            {
                case 0:
                    if (Game.IsLoading || !Game.IsScreenFadedIn)
                        return;
                    Intro_StartExplosion();
                    _missionStep++;
                    break;
                case 1:
                    Intro_CollectExtinguisher();
                    break;
                case 2:
                    HelperFunctions.DrawWaypoint(CurrentScene, _fireCoord);
                    if (_fire.IsFireNear())
                        return;
                    _missionStep++;
                    break;
                case 3:
                    CutScene_DoCutScene();
                    break;
                case 4:
                    ProcessEntities();
                    break;
            }
        }

        private void ProcessEntities()
        {
            foreach (var entity in _entities)
                if (Entity.Exists(entity))
                    if (entity.IsDead)
                        if (Blip.Exists(entity.CurrentBlip))
                            entity.CurrentBlip.Remove();
        }

        public override void OnEnded(bool success)
        {
            CleanUp(!success);
        }

        public override void OnAborted()
        {
            CleanUp(true);
        }

        #endregion

        #region Scenario

        private void ReadSettings()
        {
            _missionStep = Settings.GetValue("general", "mission_step", _missionStep);
            _explosionCoord = ParseVector3.Read(Settings.GetValue("general", "explosion_coord"), _explosionCoord);
            _fireCoord = ParseVector3.Read(Settings.GetValue("general", "fire_coord"), _fireCoord);
            _extinguisherPickupCoord = ParseVector3.Read(Settings.GetValue("general", "extinguisher_pickup_coord"),
                _extinguisherPickupCoord);
            _alienSpawn = ParseVector3.Read(Settings.GetValue("general", "alien_spawn"), _alienSpawn);
            _aiWeaponDamage = Settings.GetValue("general", "ai_weapon_damage", _aiWeaponDamage);
        }

        private void SaveSettings()
        {
            Settings.SetValue("general", "mission_step", _missionStep);
            Settings.SetValue("general", "explosion_coord", _explosionCoord);
            Settings.SetValue("general", "fire_coord", _fireCoord);
            Settings.SetValue("general", "extinguisher_pickup_coord", _extinguisherPickupCoord);
            Settings.SetValue("general", "alien_spawn", _alienSpawn);
            Settings.SetValue("general", "ai_weapon_damage", _aiWeaponDamage);
            Settings.Save();
        }

        private void Intro_CollectExtinguisher()
        {
            if (Game.Player.Character.Weapons.HasWeapon(WeaponHash.FireExtinguisher))
            {
                Intro_SetExtinguisherAmmo();
                _missionStep++;
                return;
            }

            if (_extinguisher == null)
            {
                // Request the model first, since sometimes the props fail to load.
                var extinguisherModel =
                    new Model(Function.Call<int>(Hash.GET_WEAPONTYPE_MODEL, (int) WeaponHash.FireExtinguisher));

                if (!extinguisherModel.IsLoaded)
                {
                    var requestTimeout = DateTime.UtcNow + new TimeSpan(0, 0, 0, 5);
                    extinguisherModel.Request();
                    while (requestTimeout > DateTime.UtcNow && !extinguisherModel.IsLoaded)
                        Script.Yield();

                    // If the model fails to load then 
                    // we will just give the player the extinguisher anyway.
                    if (!extinguisherModel.IsLoaded)
                    {
                        Intro_GivePlayerExtinguisher();
                        _missionStep++;
                    }
                }

                _extinguisher = World.CreateAmbientPickup(PickupType.Health, _extinguisherPickupCoord,
                    extinguisherModel, 1);
                Function.Call(Hash.PLACE_OBJECT_ON_GROUND_PROPERLY, _extinguisher);
                _extinguisher.IsPersistent = true;
                _extinguisher.FreezePosition = true;
                extinguisherModel.MarkAsNoLongerNeeded();
                return;
            }

            var t = new Trigger(_extinguisher.Position, 1.5f);
            if (t.IsInTrigger(Game.Player.Character.Position))
                _extinguisher.Delete();

            if (!Entity.Exists(_extinguisher))
            {
                Intro_GivePlayerExtinguisher();
                return;
            }

            HelperFunctions.DrawWaypoint(CurrentScene, _extinguisher.Position);
        }

        private void Intro_StartExplosion()
        {
            Script.Wait(750);
            World.AddExplosion(_explosionCoord, ExplosionType.Tanker, 150, 5.0f);
            Function.Call(Hash._PLAY_AMBIENT_SPEECH1, Game.Player.Character, "Generic_Frightened_Med",
                "Speech_Params_Force_Shouted_Critical");
            _fire = new Fire(_fireCoord - Vector3.WorldUp, false);
            _fire.Start();
            Script.Wait(1000);
            Utils.ShowSubtitleWithGxt("EXT_FIRE");
        }

        private static void Intro_GivePlayerExtinguisher()
        {
            var w = Game.Player.Character.Weapons.Give(WeaponHash.FireExtinguisher, 9999, true, true);
            Intro_SetExtinguisherAmmo();
        }

        private static void Intro_SetExtinguisherAmmo()
        {
            var w = Game.Player.Character.Weapons[WeaponHash.FireExtinguisher];
            Game.Player.Character.Weapons.Select(w);
            w.Ammo = 9999;
            w.InfiniteAmmo = true;
        }

        private void CutScene_DoCutScene()
        {
            if (_europaMissileCutScene == null)
            {
                _europaMissileCutScene = new EuropaMissileCutScene(_alienSpawn, _fireCoord);
                _europaMissileCutScene.Start();
            }
            _europaMissileCutScene.Update();
            if (!_europaMissileCutScene.Complete)
                return;
            _europaMissileCutScene.Stop();
            _entities.AddRange(_europaMissileCutScene.Entities);
            _missionStep++;
        }

        #endregion

        #region Helpers

        private void SpawnAliens()
        {
            Function.Call(Hash.SET_AI_WEAPON_DAMAGE_MODIFIER, _aiWeaponDamage);
        }

        private void RequestModels()
        {
            foreach (var model in _models)
            {
                model.Request();
                while (!model.IsLoaded)
                    Script.Yield();
            }
        }

        private void RemoveModels()
        {
            foreach (var model in _models)
            {
                if (!model.IsLoaded)
                    continue;

                model.MarkAsNoLongerNeeded();
            }
        }

        private void CleanUp(bool delete)
        {
            RemoveModels();
            CleanUpEntities(delete);
            _europaMissileCutScene?.Stop();
            Function.Call(Hash.RESET_AI_WEAPON_DAMAGE_MODIFIER);
        }

        private void CleanUpEntities(bool delete)
        {
            if (_fire != null)
                if (delete)
                    _fire.Remove();

            if (Entity.Exists(_extinguisher))
                if (delete)
                    _extinguisher.Delete();

            foreach (var entity in _entities)
                if (Entity.Exists(entity))
                {
                    if (entity is Vehicle)
                    {
                        var v = entity as Vehicle;
                        v.Driver?.Delete();
                    }

                    if (delete)
                    {
                        entity.Delete();
                        continue;
                    }

                    entity.MarkAsNoLongerNeeded();
                }
        }

        #endregion

        #endregion
    }
}