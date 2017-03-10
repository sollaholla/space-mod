﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Native;
using GTA.Math;
using SpaceMod;
using SpaceMod.DataClasses;
using NativeUI;

namespace DefaultMissions
{
    public class MarsMission01 : CustomScenario
    {
        private readonly string _ufoModelName = "zanufo";
        private Model _ufoModel;

        public MarsMission01()
        {
            Aliens = new List<Ped>();
            Ufos = new List<Vehicle>();
            OriginalCanPlayerRagdoll = PlayerPed.CanRagdoll;
            OriginalMaxHealth = PlayerPed.MaxHealth;
            PlayerPed.MaxHealth = 1500;
            PlayerPed.CanRagdoll = false;

            _ufoModelName = Settings.GetValue("settings", "ufo_model", _ufoModelName);
            Settings.SetValue("settings", "ufo_model", _ufoModelName);
            Settings.Save();

        }

        public int CurrentMissionStep { get; protected set; }

        public List<Ped> Aliens { get; }

        public List<Vehicle> Ufos { get; }

        public Ped PlayerPed => Game.Player.Character;

        public bool OriginalCanPlayerRagdoll { get; }

        public int OriginalMaxHealth { get; }

        public bool SpawnedUfos { get; private set; }

        public Vector3 PlayerPosition {
            get { return PlayerPed.Position; }
            set { PlayerPed.Position = value; }
        }

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

            SpawnPeds();
        }

        public void SpawnPeds()
        {
            var origin = PlayerPosition.Around(100f);

            for (var i = 0; i < 15; i++)
            {
                Vector3 position = origin.Around(new Random().Next(50, 75));
                Vector3 artificial = TryToGetGroundHeight(position);
                if (artificial != Vector3.Zero) position = artificial;

                Ped ped = Utilities.CreateAlien(position, WeaponHash.Railgun);
                ped.SetDefaultClothes();
                ped.Health = 3750;
                ped.AlwaysDiesOnLowHealth = false;
                ped.CanRagdoll = false;
                ped.IsOnlyDamagedByPlayer = true;

                Blip blip = ped.AddBlip();
                blip.Name = "Alien";
                blip.Scale = 0.7f;

                Aliens.Add(ped);
            }
        }

        private void CreateUfos()
        {
            if (!_ufoModel.IsLoaded)
            {
                UI.Notify($"{_ufoModelName} model failed to load! Make sure you have a valid model in the .ini file.");
                EndScenario(false);
                return;
            }

            Vector3 origin = PlayerPosition.Around(100f);

            for (int i = 0; i < 1; i++)
            {
                Vehicle vehicle = World.CreateVehicle(_ufoModel, origin + new Vector3(0, 0, 25));
                Ped ped = vehicle.CreatePedOnSeat(VehicleSeat.Driver, PedHash.MovAlien01);
                ped.SetDefaultClothes();
                vehicle.EngineRunning = true;
                vehicle.Heading = (PlayerPosition - vehicle.Position).ToHeading();
                vehicle.MaxSpeed = 50;
                vehicle.Speed = 50;
                Blip blip = vehicle.AddBlip();
                blip.Name = "UFO";
                blip.Color = BlipColor.Green;
                ped.Task.FightAgainst(Game.Player.Character);
                Ufos.Add(vehicle);
            }
        }

        private static Vector3 TryToGetGroundHeight(Vector3 position)
        {
            var artificial = position.MoveToGroundArtificial();
            var timeout = DateTime.UtcNow + new TimeSpan(0, 0, 0, 0, 1500);

            while (artificial == Vector3.Zero)
            {
                artificial = position.MoveToGroundArtificial();

                Script.Yield();

                if (DateTime.UtcNow > timeout)
                    break;
            }

            return artificial;
        }

        public override void OnUpdate()
        {
            switch(CurrentMissionStep)
            {
                case 0:
                    if (Game.IsLoading) return;
                    if (!SpawnedUfos)
                    {
                        CreateUfos();
                        SpawnedUfos = true;
                    }
                    else
                    {
                        Aliens.ForEach(UpdateAlien);
                        if (!Aliens.All(alien => alien.IsDead)) return;
                        BigMessageThread.MessageInstance.ShowMissionPassedMessage("~r~ enemies eliminated");
                        CurrentMissionStep++;
                    }
                break;

                case 1:

                    EndScenario(true);
                break;
            }
        }

        public void UpdateAlien(Ped alienPed)
        {
            if (alienPed.IsDead)
            {
                if (alienPed.CurrentBlip.Exists())
                {
                    alienPed.MarkAsNoLongerNeeded();
                    alienPed.CurrentBlip.Remove();
                    alienPed.CanRagdoll = true;
                }

                return;
            }

            float distance = Vector3.Distance(PlayerPosition, alienPed.Position);

            if (distance > 25)
            {
                alienPed.Task.RunTo(PlayerPed.Position, true);
            }
        }

        private void MarkEntitesAsNotNeeded()
        {
            while (Aliens.Count > 0)
            {
                Ped Alien = Aliens[0];
                Alien.MarkAsNoLongerNeeded();
                Aliens.RemoveAt(0);
            }

            while (Ufos.Count > 0)
            {
                Vehicle Craft = Ufos[0];
                Craft.MarkAsNoLongerNeeded();
                Craft.Driver?.Delete();
                Ufos.RemoveAt(0);
            }
        }

        private void CleanUp()
        {
            while (Aliens.Count > 0)
            {
                Ped Alien = Aliens[0];
                Alien.Delete();
                Aliens.RemoveAt(0);
            }

            while (Ufos.Count > 0)
            {
                Entity Craft = Ufos[0];
                Craft.Delete();
                Ufos.RemoveAt(0);
            }
        }

        public override void OnAborted()
        {
            CleanUp();
        }

        public override void OnEnded(bool success)
        {
            if (success)
            {
                MarkEntitesAsNotNeeded();
            }
            else
            {
                CleanUp();
            }

            PlayerPed.MaxHealth = OriginalMaxHealth;
            PlayerPed.Health = PlayerPed.MaxHealth;
            PlayerPed.CanRagdoll = OriginalCanPlayerRagdoll;
        }

    }
}