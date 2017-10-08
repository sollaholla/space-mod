using GTA.Native;

namespace GTS.Library
{
    public static class TimecycleModifier
    {
        public static void Set(string name, float strength)
        {
            Function.Call(Hash.SET_TIMECYCLE_MODIFIER, name);
            Function.Call(Hash.SET_TIMECYCLE_MODIFIER_STRENGTH, strength);
        }

        public static void Clear()
        {
            Function.Call(Hash.CLEAR_TIMECYCLE_MODIFIER);
        }
    }
}