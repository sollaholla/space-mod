using GTA;
using GTA.Math;
using GTA.Native;
using SpaceMod.Lib;
using System;

namespace DefaultMissions
{
    public static class HelperFunctions
    {
        private static Random random;

        static HelperFunctions()
        {
            random = new Random();
        }

        public static Ped SpawnAlien(
            Vector3 spawn, PedHash? model = null, float checkRadius = 5f, 
            WeaponHash weaponHash = WeaponHash.Railgun, int accuracy = 50, bool moveToGround = true)
        {
            Vector3 spawnPoint = spawn;

            if (moveToGround)
                spawn = spawn.MoveToGroundArtificial();

            if (spawnPoint == Vector3.Zero)
                return new Ped(0);

            if (World.GetNearbyPeds(spawnPoint, checkRadius).Length > 0)
                return new Ped(0);

            Ped ped = SpaceModLib.CreateAlien(spawnPoint, weaponHash, model, accuracy, random.Next(0, 359));

            if (Entity.Exists(ped))
                ped.Model.MarkAsNoLongerNeeded();

            return ped;
        }

        public static Vehicle SpawnUfo(Vector3 spawn, float checkRadius = 50, string model = "zanufo")
        {
            Vector3 spawnPoint = spawn.MoveToGroundArtificial();

            if (spawnPoint == Vector3.Zero)
                return new Vehicle(0);

            if (World.GetNearbyVehicles(spawnPoint, checkRadius).Length > 0)
                return new Vehicle(0);

            float heading;

            Vehicle vehicle = World.CreateVehicle(model, spawnPoint, heading = random.Next(0, 360));

            if (Entity.Exists(vehicle))
            {
                vehicle.Rotation = new Vector3(0, 0, heading);

                vehicle.Model.MarkAsNoLongerNeeded();
            }

            return vehicle;
        }
    }
}
