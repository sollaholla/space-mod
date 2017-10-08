using GTA;

namespace GTS.OrbitalSystems
{
    public class Orbital : Entity
    {
        public Orbital(int handle, string name, float rotationSpeed,
            bool wormHole, float triggerSizeMult, string nextScene) : base(handle)
        {
            Name = name;
            RotationSpeed = rotationSpeed;
            WormHole = wormHole;
            TriggerSizeMult = triggerSizeMult;
            NextScene = nextScene;
        }

        public string Name { get; set; }
        public float RotationSpeed { get; set; }
        public bool WormHole { get; set; }
        public float TriggerSizeMult { get; set; }
        public string NextScene { get; set; }
    }
}