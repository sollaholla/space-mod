//////////////////////////////////////////////////////////
/// 
/// Credits: All credits to Guad for this script.
/// 
/// Edited by Soloman Northrop as of 2/14/2017
/// 
/// Licensed under MIT
/// 
/// ///////////////////////////////////////////////////////

using System.Collections.Generic;
using GTA;
using GTA.Math;

namespace GTS.Scenes.Interiors
{
    internal class Map
    {
        public List<MapObject> Objects = new List<MapObject>();
        public List<MapObject> RemoveFromWorld = new List<MapObject>();
    }

    internal class MapMetadata
    {
        public MapMetadata()
        {
            Creator = Game.Player.Name;
            Name = "Nameless Map";
            Description = "";
        }

        public string Creator { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Filename { get; set; }

        public Vector3? LoadingPoint { get; set; }
        public Vector3? TeleportPoint { get; set; }

        public bool ShouldSerializeFilename()
        {
            return false;
        }
    }
}