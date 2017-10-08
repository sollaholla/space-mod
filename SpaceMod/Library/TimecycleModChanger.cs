using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTS.Utility;
using Control = GTA.Control;

namespace GTS.Library
{
    public class TimecycleModChanger
    {
        private readonly string[] _mods = new string[0];
        private readonly Timer _t;
        private int _timecycleModIndex;

        public TimecycleModChanger()
        {
            _t = new Timer {Interval = 1};
            _t.Start();
            _t.Tick += OnTick;
            if (!File.Exists(GtsSettings.TimecycleModifierPath)) return;
            _mods = new[] {string.Empty}.Concat(File.ReadAllLines(GtsSettings.TimecycleModifierPath)).ToArray();
        }

        private void OnTick(object o, EventArgs eventArgs)
        {
            if (_mods.Length <= 0)
            {
                Stop();
                return;
            }
            if (!GtsLib.IsRockstarEditorActive()) return;
            TimecycleModifier.Set(_mods[_timecycleModIndex], 1.0f);
            if (!Game.IsControlJustPressed(2, Control.SpecialAbilitySecondary)) return;
            _timecycleModIndex = (_timecycleModIndex + 1) % _mods.Length;
        }

        public void Stop()
        {
            _t?.Stop();
            _t?.Dispose();
        }
    }
}