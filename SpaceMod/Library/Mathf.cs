namespace GTS.Library
{
    public static class Mathf
    {
        /// <summary>
        ///     <see cref="Clamp" /> the <paramref name="value" /> "value" between
        ///     min, and max.
        /// </summary>
        /// <param name="value">The value we wish to clamp.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <returns>
        /// </returns>
        public static float Clamp(float value, float min, float max)
        {
            if (value < min)
                value = min;
            else if (value > max)
                value = max;
            return value;
        }

        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }
    }
}