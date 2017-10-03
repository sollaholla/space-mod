using GTA;

namespace GTS.DataClasses
{
    public class WarpEffect : Entity
    {
        public WarpEffect(int handle, float movementOffset, float rotationOffset, float movementSpeed,
            float rotationSpeed) : base(handle)
        {
            MovementOffset = movementOffset;
            RotationOffset = rotationOffset;
            MovementSpeed = movementSpeed;
            RotationSpeed = rotationSpeed;
        }

        public Prop Extension { get; set; }
        public float MovementOffset { get; set; }
        public float RotationOffset { get; set; }
        public float MovementSpeed { get; set; }
        public float RotationSpeed { get; set; }

        public new void Delete()
        {
            Extension?.Delete();
            base.Delete();
        }
    }
}