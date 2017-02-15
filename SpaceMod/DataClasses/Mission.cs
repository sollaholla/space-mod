using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;

namespace SpaceMod.DataClasses
{
    public abstract class Mission
    {
        internal Random _random = new Random();

        public delegate void OnMissionEndedEvent();

        public event OnMissionEndedEvent MissionEnded;

        public abstract void Tick(Ped playerPed, Scene currentScene);

        public void End(bool failed)
        {
            BigMessageThread.MessageInstance.ShowMissionPassedMessage(failed ? "~r~mission failed" : "mission complete");
            MissionEnded?.Invoke();
        }

        public abstract void Abort();
        public abstract void CleanUp();
        
        public void DefaultEnemySpawn(ISpatial spawnOrigin, ref List<Ped> aliensList, ref List<Entity> spaceCraftList, WeaponHash weaponHash = WeaponHash.Railgun)
        {
            var origin = spawnOrigin.Position.Around(100);

            // spawn 20 enemies.
            for (var i = 0; i < 20; i++)
            {
                // Get position.
                var position = origin.Around(_random.Next(50, 75));
                var artificial = position.MoveToGroundArtificial();
                var timeout = DateTime.UtcNow + new TimeSpan(0, 0, 0, 0, 1500);
                while (artificial == Vector3.Zero)
                {
                    artificial = position.MoveToGroundArtificial();
                    Script.Yield();
                    if (DateTime.UtcNow > timeout)
                        break;
                }
                if (artificial != Vector3.Zero)
                    position = artificial;

                // Create ped.
                var ped = Utilities.CreateAlien(position, weaponHash);

                // Create blip.
                var blip = ped.AddBlip();
                blip.Name = "Alien Hostile";
                blip.Scale = 0.7f;
                ped.SetDefaultClothes();
                aliensList?.Add(ped);
            }

            // Spawn spaceships.
            for (var i = 0; i < 5; i++)
            {
                // Move the spaceship to a spawn position.
                var position = origin.Around(_random.Next(50, 80));
                var artificial = position.MoveToGroundArtificial();
                var timeout = DateTime.UtcNow + new TimeSpan(0, 0, 0, 0, 1500);
                while (artificial == Vector3.Zero)
                {
                    Script.Yield();
                    artificial = position.MoveToGroundArtificial();
                    if (DateTime.UtcNow > timeout)
                        break;
                }
                var offset = new Vector3(0, 0, 15);
                if (artificial != Vector3.Zero)
                    position = artificial + offset;
                else position += offset;

                // Skip this loop if the position is too close to another one.
                if (Utilities.IsCloseToAnyEntity(position, spaceCraftList, 25))
                    continue;

                // Create he spacecraft.
                var spaceCraft = World.CreateProp("ufo_zancudo", Vector3.Zero, false, false);
                spaceCraft.IsPersistent = true;
                spaceCraft.FreezePosition = true;
                spaceCraft.Position = position;
                spaceCraft.Health = spaceCraft.MaxHealth = 4500;
                spaceCraft.IsFireProof = true;      // NOTE: There's no fire in a space vaccum.

                // Configure the blip.
                var blip = spaceCraft.AddBlip();
                blip.Sprite = BlipSprite.SonicWave;
                blip.Color = BlipColor.Green;
                blip.Name = "Alien Aircraft";
                spaceCraftList?.Add(spaceCraft);
            }
        }
    }
}
