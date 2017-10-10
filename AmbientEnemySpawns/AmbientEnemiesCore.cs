using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using GTS.Library;
using GTS.Scenes;
using GTS.Utility;

namespace AmbientEnemySpawns
{
    public class AmbientEnemiesCore : Scenario
    {
        private readonly List<Alien> _alienPeds;
        private readonly List<Vehicle> _alienVehicles;
        private readonly Random _randomTimer = new Random();

        private bool _isInFightWithAliens;

        private bool _onSurface;
        private bool _startedTimeout;
        private bool _successfullyKilledAliens;

        private DateTime _timeout;

        public AmbientEnemiesCore()
        {
            _alienPeds = new List<Alien>();
            _alienVehicles = new List<Vehicle>();
        }

        public override string[] TargetScenes => new[] {"MercurySurface.space"};

        public void Update()
        {
            try
            {
                HandleShooting();
                UpdateTimer();
            }
            catch (Exception e)
            {
                Debug.Log(e.Message + Environment.NewLine + e.StackTrace);
                throw;
            }
        }

        private void HandleShooting()
        {
            if (!_isInFightWithAliens)
                return;

            _alienPeds.ForEach(alien =>
            {
                alien.Update();

                if (alien.IsDead)
                {
                    if (Blip.Exists(alien.CurrentBlip))
                        alien.CurrentBlip.Remove();

                    if (alien.IsPersistent)
                        alien.MarkAsNoLongerNeeded();
                }

                const float maxDist = 250 * 250;

                if (alien.DistToEnemy > maxDist)
                    alien.Delete();
            });

            if (!_alienPeds.TrueForAll(x => x.IsDead))
                return;

            _isInFightWithAliens = false;

            if (_alienPeds.All(x => ((Ped) x).GetKiller() == PlayerPed))
                _successfullyKilledAliens = true;
        }

        private void UpdateTimer()
        {
            _onSurface = CurrentScene.Surfaces.Count > 0;

            if (_isInFightWithAliens) return;

            if (!_startedTimeout)
            {
                _timeout = DateTime.Now + new TimeSpan(0, 0,
                               _successfullyKilledAliens ? _randomTimer.Next(300, 600) : _randomTimer.Next(60, 120));

                _startedTimeout = true;
            }

            if (DateTime.Now < _timeout) return;

            if (_onSurface)
                SpawnEnemiesOnSurface();
            else
                SpawnEnemiesInSpace();

            _isInFightWithAliens = true;
            _startedTimeout = false;
        }

        private static Alien SpawnAlienPed(Vector3 spawnPos, Random rand)
        {
            var ped = GtsLibNet.CreateAlien(null, spawnPos, rand.Next(20, 180));
            ped.Weapons.Give((WeaponHash) Game.GenerateHash("weapon_pulserifle"), 15, true, true);
            ped.Accuracy = rand.Next(1, 5);
            ped.Money = 0;

            var alien = new Alien(ped.Handle, 25)
            {
                Enemy = Game.Player.Character,
                Position = spawnPos
            };
            alien.AddBlip();
            alien.IsVisible = false;

            var ptfx = new PtfxNonLooped("scr_alien_teleport", "scr_rcbarry1");
            ptfx.Request();
            while (!ptfx.IsLoaded)
                Script.Yield();

            ptfx.Play(ped.Position, ped.Rotation, 2f);
            ptfx.Remove();

            alien.IsVisible = true;

            return alien;
        }

        private void SpawnEnemiesOnSurface()
        {
            var spawnRegion = PlayerPed.Position.Around(25f);
            var random = new Random();
            var totalAliens = random.Next(8, 13);

            for (var i = 0; i < totalAliens; i++)
            {
                var randDist = Function.Call<float>(Hash.GET_RANDOM_FLOAT_IN_RANGE, 20f, 100f);
                var spawnPoint = spawnRegion.Around(randDist);
                var ground = GtsLibNet.GetGroundHeightRay(spawnPoint);
                if (ground == Vector3.Zero) continue;
                _alienPeds.Add(SpawnAlienPed(ground, random));
                Script.Yield();
            }
        }

        private void SpawnEnemiesInSpace()
        {
        }

        public void OnDisable(bool failed)
        {
            OnAborted();
        }

        public void OnAborted()
        {
            foreach (var ent in _alienPeds)
                ent?.Delete();

            foreach (var veh in _alienVehicles)
                veh?.Delete();
        }
    }
}