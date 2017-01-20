using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Math;

namespace SpaceMod
{
    public static class Constants
    {
        public const string SunSmallModel = "sun_small";
        public const string MoonLargeModel = "moon_large";
        public const string MoonMedModel = "moon_med";
        public const string MoonSurfaceModel = "moon_surface";
        public const string EarthLargeModel = "earth_large";
        public const string EarthMedModel = "earth_med";
        public const string EarthSmallModel = "earth_small";
        public const string SpaceDomeModel = "spacedome";

        public static Vector3 TrevorAirport => new Vector3(1267.619f, 3137.67f, 40.41403f);
        public static Vector3 GalaxyCenter => new Vector3(-9994.448f, -12171.48f, 8828.197f);
        public static Vector3 EarthAtmosphereEnterPosition => new Vector3(-2618.882f, -2490.627f, 628.4431f);
        public static Vector3 SunOffsetNearEarth => new Vector3(0, 6500, 0);

        public static Vector3 GetCurrentValidGalaxyPosition(Ped playerPed)
        {
            return GameplayCamera.IsRendering ? GameplayCamera.Position : playerPed.Position;
        }
    }
}
