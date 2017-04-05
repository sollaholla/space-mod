#pragma warning disable 1587
//////////////////////////////////////////////////////////
/// 
/// Credits: All credits to Guad for this script.
/// 
/// Edited by Soloman Northrop as of 2/14/2017
/// 
/// Licensed under MIT
/// 
/// ///////////////////////////////////////////////////////
#pragma warning restore 1587

using System.Collections.Generic;
using GTA;
using GTA.Math;

namespace SpaceMod.Scenes.Interiors
{
    public class Map
    {
        public List<MapObject> Objects = new List<MapObject>();
        public List<MapObject> RemoveFromWorld = new List<MapObject>();
        public List<Marker> Markers = new List<Marker>();
        public MapMetadata Metadata;
    }

    public class MapMetadata
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

    public class Marker
    {
        public MarkerType Type;
        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 Scale;
        public Vector3? TeleportTarget;
        public int Red;
        public int Green;
        public int Blue;
        public int Alpha;
        public bool BobUpAndDown;
        public bool RotateToCamera;
        public bool OnlyVisibleInEditor;
        public int Id;
    }
}