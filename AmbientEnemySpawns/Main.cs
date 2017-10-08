using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Math;
using GTA.Native;
using GTS.Library;
using GTS.Scenes;

namespace AmbientEnemySpawns
{
    public class Main : Scenario
    {
        private List<Alien> _alienPeds;
        private List<Vehicle> _alienVehicles;

        private bool _onSurface;

        private bool _isInFightWithAliens = false;

        private DateTime _timeout;
        private bool startedTimeout;

        public Main()
        {
            _alienPeds = new List<Alien>();
            _alienVehicles = new List<Vehicle>();
        }

        public void Update()
        {
            UI.ShowSubtitle(_isInFightWithAliens.ToString());

            _alienPeds.ForEach(alien =>
            {
                if (Game.IsLoading)
                    alien.FreezePosition = true;
                else
                    alien.FreezePosition = false;
            });

            HandleShooting();
            UpdateTimer();
        }

        private void HandleShooting()
        {
            //if (!_isCombatInProgress)
            //{
            //    var lastShotCoord = PlayerPed.GetLastWeaponImpactCoords();
            //    var found = _alienPeds.Any(x => x.Ped.IsInCombatAgainst(PlayerPed));
            //    foreach (var hostile in _alienPeds)
            //    {
            //        var hPos = hostile.Position;
            //        var dist = Function.Call<float>(Hash.VDIST2, hPos.X, hPos.Y, hPos.Z,
            //            lastShotCoord.X,
            //            lastShotCoord.Y,
            //            lastShotCoord.Z);
            //        const float maxDist = 125 * 125;
            //        if (dist > maxDist) continue;
            //        found = true;
            //    }
            //    if (!found) return;
            //    foreach (var hostile in _alienPeds)
            //        if (!hostile.Ped.IsInCombat)
            //            if (hostile.Ped.IsInVehicle())
            //                hostile.Ped.Task.FightAgainst(PlayerPed);
            //            else hostile.Ped.Task.ShootAt(PlayerPed);

            //    _isCombatInProgress = true;
            //}
            //else
            //{
            //    foreach (var hostile in _alienPeds)
            //    {
            //        if (!Blip.Exists(hostile.CurrentBlip)) continue;
            //        if (!hostile.IsDead) continue;
            //        hostile.CurrentBlip.Remove();
            //    }

            //    if (_alienPeds.All(x => x.IsDead))
            //    {

            //        _isCombatInProgress = false;
            //    }
            //}

            _alienPeds.ForEach(alien => {
                alien.Update();
                if (Blip.Exists(alien.CurrentBlip) && alien.IsDead)
                    alien.CurrentBlip.Remove();
            });

            if(_alienPeds.TrueForAll(x => x.IsDead))
            {
                _isInFightWithAliens = false;
            }
        }
        
        private void UpdateTimer()
        {
            _onSurface = CurrentScene.Surfaces.Count > 0;
            if (!_isInFightWithAliens)
            {
                if (!startedTimeout)
                {
                    _timeout = DateTime.UtcNow + new TimeSpan(0, 0, 20); //gonna change later.
                    startedTimeout = true;
                }

                UI.ShowSubtitle(_timeout.ToLongTimeString());

                if (DateTime.UtcNow < _timeout) return;

                if (_onSurface)
                    SpawnEnemiesOnSurface();
                else
                    SpawnEnemiesInSpace();

                _isInFightWithAliens = true;
                startedTimeout = false;
            }
        }

        private Alien SpawnAlienPed(Vector3 spawnPos, float ground, Random rand)
        {
            var ped = GtsLibNet.CreateAlien(null, spawnPos, rand.Next(20, 180));
            var alien = new Alien(ped.Handle, 25*25);

            alien.Position = new Vector3(ped.Position.X, ped.Position.Y, ground);
            alien.AddBlip();
            alien.IsVisible = false;

            PtfxNonLooped ptfx = new PtfxNonLooped("scr_alien_teleport", "scr_rcbarry1");
            ptfx.Request();
            while (!ptfx.IsLoaded)
            {
                Script.Yield();
            }

            ptfx.Play(ped.Position, ped.Rotation, 1f);

            alien.IsVisible = true;

            return alien;
        }

        private void SpawnEnemiesOnSurface()
        {
            var spawnRegion = PlayerPed.Position.Around(25f);
            var random = new Random();
            var totalAliens = random.Next(8, 13);

            for(int i = 0; i < totalAliens; i++)
            {
                var randDist = Function.Call<float>(Hash.GET_RANDOM_FLOAT_IN_RANGE, 20f, 100f);
                var spawnPoint = spawnRegion.Around(randDist);
                var ground = World.GetGroundHeight(spawnPoint + Vector3.WorldUp);
                if (ground == 0) continue;

                _alienPeds.Add(SpawnAlienPed(spawnPoint, ground, random));
                Script.Yield();
            }
        }

        private void SpawnEnemiesInSpace()
        {

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
