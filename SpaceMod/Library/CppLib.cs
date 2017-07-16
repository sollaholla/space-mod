using System.Runtime.InteropServices;
using GTA.Native;

namespace GTS.Library
{
    internal static class GtsLib
    {
        //private static bool initialized = false;

        //[DllImport("GTSLib.dll")]
        //private static extern bool GTSLib_Init();

        [DllImport("GTSLib.asi")]
        private static extern bool GTSLib_InitCredits();

        public static void RollCredits()
        {
            //if (!initialized)
            //{
            //    GTSLib_Init();
            //    initialized = true;
            //}

            if (GTSLib_InitCredits())
            {
                Function.Call(Hash.CLEAR_PRINTS);
                Function.Call(Hash.CLEAR_BRIEF);
                Function.Call(Hash.CLEAR_ALL_HELP_MESSAGES);
                Function.Call(Hash.PLAY_END_CREDITS_MUSIC, true);
                Function.Call(Hash.SET_MOBILE_RADIO_ENABLED_DURING_GAMEPLAY, true);
                Function.Call(Hash.SET_MOBILE_PHONE_RADIO_STATE, true);
                Function.Call(Hash.SET_RADIO_TO_STATION_NAME, "RADIO_01_CLASS_ROCK");
                Function.Call(Hash._0x4E404A9361F75BB2, "RADIO_01_CLASS_ROCK", "END_CREDITS_SAVE_MICHAEL_TREVOR",
                    true); // or END_CREDITS_KILL_TREVOR or END_CREDITS_KILL_MICHAEL
                if (!Function.Call<bool>(Hash.IS_AUDIO_SCENE_ACTIVE, "END_CREDITS_SCENE"))
                    Function.Call(Hash.START_AUDIO_SCENE, "END_CREDITS_SCENE");
                Function.Call(Hash.SET_CREDITS_ACTIVE, true);
                Function.Call(Hash._0xB51B9AB9EF81868C, false);
                Function.Call(Hash.SET_GAME_PAUSES_FOR_STREAMING, false);
                Function.Call(Hash.DISPLAY_RADAR, false);
                Function.Call(Hash.DISPLAY_HUD, false);
                Function.Call(Hash._0x23227DF0B2115469);
            }
        }

        [DllImport("GTSLib.asi")]
        private static extern void GTSLib_EndCredits();

        public static void CutCredits()
        {
            //if (!initialized)
            //{
            //    GTSLib_Init();
            //    initialized = true;
            //}

            Function.Call(Hash.PLAY_END_CREDITS_MUSIC, false);
            Function.Call(Hash.SET_MOBILE_RADIO_ENABLED_DURING_GAMEPLAY, false);
            Function.Call(Hash.SET_MOBILE_PHONE_RADIO_STATE, false);
            Function.Call(Hash.SET_RADIO_TO_STATION_NAME, "OFF");
            if (Function.Call<bool>(Hash.IS_AUDIO_SCENE_ACTIVE, "END_CREDITS_SCENE"))
                Function.Call(Hash.STOP_AUDIO_SCENE, "END_CREDITS_SCENE");
            Function.Call(Hash.SET_CREDITS_ACTIVE, false);
            Function.Call(Hash._0xB51B9AB9EF81868C, true);
            Function.Call(Hash.SET_GAME_PAUSES_FOR_STREAMING, true);
            Function.Call(Hash.DISPLAY_RADAR, true);
            Function.Call(Hash.DISPLAY_HUD, true);
            GTSLib_EndCredits();
        }

        [DllImport("GTSLib.asi")]
        private static extern bool GTSLib_SetWorldGravity(float gravity);

        public static void SetGravityLevel(float gravity)
        {
            //if (!initialized)
            //{
            //    GTSLib_Init();
            //    initialized = true;
            //}

            GTSLib_SetWorldGravity(gravity);
        }
    }
}