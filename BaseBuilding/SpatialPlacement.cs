using GTA;
using GTA.Math;

namespace BaseBuilder
{
    public class SpatialPlacement : ISpatial
    {
        public SpatialPlacement(Vector3 rotation, Vector3 position)
        {
            Rotation = rotation;
            Position = position;
        }

        public Vector3 Rotation { get; set; }
        public Vector3 Position { get; set; }
    }
}
