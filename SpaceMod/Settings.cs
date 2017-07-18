using GTA.Math;

namespace GTS
{
    /// <summary>
    /// Holds setting variables used in certain parts of the mod that need to be accessed.
    /// </summary>
    internal static class Settings
    {
        /// <summary>
        /// </summary>
        public static bool ShowCustomGui = true;

        /// <summary>
        /// </summary>
        public static bool UseScenarios = true;

        /// <summary>
        /// </summary>
        public static bool UseSpaceWalk = true;

        /// <summary>
        /// </summary>
        public static bool MoonJump = true;

        /// <summary>
        /// </summary>
        public static float MouseControlFlySensitivity = 3;

        /// <summary>
        /// </summary>
        public static int VehicleFlySpeed = 35;

        /// <summary>
        /// </summary>
        public static Vector3 DefaultVehicleSpawn = new Vector3(-10015f, -10015f, 10001f);
    }
}