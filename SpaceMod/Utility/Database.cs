﻿using GTA;
using GTA.Math;

namespace GTS.Utility
{
    /// <summary>
    ///     Holds data information about the mod.
    /// </summary>
    public static class Database
    {
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
        ///     Return the <c>Gameplay</c> cameras current position if rendering, if
        ///     the camera is not rendering (e.g. he's in a vehicle with first
        ///     person cam) then it will return the player position.
        /// </summary>
        /// <returns>
        /// </returns>
        internal static Vector3 ViewFinderPosition()
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