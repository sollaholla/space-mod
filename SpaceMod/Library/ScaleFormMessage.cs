using System;
using GTA;
using GTA.Native;

namespace GTS.Library
{
    /// <summary>
    ///     Original source: Guad Maz
    /// </summary>
    public class ScaleFormMessage
    {
        private Scaleform _sc;
        private int _start;
        private int _timer;

        internal void Load()
        {
            if (_sc != null) return;
            _sc = new Scaleform("MP_BIG_MESSAGE_FREEMODE");
            var timeout = 1000;
            var start = DateTime.Now;
            while (!Function.Call<bool>(Hash.HAS_SCALEFORM_MOVIE_LOADED, _sc.Handle) &&
                   DateTime.Now.Subtract(start).TotalMilliseconds < timeout) Script.Yield();
        }

        internal void Dispose()
        {
            Function.Call(Hash.SET_SCALEFORM_MOVIE_AS_NO_LONGER_NEEDED, new OutputArgument(_sc.Handle));
            _sc = null;
        }

        public void SHOW_MISSION_PASSED_MESSAGE(string msg, int time = 5000)
        {
            Load();
            _start = Game.GameTime;
            _sc.CallFunction("SHOW_MISSION_PASSED_MESSAGE", msg, "", 100, true, 0, true);
            _timer = time;
        }

        public void SHOW_SHARD_CENTERED_MP_MESSAGE(string msg, string desc, HudColor textColor, HudColor bgColor,
            int time = 5000)
        {
            Load();
            _start = Game.GameTime;
            _sc.CallFunction("SHOW_SHARD_CENTERED_MP_MESSAGE", msg, desc, (int) bgColor, (int) textColor);
            _timer = time;
        }

        public void SHOW_SHARD_CREW_RANKUP_MP_MESSAGE(string title, string subtitle, int time = 5000)
        {
            Load();
            _start = Game.GameTime;
            _sc.CallFunction("SHOW_SHARD_CREW_RANKUP_MP_MESSAGE", title, subtitle);
            _timer = time;
        }

        public void SHOW_BIG_MP_MESSAGE(string msg, string subtitle, int rank, int time = 5000)
        {
            Load();
            _start = Game.GameTime;
            _sc.CallFunction("SHOW_BIG_MP_MESSAGE", msg, subtitle, rank, "", "");
            _timer = time;
        }

        public void SHOW_WEAPON_PURCHASED(string msg, string weaponName, WeaponHash weapon, int time = 5000)
        {
            Load();
            _start = Game.GameTime;
            _sc.CallFunction("SHOW_WEAPON_PURCHASED", msg, weaponName, unchecked((int) weapon), "", 100);
            _timer = time;
        }

        public void SHOW_CENTERED_MP_MESSAGE_LARGE(string msg, int time = 5000)
        {
            Load();
            _start = Game.GameTime;
            _sc.CallFunction("SHOW_CENTERED_MP_MESSAGE_LARGE", msg, "test", 100, true, 100);
            _sc.CallFunction("TRANSITION_IN");
            _timer = time;
        }

        public void CALL_FUNCTION(string funcName, params object[] paremeters)
        {
            Load();
            _sc.CallFunction(funcName, paremeters);
        }

        internal void DoTransition()
        {
            if (_sc == null) return;
            _sc.Render2D();
            if (_start != 0 && Game.GameTime - _start > _timer)
            {
                _sc.CallFunction("TRANSITION_OUT");
                _start = 0;
                Dispose();
            }
        }
    }
}