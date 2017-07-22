using System;
using GTA.Math;

namespace DefaultMissions
{
    public class Trigger
    {
        public Trigger(Vector3 position, float radius)
        {
            Position = position;
            Radius = radius;
        }

        public Vector3 Position { get; }

        public float Radius { get; }

        public bool IsInTrigger(Vector3 position)
        {
            var distance = Vector3.DistanceSquared(Position, position);
            return distance <= Convert.ToSingle(Math.Pow(Radius, 2));
        }
    }
}