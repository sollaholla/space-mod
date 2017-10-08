using GTA.Math;

namespace GTS.Utility
{
    public static class VectorParse
    {
        /// <summary>
        ///     <see cref="VectorParse.Read" /> a <see cref="Vector3" /> from a
        ///     <see cref="string" /> representation. Format should be "X:1 Y:2
        ///     Z:3". If any number fails to be parsed, or an error occurs, this
        ///     will return <see cref="GTA.Math.Vector3.Zero" /> .
        /// </summary>
        /// <param name="str"></param>
        /// <param name="defaultValue"></param>
        /// <returns>
        /// </returns>
        public static Vector3 Read(string str, Vector3 defaultValue)
        {
            try
            {
                // if we passed an empty string then just forget it.
                if (string.IsNullOrEmpty(str))
                    return defaultValue;

                str = str.Replace("X:", string.Empty).Replace("Y:", string.Empty).Replace("Z:", string.Empty);
                var split = str.Split(' ');

                // we'll use these as the new values.
                var x = split[0];
                var y = split[1];
                var z = split[2];

                // if we succeed in parsing them all, then we return the vector3.
                if (float.TryParse(x, out float newX) &&
                    float.TryParse(y, out float newY) &&
                    float.TryParse(z, out float newZ))
                    return new Vector3(newX, newY, newZ);

                // otherwise we just return the default value.
                return defaultValue;
            }
            catch
            {
                // couldn't parse the string so we just return an empty vector3.
                return defaultValue;
            }
        }
    }
}