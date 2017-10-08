using GTA;
using GTA.Math;

namespace GTS.Scenes.Interiors
{
    /// <summary>
    /// All credits to GuadMaz for this script.
    /// </summary>
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
}