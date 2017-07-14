using GTA;
using GTA.Math;

namespace GTS.OrbitalSystems
{
    public class AttachedOrbital : Entity
    {
        private readonly Prop prop;

        public AttachedOrbital(Prop prop, Vector3 attachOffset) : base(prop.Handle)
        {
            this.prop = prop;

	        AttachOffset = attachOffset;
        }

		public Vector3 AttachOffset { get; }
    }
}
