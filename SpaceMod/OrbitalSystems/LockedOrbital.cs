using GTA;
using GTA.Math;

namespace SpaceMod.OrbitalSystems
{
    public class LockedOrbital : Entity
    {
        private readonly Prop _prop;

        public LockedOrbital(int handle, Vector3 offset, bool emitLight, float scale) : base(handle)
        {
            _prop = new Prop(handle);
	        Offset = offset;
	        EmitLight = emitLight;
	        Scale = scale;
        }

		public Vector3 Offset { get; }

		public bool EmitLight { get; }

		public float Scale { get; }

        public new Vector3 Position
        {
            get { return _prop.Position; }
            set { _prop.Position = value; }
        }

        public new Vector3 Rotation
        {
            get { return _prop.Rotation; }
            set { _prop.Rotation = value; }
        }
    }
}
