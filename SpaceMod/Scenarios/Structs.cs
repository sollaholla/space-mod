using GTA.Math;

namespace DefaultMissions
{
    public struct Trigger
    {
        public Trigger(Vector3 position, float radius) : this()
        {
            Position = position;
            Radius = radius;
        }

        public Vector3 Position { get; }

        public float Radius { get; }

        public bool IsInTrigger(Vector3 position)
        {
            float distance = Vector3.DistanceSquared(Position, position);

            if (distance <= Radius * Radius)
            {
                return true;
            }

            return false;
        }
    }
}
