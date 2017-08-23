using GTA;

namespace GTS.OrbitalSystems
{
    public class Orbital : Entity
    {
        public Orbital(IHandleable prop, string name, float rotationSpeed) : base(prop.Handle)
        {
            Name = name;
            RotationSpeed = rotationSpeed;
        }

        public string Name { get; set; }

        public bool WormHole { get; set; }

        public float RotationSpeed { get; set; }
    }
}