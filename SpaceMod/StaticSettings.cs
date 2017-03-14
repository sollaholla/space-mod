using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Math;

namespace SpaceMod
{
    public static class StaticSettings
    {
        // Mod stuff
        public static bool showCustomUI = true;
        public static bool UseScenarios = true;

        // Vehicle stuff.
        public static float MouseControlFlySensitivity = 3;
        public static Vector3 VehicleSurfaceSpawn = new Vector3(-9978f, -12190.14f, 2500f);
        public static int VehicleFlySpeed = 50;
    }
}
