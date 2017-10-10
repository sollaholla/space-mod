using System;
using System.ComponentModel;

namespace GTSCommon.DataModels
{
    [Serializable]
    public class SurfaceInfo : IDrawable
    {
        [Category("Settings")]
        [Description(
            "True if you want this surface to tile infinitely. WARNING: Your model should be seamless on each edge.")]
        public bool Tile { get; set; }

        [Category("Settings")]
        [Description(
            "The size of your terrain in meters. 1024 is the default scale. This will affect how far your terrain tiles are generated from the parent terrain tile.")]
        public float TileSize { get; set; } = 1024;

        [Category("Settings")]
        [Description(
            "The dimensions of the terrain e.g. 1x1 makes 7 tiles ([0,0][1,0][0,1][1,1][-1,0][0,-1][-1,-1]) etc.")]
        public int Dimensions { get; set; } = 4;

        [Category("Required")]
        [Description("The name of the ydr/ydd model. Example: 'earth_large'")]
        [RefreshProperties(RefreshProperties.All)]
        public string Model { get; set; }

        [Category("Required")]
        [Description("The position of this object offsetted from the center of space.")]
        [RefreshProperties(RefreshProperties.All)]
        public XVector3 Position { get; set; }

        [Category("Settings")]
        public int LodDistance { get; set; } = -1;

        public override string ToString()
        {
            return string.IsNullOrEmpty(Model) ? base.ToString() : Model;
        }
    }
}