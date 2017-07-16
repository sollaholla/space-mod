using GTA;
using GTA.Math;

namespace GTS.OrbitalSystems
{
    public class Orbital : AttachedOrbital
    {
        public Orbital(Prop prop, string name, float rotationSpeed) : base(prop, Vector3.Zero)
        {
            Name = name;

            RotationSpeed = rotationSpeed;
        }

        public string Name { get; set; }

        public bool WormHole { get; set; }

        public float RotationSpeed { get; set; }

        public void Rotate()
        {
            var rotation = Rotation;
            rotation.Z += Game.LastFrameTime * RotationSpeed;
            Rotation = rotation;
        }
    }
}