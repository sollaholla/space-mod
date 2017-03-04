using GTA;
using GTA.Math;

namespace SpaceMod
{
    public static class Database
    {
        //public const string SunSmallModel = "sun_small";
        //public const string MoonLargeModel = "moon_large";
        //public const string MoonMedModel = "moon_med";
        //public const string MoonSurfaceModel = "moon_surface";
        //public const string EarthLargeModel = "earth_large";
        //public const string EarthMedModel = "earth_med";
        //public const string EarthSmallModel = "earth_small";
        //public const string KaroonLargeModel = "alienplanet1_large";
        //public const string SpaceDomeModel = "spacedome";
        //public const string IsslModel = "issl";
        //public const string MarsLargeModel = "mars_large";
        //public const string MarsMedModel = "mars_med";
        //public const string MarsSmallModel = "mars_small";
        //public const string MarsSurfaceModel = "mars_surface";
        //public const string MarsBaseDoor001Model = "mars_base_door001";
        //public const string SpaceDomeAndromedaModel = "spacedome_and";
        //public const string WormHoleSmallModel = "spacedome_and_wrm_small";
        //public const string WormHoleLargeModel = "spacedome_and_wrm_small";
        //public const string BlueSunSmallModel = "bluesun_small";
        //public const string AlienPlanet1LargeModel = "alienplanet1_large";

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
        public static Vector3 EarthAtmosphereEnterPosition => new Vector3(-2618.882f, -2490.627f, 628.4431f);
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
