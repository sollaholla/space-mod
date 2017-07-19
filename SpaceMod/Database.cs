using GTA;
using GTA.Math;

namespace GTS
{
    /// <summary>
    ///     Holds data information about the mod.
    /// </summary>
    public static class Database
    {
        /// <summary>
        /// </summary>
        internal const string PathToInteriors = ".\\scripts\\Space\\Interiors";

        /// <summary>
        /// </summary>
        internal const string PathToScenes = ".\\scripts\\Space\\Scenes";

        /// <summary>
        /// </summary>
        internal const string PathToScenarios = ".\\scripts\\Space\\Scenarios";

        /// <summary>
        /// </summary>
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

        /// <summary>
        ///     The location of the airfield owned by Trevor.
        /// </summary>
        public static Vector3 TrevorAirport { get; }

        /// <summary>
        ///     The relationship group for aliens.
        /// </summary>
        public static int AlienRelationshipGroup { get; }

        /// <summary>
        ///     The location we teleport once we enter earths atmosphere.
        /// </summary>
        public static Vector3 EarthAtmosphereEnterPosition { get; }

        /// <summary>
        ///     Return the <c>Gameplay</c> cameras current position if rendering, if
        ///     the camera is not rendering (e.g. he's in a vehicle with first
        ///     person cam) then it will return the player position.
        /// </summary>
        /// <returns>
        /// </returns>
        internal static Vector3 GetGalaxyPosition()
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