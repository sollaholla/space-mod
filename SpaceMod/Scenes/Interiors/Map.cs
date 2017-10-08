using System.Collections.Generic;

namespace GTS.Scenes.Interiors
{
    /// <summary>
    /// All credits to GuadMaz for this script.
    /// </summary>
    public class Map
    {
        public List<MapObject> Objects = new List<MapObject>();
        public List<MapObject> RemoveFromWorld = new List<MapObject>();
    }
}