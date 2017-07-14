using GTA.Math;

namespace SpaceMod
{
    internal static class Settings
    {
        public static bool ShowCustomUi = true;
        public static bool UseScenarios = true;
        public static bool UseSpaceWalk = true;
        public static bool MoonJump     = true;

        public static float MouseControlFlySensitivity  = 3;
        public static int VehicleFlySpeed               = 15;

        public static Vector3 DefaultVehicleSpawn = new Vector3(-10015f, -10015f, 10001f);
    }
}
