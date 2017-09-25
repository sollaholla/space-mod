using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using GTS.Library;
using GTS.Scenes;

namespace DefaultMissions
{
    // ReSharper disable CompareOfFloatsByEqualityOperator
    public class Moon : Scenario
    {
        private readonly List<Ped> _hostiles;
        private readonly List<Vehicle> _ufos;

        private bool _combatInitialized;

        public Moon()
        {
            _hostiles = new List<Ped>();
            _ufos = new List<Vehicle>();
        }

        public override bool BlockOrbitLanding => false;

        public void Start()
        {
            var spawnRegion = new Vector3(-9946.63f, -10148.71f, 1000.36f);
            var random = new System.Random();
            for (var i = 0; i < 15; i++)
            {
                var randDist = Function.Call<float>(Hash.GET_RANDOM_FLOAT_IN_RANGE, 20f, 100f);
                var spawnPoint = spawnRegion.Around(randDist);
                var ground = World.GetGroundHeight(spawnPoint + Vector3.WorldUp);
                if (ground == 0) continue;
                var ped = GtsLibNet.CreateAlien(null, spawnPoint, random.Next(135, 220));
                ped.Position = new Vector3(ped.Position.X, ped.Position.Y, ground);
                ped.Weapons.Give((WeaponHash)Game.GenerateHash("weapon_pulserifle"), 15, true, true);
                ped.AddBlip();
                ped.IsOnlyDamagedByPlayer = true;
                _hostiles.Add(ped);
                Script.Yield();
            }

            var ufoModel = new Model("zanufo");
            ufoModel.Request();
            while (!ufoModel.IsLoaded)
                Script.Yield();
            for (var i = 0; i < 4; i++)
            {
                var randDist = Function.Call<float>(Hash.GET_RANDOM_FLOAT_IN_RANGE, 40f, 145f);
                var spawnPoint = spawnRegion.Around(randDist);
                var vehicle = World.CreateVehicle(ufoModel, spawnPoint);
                vehicle.PlaceOnGround();
                vehicle.IsOnlyDamagedByPlayer = true;
                var pedModel = (Model)GtsLibNet.GetAlienModel();
                pedModel.Request();
                while (!pedModel.IsLoaded)
                    Script.Yield();
                var ped = vehicle.CreatePedOnSeat(VehicleSeat.Driver, pedModel);
                var b = ped.AddBlip();
                b.Sprite = (BlipSprite)422;
                b.Name = "UFO";
                ped.IsOnlyDamagedByPlayer = true;
                Function.Call(Hash.SET_CURRENT_PED_VEHICLE_WEAPON, ped, Game.GenerateHash("VEHICLE_WEAPON_PLAYER_LAZER"));
                GtsLibNet.GivePedAlienAttributes(ped);
                pedModel.MarkAsNoLongerNeeded();
                _hostiles.Add(ped);
                _ufos.Add(vehicle);
                Script.Yield();
            }
            ufoModel.MarkAsNoLongerNeeded();
        }

        public void Update()
        {
            if (!_combatInitialized)
            {
                var lastShotCoord = PlayerPed.GetLastWeaponImpactCoords();
                var found = _hostiles.Any(x => x.IsInCombatAgainst(PlayerPed));
                foreach (var hostile in _hostiles)
                {
                    var hPos = hostile.Position;
                    var dist = Function.Call<float>(Hash.VDIST2, hPos.X, hPos.Y, hPos.Z, 
                        lastShotCoord.X,
                        lastShotCoord.Y,
                        lastShotCoord.Z);
                    const float maxDist = 125 * 125;
                    if (dist > maxDist) continue;
                    found = true;
                }
                if (!found) return;
                foreach (var hostile in _hostiles)
                    if (!hostile.IsInCombat)
                    {
                        if (hostile.IsInVehicle())
                            hostile.Task.FightAgainst(PlayerPed);
                        else hostile.Task.ShootAt(PlayerPed); 
                    }

                _combatInitialized = true;
            }
            else
            {
                foreach (var hostile in _hostiles)
                {
                    if (!Blip.Exists(hostile.CurrentBlip)) continue;
                    if (!hostile.IsDead) continue;
                    hostile.CurrentBlip.Remove();
                }

                if (_hostiles.All(x => x.IsDead))
                    EndScenario(true);
            }
        }

        public void OnDisable(bool success)
        {
            foreach (var ent in _hostiles)
            {
                ent?.MarkAsNoLongerNeeded();
                ent?.CurrentBlip?.Remove();
            }

            foreach (var veh in _ufos)
            {
                veh?.MarkAsNoLongerNeeded();
                veh?.CurrentBlip?.Remove();
            }
        }

        public void OnAborted()
        {
            foreach (var ent in _hostiles)
                ent?.Delete();

            foreach (var veh in _ufos)
                veh?.Delete();
        }
    }
}