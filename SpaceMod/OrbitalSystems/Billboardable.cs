using GTA;
using GTA.Math;

namespace GTS.OrbitalSystems
{
    public sealed class Billboardable : Entity
    {
        public Billboardable(int handle, Vector3 startPosition) : base(handle)
        {
            StartPosition = startPosition;
            Rotation = Vector3.Zero;
        }

        public Vector3 StartPosition { get; }

        public float ParallaxAmount { get; set; }

        public float ParallaxStartDistance { get; set; }
    }
}