using System.Runtime.InteropServices;

namespace GTS.Library.Security
{
    internal static class NativeMethods
    {
        [DllImport("GTSLib.asi")]
        public static extern bool GTSLib_IsLibraryInitialized();

        [DllImport("GTSLib.asi")]
        public static extern bool GTSLib_InitCredits();

        [DllImport("GTSLib.asi")]
        public static extern void GTSLib_EndCredits();

        [DllImport("GTSLib.asi")]
        public static extern void GTSLib_SetWorldGravity(float gravity);

        [DllImport("GTSLib.asi")]
        public static extern byte GTSLib_IsRockstarEditorActive();

        [DllImport("GTSLib.asi", CharSet = CharSet.Unicode)]
        public static extern void GTSLib_SetScriptCanBePaused(
            //[MarshalAs(UnmanagedType.LPStr)]
            string name,
            bool toggle
        );

        [DllImport("GTSLib.asi", CharSet = CharSet.Unicode)]
        public static extern uint GTSLib_GetScriptAllocatedStackSize(
            //[MarshalAs(UnmanagedType.LPStr)]
            string name
        );

        [DllImport("GTSLib.asi")]
        public static extern void GTSLib_SetVehicleGravity(int vehicle, float gravity);

        [DllImport("GTSLib.asi")]
        public static extern void GTSLib_DisableLoadingScreenHandler(bool toggle);
    }
}
