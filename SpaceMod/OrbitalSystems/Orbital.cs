using GTA;
using GTA.Math;

namespace GTS.OrbitalSystems
{
    public class Orbital : Entity
    {
        public Orbital(IHandleable prop, string name, float rotationSpeed,
            bool wormHole, float triggerDistance, string nextScene, Vector3 nextScenePosition,
            Vector3 nextSceneRotation) : base(prop.Handle)
        {
            Name = name;
            RotationSpeed = rotationSpeed;
            WormHole = wormHole;
            TriggerDistance = triggerDistance;
            NextScene = nextScene;
            NextScenePosition = nextScenePosition;
            NextSceneRotation = nextSceneRotation;
            OriginalPosition = Position;
        }

        public string Name { get; set; }
        public float RotationSpeed { get; set; }
        public bool WormHole { get; set; }
        public float TriggerDistance { get; set; }
        public string NextScene { get; set; }
        public Vector3 NextScenePosition { get; set; }
        public Vector3 NextSceneRotation { get; set; }
        public Vector3 OriginalPosition { get; set; }
    }
}