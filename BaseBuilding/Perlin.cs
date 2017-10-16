using System;

namespace BaseBuilding
{
    public static class Perlin
    {
        private const long M = 4294967296;
        private const long A = 1664525;
        private const long C = 1;
        private static readonly Random Random = new Random();

        public static float GetNoise()
        {
            var z = (float) Math.Floor((float) Random.NextDouble() * M);
            z = (A * z + C) % M;
            return z / M;
        }
    }
}