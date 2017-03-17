using GTA;
using GTA.Math;
using SpaceMod.DataClasses;

namespace SpaceMod
{
    public static class Database
    {
        public const string PathToSprites = @"./scripts/SpaceMod/Sprites";
        public const string PathToInteriors = @"./scripts/SpaceMod/IPL";
        public const string PathToScenes = @"./scripts/SpaceMod/Scenes";
        public const string PathToScenarios = @"./scripts/SpaceMod/Scenarios";
        
        static Database()
        {
            AlienRelationship = World.AddRelationshipGroup("Aliens");
            World.SetRelationshipBetweenGroups(Relationship.Hate, Game.Player.Character.RelationshipGroup, AlienRelationship);
            World.SetRelationshipBetweenGroups(Relationship.Hate, AlienRelationship, Game.Player.Character.RelationshipGroup);
        }

        public static Vector3 TrevorAirport => new Vector3(1267.619f, 3137.67f, 40.41403f);
        public static Vector3 GalaxyCenter => new Vector3(-9994.448f, -12171.48f, 10000f);
        public static Vector3 PlanetSurfaceGalaxyCenter => new Vector3(-9994.448f, -12171.48f, 2500.197f);
        public static Vector3 EarthAtmosphereEnterPosition => new Vector3(-3874.793f, 3878.417f, 780.7289f);
        public static Vector3 SunOffsetNearEarth => new Vector3(0, 6500, 0);

        /// <summary>
        /// Return the Gameplay cameras current position if rendering, if the camera is not rendering (e.g. he's in a vehicle
        /// with first person cam) then it will return the player position.
        /// </summary>
        /// <param name="playerPed"></param>
        /// <returns></returns>
        public static Vector3 GetValidGalaxyDomePosition(Ped playerPed)
        {
            return GameplayCamera.IsRendering ? GameplayCamera.Position : playerPed.Position;
        }

        public static int AlienRelationship { get; }
    }
}
