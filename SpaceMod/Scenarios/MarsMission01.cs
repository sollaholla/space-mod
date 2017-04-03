using System;
using System.Collections.Generic;
using System.Linq;
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
            PlayerPed.Health = PlayerPed.MaxHealth;
            PlayerPed.CanRagdoll = false;
            PlayerPed.IsExplosionProof = true;

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

        public Vector3 PlayerPosition {
            get { return PlayerPed.Position; }
            set { PlayerPed.Position = value; }
        }

        public override void Start()
        {
            SpawnEntities();
        }

        public void SpawnEntities()
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

            _ufoModel = new Model(_ufoModelName);
            _ufoModel.Request();
            DateTime timout = DateTime.UtcNow + new TimeSpan(0, 0, 0, 10);
            while (!_ufoModel.IsLoaded)
            {
                Script.Yield();
                if (DateTime.UtcNow > timout)
                    break;
            }

            if (!_ufoModel.IsLoaded)
            {
                UI.Notify($"{_ufoModelName} model failed to load! Make sure you have a valid model in the .ini file.");
                EndScenario(false);
                return;
            }

            for (var i = 0; i < 1; i++)
            {
                Vector3 position = origin.Around(75);

                float heading = (position - PlayerPosition).ToHeading();
                Vehicle spaceCraft = World.CreateVehicle(_ufoModel, position + new Vector3(0, 0, 150),
                    heading);

                Ped ped = spaceCraft.CreatePedOnSeat(VehicleSeat.Driver, PedHash.MovAlien01);
                Function.Call(Hash.TASK_PLANE_MISSION, ped, spaceCraft, 0, PlayerPed, 0, 0, 0, 6, 25, 0, heading, 3000, 2515);

                spaceCraft.MaxHealth = 500;
                spaceCraft.Health = spaceCraft.MaxHealth;
                spaceCraft.MaxSpeed = 50;
                spaceCraft.Speed = 50;

                Blip blip = spaceCraft.AddBlip();
                blip.Name = "UFO";
                blip.Color = BlipColor.Green;

                Ufos.Add(spaceCraft);
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
            switch (CurrentMissionStep)
            {
                case 0:
                    Aliens.ForEach(UpdateAlien);
                    Ufos.ForEach(UpdateUfo);
                    if (!Aliens.All(alien => alien.IsDead) || !Ufos.All(ufo => ufo.Driver != null && ufo.Driver.IsDead)) return;
                    BigMessageThread.MessageInstance.ShowMissionPassedMessage("~r~ enemies eliminated");
                    CurrentMissionStep++;
                    break;

                case 1:

                    EndScenario(true);
                    break;
            }
        }

        private void UpdateUfo(Vehicle ufo)
        {
	        ufo.FreezePosition = CurrentScene.SceneData.CurrentIplData.Name == "mbi2";

	        if ((ufo.IsDead || ufo.Driver != null && ufo.Driver.IsDead || !ufo.IsDriveable) && ufo.CurrentBlip.Exists())
            {
                ufo.CurrentBlip.Remove();
                ufo.Driver?.Kill();
                World.AddExplosion(ufo.Position, ExplosionType.Grenade, 75, 1.5f, true, true);
            }
        }

        public void UpdateAlien(Ped alienPed)
		{
			alienPed.FreezePosition = CurrentScene.SceneData.CurrentIplData.Name == "mbi2";

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

            Utilities.ArtificalDamage(alienPed, PlayerPed, 1.5f, 75);

            float distance = Vector3.Distance(PlayerPosition, alienPed.Position);

            if (distance > 25)
            {
                alienPed.Task.RunTo(PlayerPed.Position, true);
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
            PlayerPed.IsExplosionProof = false;
        }

        private void MarkEntitesAsNotNeeded()
        {
            while (Aliens.Count > 0)
            {
                Ped alien = Aliens[0];
                alien.MarkAsNoLongerNeeded();
                Aliens.RemoveAt(0);
            }

            while (Ufos.Count > 0)
            {
                Vehicle craft = Ufos[0];
                craft.MarkAsNoLongerNeeded();
                craft.Driver?.Delete();
                Ufos.RemoveAt(0);
            }
        }

        private void CleanUp()
        {
            while (Aliens.Count > 0)
            {
                Ped alien = Aliens[0];
                alien.Delete();
                Aliens.RemoveAt(0);
            }

            while (Ufos.Count > 0)
            {
                Entity craft = Ufos[0];
                craft.Delete();
                Ufos.RemoveAt(0);
            }
        }

    }
}
