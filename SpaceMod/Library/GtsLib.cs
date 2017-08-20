using System.Collections.Generic;
using System.Runtime.InteropServices;
using GTA;
using GTA.Native;

namespace GTS.Library
{
    /// <summary>
    ///     Full Credits: Unknown Modder (c) 2017
    ///     GTSLib.asi, NoBoundaryLimits.asi, RespawnFix.asi
    /// </summary>
    internal static class GtsLib
    {
        private static readonly Dictionary<string, uint> ScriptStackSizes = new Dictionary<string, uint>();

        [DllImport("GTSLib.asi")]
        private static extern bool GTSLib_IsLibraryInitialized();

        [DllImport("GTSLib.asi")]
        private static extern bool GTSLib_InitCredits();

        [DllImport("GTSLib.asi")]
        private static extern void GTSLib_EndCredits();

        [DllImport("GTSLib.asi")]
        private static extern void GTSLib_SetWorldGravity(float gravity);

        [DllImport("GTSLib.asi")]
        private static extern void GTSLib_RemoveWater();

        [DllImport("GTSLib.asi")]
        private static extern void GTSLib_RestoreWater();

        [DllImport("GTSLib.asi")]
        private static extern byte GTSLib_IsRockstarEditorActive();

        [DllImport("GTSLib.asi")]
        private static extern void GTSLib_SetScriptCanBePaused([MarshalAs(UnmanagedType.LPStr)] string name,
            bool toggle);

        [DllImport("GTSLib.asi")]
        private static extern uint GTSLib_GetScriptAllocatedStackSize([MarshalAs(UnmanagedType.LPStr)] string name);

        [DllImport("GTSLib.asi")]
        private static extern void GTSLib_SetVehicleGravity(int vehicle, float gravity);

        /// <summary>
        ///     Run the custom in-game credits for Grand Theft Space.
        /// </summary>
        public static void InitCredits()
        {
            if (!GTSLib_IsLibraryInitialized()) return;
            if (!GTSLib_InitCredits()) return;
            Function.Call(Hash.CLEAR_PRINTS);
            Function.Call(Hash.CLEAR_BRIEF);
            Function.Call(Hash.CLEAR_ALL_HELP_MESSAGES);
            Function.Call(Hash.SET_CREDITS_ACTIVE, true);
            Function.Call(Hash._0xB51B9AB9EF81868C, false);
            Function.Call(Hash.SET_GAME_PAUSES_FOR_STREAMING, false);
            Function.Call(Hash.DISPLAY_RADAR, false);
            Function.Call(Hash.DISPLAY_HUD, false);
            Function.Call(Hash._0x23227DF0B2115469);
        }

        /// <summary>
        ///     Make sure you call <see cref="InitCredits" /> before calling this method.
        /// </summary>
        public static void EndCredits()
        {
            if (!GTSLib_IsLibraryInitialized()) return;
            Function.Call(Hash.SET_CREDITS_ACTIVE, false);
            Function.Call(Hash._0xB51B9AB9EF81868C, true);
            Function.Call(Hash.SET_GAME_PAUSES_FOR_STREAMING, true);
            Function.Call(Hash.DISPLAY_RADAR, true);
            Function.Call(Hash.DISPLAY_HUD, true);
            GTSLib_EndCredits();
        }

        /// <summary>
        ///     Set the world gravity level to the specified value. Default value: 9.8000002f.
        /// </summary>
        /// <param name="gravity"></param>
        public static void SetGravityLevel(float gravity)
        {
            if (!GTSLib_IsLibraryInitialized())
                return;

            GTSLib_SetWorldGravity(gravity);
        }

        /// <summary>
        ///     Gives a script the capability to run without being paused.
        /// </summary>
        /// <param name="toggle"></param>
        public static void SetScriptCanBePaused(bool toggle)
        {
            if (!GTSLib_IsLibraryInitialized()) return;
            GTSLib_SetScriptCanBePaused(Function.Call<string>(Hash.GET_THIS_SCRIPT_NAME), toggle);
        }

        /// <summary>
        ///     Set the specified vehicle's gravity level.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="gravity"></param>
        public static void SetVehicleGravity(Vehicle vehicle, float gravity)
        {
            if (!GTSLib_IsLibraryInitialized())
                return;
            if (vehicle == null)
                return;
            GTSLib_SetVehicleGravity(vehicle.Handle, gravity);
        }

        /// <summary>
        ///     Reset the vehicle's gravity level to the game default: 9.8000002f.
        /// </summary>
        /// <param name="vehicle"></param>
        public static void ResetVehicleGravity(Vehicle vehicle)
        {
            if (!GTSLib_IsLibraryInitialized())
                return;
            const float defGravity = 9.8000002f;
            if (vehicle == null)
                return;
            SetVehicleGravity(vehicle, defGravity);
        }

        /// <summary>
        ///     Get the stack size of the given script.
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        public static uint GetScriptStackSize(string script)
        {
            if (!GTSLib_IsLibraryInitialized() || string.IsNullOrEmpty(script))
                return 0;

            if (ScriptStackSizes.ContainsKey(script))
                return ScriptStackSizes[script];

            var stackSize = GTSLib_GetScriptAllocatedStackSize(script);
            ScriptStackSizes.Add(script, stackSize);
            return stackSize;
        }

        /// <summary>
        ///     Get a value indicating whether or not the rockstar video editor is active.
        /// </summary>
        /// <returns></returns>
        public static bool IsRockstarEditorActive()
        {
            return GTSLib_IsLibraryInitialized() && GTSLib_IsRockstarEditorActive() == 1;
        }

        /// <summary>
        ///     Remove the in-game water.
        /// </summary>
        public static void RemoveWater()
        {
            if (!GTSLib_IsLibraryInitialized()) return;
            GTSLib_RemoveWater();
        }

        /// <summary>
        ///     Restore the in-game water.
        /// </summary>
        public static void RestoreWater()
        {
            if (!GTSLib_IsLibraryInitialized()) return;
            GTSLib_RestoreWater();
        }
    }
}