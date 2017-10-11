using System.Collections.Generic;
using GTA;
using GTA.Native;
using GTS.Library.Security;

namespace GTS.Library
{
    /// <summary>
    ///     Full Credits: Unknown Modder (c) 2017
    ///     GTSLib.asi, NoBoundaryLimits.asi, RespawnFix.asi
    /// </summary>
    public static class GtsLib
    {
        private static readonly Dictionary<string, uint> ScriptStackSizes = new Dictionary<string, uint>();

        //private enum EDecorType
        //{
        //    DECOR_TYPE_FLOAT = 1,
        //    DECOR_TYPE_BOOL = 2,
        //    DECOR_TYPE_INT,
        //    DECOR_TYPE_UNK,
        //    DECOR_TYPE_TIME
        //}

        static GtsLib()
        {
            //if (!File.Exists(".\\atmosphere.asi"))
            //    return;
            //if (Function.Call<bool>(Hash.DECOR_EXIST_ON, Game.Player.Character, "enabled") ||
            //    Function.Call<bool>(Hash.DECOR_EXIST_ON, Game.Player.Character, "fileindex") ||
            //    Function.Call<bool>(Hash.DECOR_EXIST_ON, Game.Player.Character, "reload"))
            //    return;

            //unsafe
            //{
            //    var pattern =
            //        new MemoryAccess.Pattern(
            //            "\x40\x53\x48\x83\xEC\x20\x80\x3D\x00\x00\x00\x00\x00\x8B\xDA\x75\x29",
            //            "xxxxxxxx????xxxxx");
            //    var address = pattern.Get().ToInt64();
            //    *(sbyte*)(address + *(int*)(address + 8) + 13) = 0;
            //    Function.Call(Hash.DECOR_REGISTER, "enabled", 3);
            //    Function.Call(Hash.DECOR_REGISTER, "debug", 2);
            //    Function.Call(Hash.DECOR_REGISTER, "reload", 2);
            //    Function.Call(Hash.DECOR_REGISTER, "fileindex", 3);
            //}
        }

        /// <summary>
        ///     Run the custom in-game credits for Grand Theft Space.
        /// </summary>
        public static void InitCredits()
        {
            if (!NativeMethods.GTSLib_IsLibraryInitialized()) return;
            if (!NativeMethods.GTSLib_InitCredits()) return;
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
            if (!NativeMethods.GTSLib_IsLibraryInitialized()) return;
            Function.Call(Hash.SET_CREDITS_ACTIVE, false);
            Function.Call(Hash._0xB51B9AB9EF81868C, true);
            Function.Call(Hash.SET_GAME_PAUSES_FOR_STREAMING, true);
            Function.Call(Hash.DISPLAY_RADAR, true);
            Function.Call(Hash.DISPLAY_HUD, true);
            NativeMethods.GTSLib_EndCredits();
        }

        /// <summary>
        ///     Set the world gravity level to the specified value. Default value: 9.8000002f.
        /// </summary>
        /// <param name="gravity"></param>
        public static void SetGravityLevel(float gravity)
        {
            if (!NativeMethods.GTSLib_IsLibraryInitialized())
                return;

            NativeMethods.GTSLib_SetWorldGravity(gravity);
        }

        /// <summary>
        ///     Gives a script the capability to run without being paused.
        /// </summary>
        /// <param name="toggle"></param>
        public static void SetScriptCanBePaused(bool toggle)
        {
            if (!NativeMethods.GTSLib_IsLibraryInitialized()) return;
            NativeMethods.GTSLib_SetScriptCanBePaused(Function.Call<string>(Hash.GET_THIS_SCRIPT_NAME), toggle);
        }

        /// <summary>
        ///     Set the specified vehicle's gravity level.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="gravity"></param>
        public static void SetVehicleGravity(Vehicle vehicle, float gravity)
        {
            if (!NativeMethods.GTSLib_IsLibraryInitialized())
                return;
            if (vehicle == null)
                return;
            NativeMethods.GTSLib_SetVehicleGravity(vehicle.Handle, gravity);
        }

        /// <summary>
        ///     Reset the vehicle's gravity level to the game default: 9.8000002f.
        /// </summary>
        /// <param name="vehicle"></param>
        public static void ResetVehicleGravity(Vehicle vehicle)
        {
            if (!NativeMethods.GTSLib_IsLibraryInitialized())
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
            if (!NativeMethods.GTSLib_IsLibraryInitialized() || string.IsNullOrEmpty(script))
                return 0;

            if (ScriptStackSizes.ContainsKey(script))
                return ScriptStackSizes[script];

            var stackSize = NativeMethods.GTSLib_GetScriptAllocatedStackSize(script);
            ScriptStackSizes.Add(script, stackSize);
            return stackSize;
        }

        /// <summary>
        ///     Get a value indicating whether or not the rockstar video editor is active.
        /// </summary>
        /// <returns></returns>
        public static bool IsRockstarEditorActive()
        {
            return NativeMethods.GTSLib_IsLibraryInitialized() && NativeMethods.GTSLib_IsRockstarEditorActive() == 1;
        }

        public static void DisableAtmosphereScript()
        {
            if (!Function.Call<bool>(Hash.DECOR_GET_BOOL, Game.Player.Character, "enabled")) return;
            Function.Call(Hash.DECOR_SET_BOOL, Game.Player.Character, "enabled", false);
        }

        public static void EnableAtmosphereScript()
        {
            if (Function.Call<bool>(Hash.DECOR_GET_BOOL, Game.Player.Character, "enabled")) return;
            Function.Call(Hash.DECOR_SET_BOOL, Game.Player.Character, "enabled", true);
        }

        public static void SetAirDragMultForVehicle(this Player player, float value)
        {
            if (!NativeMethods.GTSLib_IsLibraryInitialized()) return;
            NativeMethods.GTSLib_SetAirDragMultiplierForPlayersVehicle(value);
        }

        public static void DisableLoadingScreenHandler(bool toggle)
        {
            if (!NativeMethods.GTSLib_IsLibraryInitialized()) return;
            NativeMethods.GTSLib_DisableLoadingScreenHandler(toggle);
        }
    }
}