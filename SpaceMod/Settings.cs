using GTA.Math;

namespace GTS
{
    /// <summary>
    ///     Holds setting variables used in certain parts of the mod that need to be
    ///     accessed.
    /// </summary>
    internal static class Settings
    {
        public static bool ShowCustomGui = true;

        public static bool UseScenarios = true;

        public static bool UseSpaceWalk = true;

        public static bool MoonJump = true;

        public static float MouseControlFlySensitivity = 3;

        public static int VehicleFlySpeed = 35;

        public static Vector3 EarthAtmosphereEnterPosition = new Vector3(-4000, 4000, 6000);

        public static Vector3 EarthAtmosphereEnterRotation = new Vector3(-35, 0, 0);

        public static bool DebugTriggers = false;

        public static bool AlwaysUseSound = false;

        public static float EnterOrbitHeight = 6500f;

        public static Vector3 DefaultSceneRotation = new Vector3(0, 0, 207.6315f);

        public static Vector3 DefaultScenePosition = new Vector3(-1539.895f, 3385.976f, 62.89648f);

        public static string DefaultScene = "EarthOrbit.space";
    }
}