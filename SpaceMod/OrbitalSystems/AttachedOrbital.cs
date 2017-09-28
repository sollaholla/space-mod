using GTA;
using GTA.Math;

namespace GTS.OrbitalSystems
{
    public class AttachedOrbital : Entity
    {
        public AttachedOrbital(IHandleable prop, Vector3 attachOffset, Vector3 attachRotation, 
            bool freezeX, bool freezeY, bool freezeZ,
            bool shiftX, bool shiftY, bool shiftZ,
            float shiftAmount) 
            : base(prop.Handle)
        {
            AttachOffset = attachOffset;
            AttachRotation = attachRotation;
            FreezeX = freezeX;
            FreezeY = freezeY;
            FreezeZ = freezeZ;
            ShiftAmount = shiftAmount;
            ShiftX = shiftX;
            ShiftY = shiftY;
            ShiftZ = shiftZ;
        }

        public Vector3 AttachOffset { get; }

        public Vector3 AttachRotation { get; }

        public bool FreezeX { get; set; }

        public bool FreezeY { get; set; }

        public bool FreezeZ { get; set; }

        public bool ShiftX { get; set; }

        public bool ShiftY { get; set; }

        public bool ShiftZ { get; set; }

        public float ShiftAmount { get; set; }
    }
}