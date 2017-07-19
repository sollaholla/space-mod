﻿using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using GTA;
using GTA.Math;
using GTA.Native;
using GTS.Library;
using GTS.Scenes;

namespace DefaultMissions
{
    public static class HelperFunctions
    {
        private static readonly Random Random;

        static HelperFunctions()
        {
            Random = new Random();
        }

        public static Ped SpawnAlien(
            Vector3 spawn, PedHash? model = null, float checkRadius = 5f,
            WeaponHash weaponHash = WeaponHash.Railgun, int accuracy = 50, bool moveToGround = true,
            bool markModelAsNoLongerNeeded = true)
        {
            var spawnPoint = spawn;

            if (moveToGround)
                spawnPoint = spawn.MoveToGroundArtificial();

            if (spawnPoint == Vector3.Zero)
                return new Ped(0);

            if (World.GetNearbyPeds(spawnPoint, checkRadius).Length > 0)
                return new Ped(0);

            var ped = Utils.CreateAlien(spawnPoint, weaponHash, model, accuracy, Random.Next(0, 359));

            if (Entity.Exists(ped) && markModelAsNoLongerNeeded)
                ped.Model.MarkAsNoLongerNeeded();

            return ped;
        }

        public static Vehicle SpawnUfo(Vector3 spawn, float checkRadius = 50, string model = "zanufo")
        {
            var spawnPoint = spawn.MoveToGroundArtificial();

            if (spawnPoint == Vector3.Zero)
                return new Vehicle(0);

            if (World.GetNearbyVehicles(spawnPoint, checkRadius).Length > 0)
                return new Vehicle(0);

            float heading;

            var vehicle = World.CreateVehicle(model, spawnPoint, heading = Random.Next(0, 360));

            if (Entity.Exists(vehicle))
            {
                vehicle.Rotation = new Vector3(0, 0, heading);

                vehicle.Model.MarkAsNoLongerNeeded();
            }

            return vehicle;
        }

        public static void DrawWaypoint(Scene scene, Vector3 position)
        {
            var distance = Vector3.Distance(position, Game.Player.Character.Position);

            scene.DrawMarkerAt(position, $"{distance:N0}M", Color.White);
        }

        /// <summary>
        /// Returns <see langword="true"/> if we went to mars.
        /// </summary>
        /// <returns></returns>
        public static bool DidGoToMars()
        {
            var currentDirectory = Directory.GetParent(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath)
                .FullName;
            var path = Path.Combine(currentDirectory, Path.ChangeExtension(typeof(Mars).Name, "ini"));
            var settings = ScriptSettings.Load(path);
            var currentStep = settings.GetValue(Mars.SettingsGeneralSectionString, Mars.SettingsMissionStepString, 0);
            return currentStep > 0;
        }
    }
}