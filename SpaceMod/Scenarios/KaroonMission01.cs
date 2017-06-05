using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using SpaceMod;
using SpaceMod.Lib;
using SpaceMod.Scenario;

namespace DefaultMissions
{
    public class KaroonMission01 : CustomScenario
    {
        private readonly string _ufoModelName = "zanufo";
        private Model _ufoModel;

        public KaroonMission01()
        {
            _ufoModelName = Settings.GetValue("settings", "ufo_model", _ufoModelName);
            Settings.SetValue("settings", "ufo_model", _ufoModelName);
            Settings.Save();

            Ufos = new List<Vehicle>();
            PlayerVehicle = Game.Player.Character.CurrentVehicle;

            if (PlayerVehicle != null)
            {
                OriginalVehicleHealth = PlayerVehicle.MaxHealth;
                PlayerVehicle.IsExplosionProof = true;
                PlayerVehicle.Health = 10000;
            }
        }

        public Vehicle PlayerVehicle { get; }
        public List<Vehicle> Ufos { get; }
        public bool DidLoad { get; private set; }
        public int OriginalVehicleHealth { get; }

        public override void Start()
        {
            _ufoModel = new Model(_ufoModelName);
            _ufoModel.Request();
            DateTime timout = DateTime.UtcNow + new TimeSpan(0, 0, 0, 5);
            while (!_ufoModel.IsLoaded)
            {
                Script.Yield();
                if (DateTime.UtcNow > timout)
                    break;
            }
        }

        private void CreateSpaceShips()
        {
            Vector3 spawn = SpaceModDatabase.GalaxyCenter + new Vector3(-1500, 0, 0);
            Ped playerPed = Game.Player.Character;

            if (!_ufoModel.IsLoaded)
            {
                UI.Notify($"{_ufoModelName} model failed to load! Make sure you have a valid model in the .ini file.");
                EndScenario(false);
                return;
            }

            for (int i = 0; i < 4; i++)
            {
                Vector3 spawnAround = spawn.Around(400);
                Vehicle vehicle = World.CreateVehicle(_ufoModel, spawnAround);
                Ufos.ForEach(ufo => vehicle.SetNoCollision(ufo, true));
                Ped ped = vehicle.CreatePedOnSeat(VehicleSeat.Driver, PedHash.MovAlien01);
                ped.SetDefaultClothes();
                ped.RelationshipGroup = SpaceModDatabase.AlienRelationship;
                vehicle.Heading = (playerPed.Position - vehicle.Position).ToHeading();
                vehicle.MaxSpeed = 50;
                vehicle.IsOnlyDamagedByPlayer = true;
                Blip blip = vehicle.AddBlip();
                blip.Name = "UFO";
                blip.Color = BlipColor.Green;
                ped.Task.FightAgainst(playerPed);
                ped.AlwaysKeepTask = true;
                Ufos.Add(vehicle);
            }
        }

        public override void OnUpdate()
        {
            if (Game.IsLoading) return;

            if (!DidLoad)
            {
                SpaceModLib.ShowSubtitleWithGXT("GTS_LABEL_22", 7500);
                CreateSpaceShips();
                DidLoad = true;
                return;
            }

            Ufos?.ForEach(vehicle =>
            {
                // TODO: Figure out a variable for when the vehicle is not dead but is spiraling downward.
                if (vehicle.IsDead || !vehicle.IsDriveable || vehicle.IsOnFire || vehicle.Driver.IsDead && vehicle.CurrentBlip.Exists())
                {
                    vehicle.CurrentBlip.Remove();
                    vehicle.Driver?.Kill();
                    vehicle.Explode();

                    if (Ufos.TrueForAll(x => !x.CurrentBlip.Exists()))
                    {
                        ScaleFormMessages.Message.SHOW_MISSION_PASSED_MESSAGE(Game.GetGXTEntry("BM_LABEL_3"));
                        EndScenario(true);
                    }
                    return;
                }

                if (vehicle.Driver == null)
                    return;

                Vector3 lastDamagePos = vehicle.Driver.GetLastWeaponImpactCoords();

                if (lastDamagePos == Vector3.Zero)
                    return;

                if (PlayerVehicle == null)
                    return;

                float distance;
                if ((distance = lastDamagePos.DistanceTo(PlayerVehicle.Position)) >= 25)
                    return;

                if (distance == 0)
                    distance = 1;

                PlayerVehicle.Health -= 50 / (int)distance;
                PlayerVehicle.EngineHealth -= 120 / distance;
            });
        }

        public override void OnEnded(bool success)
        {
            if (success)
            {
                MarkVehiclesAsNoLongerNeeded();
            }
            else
            {
                DeleteAllVehicles();
            }

            Reset();
        }

        public override void OnAborted()
        {
            DeleteAllVehicles();
            Reset();
        }

        private void MarkVehiclesAsNoLongerNeeded()
        {
            while (Ufos.Count > 0)
            {
                Vehicle vehicle = Ufos[0];
                vehicle.CurrentBlip?.Remove();
                vehicle.MarkAsNoLongerNeeded();
                Ufos.RemoveAt(0);
            }
        }

        private void DeleteAllVehicles()
        {
            while (Ufos.Count > 0)
            {
                Vehicle vehicle = Ufos[0];
                vehicle.Delete();
                Ufos.RemoveAt(0);
            }
        }

        private void Reset()
        {
            if (PlayerVehicle != null)
            {
                PlayerVehicle.MaxHealth = OriginalVehicleHealth;
                PlayerVehicle.Health = PlayerVehicle.Health;
                PlayerVehicle.IsExplosionProof = false;
            }
        }
    }
}
