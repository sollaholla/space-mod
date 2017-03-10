using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;
using SpaceMod;
using SpaceMod.DataClasses;

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

            UFOS = new List<Vehicle>();
            PlayerVehicle = Game.Player.Character.CurrentVehicle;

            if (PlayerVehicle != null)
            {
                OriginalVehicleHealth = PlayerVehicle.MaxHealth;
                PlayerVehicle.IsExplosionProof = true;
                PlayerVehicle.Health = 10000;
            }
        }

        public Vehicle PlayerVehicle { get; }

        public bool DidLoad { get; private set; }

        public List<Vehicle> UFOS { get; }

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
            Vector3 spawn = Database.GalaxyCenter + new Vector3(-1500, 0, 0);
            Ped playerPed = Game.Player.Character;
            
            if (!_ufoModel.IsLoaded)
            {
                UI.Notify($"{_ufoModelName} model failed to load! Make sure you have a valid model in the .ini file.");
                EndScenario(false);
                return;
            }

            for (int i = 0; i < 4; i++)
            {
                Vector3 spawnAround = spawn.Around(100);
                Vehicle vehicle = World.CreateVehicle(_ufoModel, spawnAround);
                Ped ped = vehicle.CreatePedOnSeat(VehicleSeat.Driver, PedHash.MovAlien01);
                ped.SetDefaultClothes();
                vehicle.Heading = (playerPed.Position - vehicle.Position).ToHeading();
                vehicle.MaxSpeed = 50;
                Blip blip = vehicle.AddBlip();
                blip.Name = "UFO";
                blip.Color = BlipColor.Green;
                ped.Task.FightAgainst(playerPed);
                UFOS.Add(vehicle);
            }
        }

        public override void OnUpdate()
        {
            if (Game.IsLoading) return;

            if (!DidLoad)
            {
                UI.ShowSubtitle("An ~g~Alien~s~ convoy approaches!", 7500);
                CreateSpaceShips();
                Utilities.DisplayHelpTextThisFrame(
                    "~g~Alien~s~: Turn back now human, or you will be shot down.\n\n~b~You~s~: Not on your life scum-bag.");
                DidLoad = true;
            }
            else
            {
                UFOS?.ForEach(vehicle =>
                {
                    // TODO: Figure out a variable for when the vehicle is not dead but is spiraling downward.
                    if (vehicle.IsDead || !vehicle.IsDriveable || vehicle.IsOnFire || vehicle.Driver.IsDead && vehicle.CurrentBlip.Exists())
                    {
                        vehicle.CurrentBlip.Remove();
                        vehicle.Driver?.Kill();

                        if (UFOS.TrueForAll(x => !x.CurrentBlip.Exists()))
                        {
                            BigMessageThread.MessageInstance.ShowMissionPassedMessage("~r~enemies eliminated");
                            EndScenario(true);
                        }
                    }

                    if (vehicle.Driver != null)
                    {
                        Vector3 lastDamagePos = vehicle.Driver.GetLastWeaponImpactCoords();

                        if (lastDamagePos != Vector3.Zero)
                        {
                            if (lastDamagePos.DistanceTo(PlayerVehicle.Position) < 15)
                            {
                                PlayerVehicle.ApplyDamage(PlayerVehicle.GetOffsetFromWorldCoords(lastDamagePos), 1500, 2500);
                                PlayerVehicle.Health -= 50;
                            }
                        }
                    }
                });
            }
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
            while (UFOS.Count > 0)
            {
                Vehicle vehicle = UFOS[0];
                vehicle.MarkAsNoLongerNeeded();
                UFOS.RemoveAt(0);
            }
        }

        private void DeleteAllVehicles()
        {
            while (UFOS.Count > 0)
            {
                Vehicle vehicle = UFOS[0];
                vehicle.Delete();
                UFOS.RemoveAt(0);
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
