using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using GTA;
using GTA.Math;
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

        public static Vehicle SpawnUfo(Vector3 spawn, float checkRadius = 50, string model = "zanufo")
        {
            var spawnPoint = Utils.GetGroundHeightRay(spawn);

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
        ///     Returns <see langword="true" /> if we went to mars.
        /// </summary>
        /// <returns>
        /// </returns>
        public static bool DidGoToMars()
        {
            var currentDirectory = Directory.GetParent(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath)
                .FullName;
            var path = Path.Combine(currentDirectory, Path.ChangeExtension(typeof(Mars).Name, "ini"));
            var settings = ScriptSettings.Load(path);
            var currentStep = settings.GetValue(Mars.SettingsGeneralSectionString, Mars.SettingsMissionStepString, 0);
            return currentStep > 0;
        }
        
        /// <summary>
        ///     Returns <see langword="true" /> if we went to mars.
        /// </summary>
        /// <returns>
        /// </returns>
        public static bool DidCompleteScenario<T>()
        {
            var currentDirectory = Directory.GetParent(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath)
                .FullName;
            var path = Path.Combine(currentDirectory, Path.ChangeExtension(typeof(T).Name, "ini"));
            var settings = ScriptSettings.Load(path);
            var currentStep = settings.GetValue("SCENARIO_CONFIG", "COMPLETE", false);
            return currentStep;
        }
    }
}