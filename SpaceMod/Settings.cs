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

        public static float VehicleReentrySpeed = 500f;

        public static Vector3 EarthAtmosphereEnterPosition = new Vector3(-5000f, 5000f, 1000f);

        public static Vector3 EarthAtmosphereEnterRotation = new Vector3(35f, 0f, 180f);

        public static bool DebugTriggers = false;

        public static bool AlwaysUseSound = true;

        public static float EnterOrbitHeight = 10000f;

        public static Vector3 DefaultOrbitOffset = new Vector3(-1710.931f, 3930.234f, 69.25195f);

        public static Vector3 DefaultOrbitRotation = new Vector3(37.83031f, -74.92153f, 61.03346f);

        public static string DefaultOrbitScene = "1FullSolarSystem.space";

        public static bool DisableWantedLevel = true;

        public static float ShutStage1Height = 5000f;

        public static float ShutStage2Height = 7000f;

        public static float ShuttleNewtonsOfForce = 9.8f;

        public static float ShuttleThrustInterpolation = 0.5f;

        public static float ShuttleGimbalFront = 0.18f;

        public static string SpaceVehiclesPath = ".\\scripts\\GrandTheftSpace\\Space\\SpaceVehicles.xml";

        public static string ScenesFolder = ".\\scripts\\GrandTheftSpace\\Space\\Scenes";

        public static string InteriorsFolder = ".\\scripts\\GrandTheftSpace\\Space\\Interiors";

        public static string ScenariosFolder = ".\\scripts\\GrandTheftSpace\\Space\\Scenarios";

        public static string AudioFolder = ".\\scripts\\GrandTheftSpace\\Space\\Audio";

        public static string TimecycleModifierPath = ".\\scripts\\GrandTheftSpace\\Space\\TimecycleMods.txt";

        public static string LogPath = ".\\scripts\\GrandTheftSpace\\GTS.log";
    }
}