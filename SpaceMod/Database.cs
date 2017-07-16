using GTA;
using GTA.Math;

namespace GTS
{
    public static class Database
    {
        internal const string PathToInteriors = ".\\scripts\\Space\\Interiors";
        internal const string PathToScenes = ".\\scripts\\Space\\Scenes";
        internal const string PathToScenarios = ".\\scripts\\Space\\Scenarios";
        internal const string NotifyHeader = "~p~~h~Grand Theft Space~h~~n~~s~";

        static Database()
        {
            AlienRelationshipGroup = World.AddRelationshipGroup("Aliens");
            World.SetRelationshipBetweenGroups(Relationship.Hate, Game.Player.Character.RelationshipGroup,
                AlienRelationshipGroup);
            World.SetRelationshipBetweenGroups(Relationship.Hate, AlienRelationshipGroup,
                Game.Player.Character.RelationshipGroup);

            TrevorAirport = new Vector3(1267.619f, 3137.67f, 40.41403f);
            EarthAtmosphereEnterPosition = new Vector3(-3874.793f, 3878.417f, 780.7289f);
        }

        public static Vector3 TrevorAirport { get; }
        public static int AlienRelationshipGroup { get; }
        public static Vector3 EarthAtmosphereEnterPosition { get; }

        /// <summary>
        ///     Return the Gameplay cameras current position if rendering, if the camera is not rendering (e.g. he's in a vehicle
        ///     with first person cam) then it will return the player position.
        /// </summary>
        /// <returns></returns>
        internal static Vector3 GetGalaxPosition()
        {
            return
                GameplayCamera.IsRendering
                    ? GameplayCamera.Position
                    : Camera.Exists(World.RenderingCamera)
                        ? World.RenderingCamera.Position
                        : Game.Player.Character.Position;
        }
    }
}