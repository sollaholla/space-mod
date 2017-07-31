using GTA;
using GTA.Math;

namespace GTS.OrbitalSystems
{
    public class Billboardable : Entity
    {
        public Billboardable(int handle, Vector3 originalPosition, float perpectiveScaling) : base(handle)
        {
            ParallaxScaling = perpectiveScaling;
            OriginalPosition = originalPosition;
        }

        public Vector3 OriginalPosition { get; }

        public float ParallaxScaling { get; }
    }
}