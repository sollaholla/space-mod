using GTA;
using GTA.Math;

namespace GTS.OrbitalSystems
{
    public class AttachedOrbital : Entity
    {
        public AttachedOrbital(IHandleable prop, Vector3 attachOffset, Vector3 attachRotation) : base(prop.Handle)
        {
            AttachOffset = attachOffset;
            AttachRotation = attachRotation;
        }

        public Vector3 AttachOffset { get; }

        public Vector3 AttachRotation { get; }

        public bool FreezeX { get; set; }

        public bool FreezeY { get; set; }

        public bool FreezeZ { get; set; }
    }
}