using System;
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
        public int currentMissionStep { get; protected set; }

        public List<Ped> Aliens { get; }

        public List<Ped> PilotAliens { get; }

        public List<Vehicle> Ufos { get; }

        public Ped PlayerPed => Game.Player.Character;

        public bool OriginalCanPlayerRagdoll { get; }

        public int OriginalMaxHealth { get; }

        public Vector3 PlayerPosition
        {
            get { return PlayerPed.Position; }
            set { PlayerPed.Position = value; }
        }

        public MarsMission01()
        {
            Aliens = new List<Ped>();
            PilotAliens = new List<Ped>();
            Ufos = new List<Vehicle>();
            OriginalCanPlayerRagdoll = PlayerPed.CanRagdoll;
            OriginalMaxHealth = PlayerPed.MaxHealth;
            PlayerPed.MaxHealth = 1500;
            PlayerPed.CanRagdoll = false;
        }

        public override void Start()
        {
            SpawnEnemies();
        }

        public void SpawnEnemies()
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

            for(int i = 0; i < 2; i++)
            {
                Vector3 position = origin.Around(new Random().Next(25, 50));
                position.Z += 20;

                Ped alien = Utilities.CreateAlien(Vector3.Zero, WeaponHash.Railgun);
                alien.SetDefaultClothes();
                alien.AlwaysDiesOnLowHealth = false;
                alien.CanRagdoll = false;
                alien.IsOnlyDamagedByPlayer = true;

                Vehicle ufo = World.CreateVehicle(new Model("zanufo"), position);
                ufo.EngineRunning = true;
                ufo.MaxHealth = 2000;
                ufo.Health = ufo.MaxHealth;

                Blip UFOblip = ufo.AddBlip();
                Blip EnemyBlip = alien.AddBlip();

                UFOblip.Name = "Enemy UFO";

                EnemyBlip.Name = "Alien";
                EnemyBlip.Scale = 0.7f;

                Aliens.Add(alien);
                PilotAliens.Add(alien);

                Ufos.Add(ufo);
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
            switch(currentMissionStep)
            {
                case 0:

                    Aliens.ForEach(UpdateAlien);

                    if (!Aliens.All(alien => alien.IsDead)) return;
                    BigMessageThread.MessageInstance.ShowMissionPassedMessage("~r~ enemies eliminated");
                    currentMissionStep++;
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

            if (distance > 25 && !PilotAliens.Contains(alienPed))
            {
                alienPed.Task.RunTo(PlayerPed.Position, true);
            }
            else
            {
                alienPed.Task.FightAgainst(PlayerPed);
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
                Entity Craft = Ufos[0];
                Craft.MarkAsNoLongerNeeded();
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
