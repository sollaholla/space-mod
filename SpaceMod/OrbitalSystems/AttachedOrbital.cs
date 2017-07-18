using GTA;
using GTA.Math;

namespace GTS.OrbitalSystems
{
    public class AttachedOrbital : Entity
    {
        private readonly Prop _prop;

        public AttachedOrbital(Prop prop, Vector3 attachOffset, Vector3 attachRotation) : base(prop.Handle)
        {
            _prop = prop;
            AttachOffset = attachOffset;
            AttachRotation = attachRotation;
        }

        public Vector3 AttachOffset { get; }

        public Vector3 AttachRotation { get; }
    }
}