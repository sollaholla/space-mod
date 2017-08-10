﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GTA;
using GTS.Library;

namespace GTS
{
    public class RockstarEditorTimecycler
    {
        private int _timecycleModIndex;
        private readonly string[] _timecycleMods;
        private readonly System.Windows.Forms.Timer _timer;

        public RockstarEditorTimecycler()
        {
            const string timecycles = ".\\scripts\\Space\\TimecycleMods.txt";
            if (!File.Exists(timecycles)) return;
            _timecycleMods = new List<string> { "" }.Concat(File.ReadAllLines(timecycles)).ToArray();
            if (_timecycleMods.Length <= 0) return;
            _timer = new System.Windows.Forms.Timer { Interval = 10 };
            _timer.Start();
            _timer.Tick += RockstarEditorTick;
        }

        private void RockstarEditorTick(object o, EventArgs eventArgs)
        {
            if (_timecycleMods == null)
            {
                _timer.Stop();
                return;
            }

            if (!GtsLib.IsRockstarEditorActive()) return;
            TimeCycleModifier.Set(_timecycleMods[_timecycleModIndex], 1.0f);
            if (!Game.IsControlJustPressed(2, Control.SpecialAbilitySecondary)) return;
            _timecycleModIndex = (_timecycleModIndex + 1) % _timecycleMods.Length;
        }
    }
}
