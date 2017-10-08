using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GTA;

namespace GTS.Utility
{
    public class LoadScaleformDrawer : Script
    {
        private readonly List<LoadScaleform> _loadScaleforms;

        private readonly object _tickLock = new object();

        public LoadScaleformDrawer()
        {
            _loadScaleforms = new List<LoadScaleform>();
            Tick += OnTick;
            Instance = this;
        }

        public static LoadScaleformDrawer Instance { get; private set; }

        public bool DrawingScaleforms => _loadScaleforms != null && _loadScaleforms.Any(x => x.Draw);

        private void OnTick(object sender, EventArgs e)
        {
            if (!Monitor.TryEnter(_tickLock)) return;

            try
            {
                _loadScaleforms?.ForEach(loadForm =>
                {
                    if (!loadForm.Draw || _loadScaleforms.Any(x => x != loadForm && x.Draw)) return;
                    loadForm.Scaleform.Render2D();
                });
            }
            finally
            {
                Monitor.Exit(_tickLock);
            }
        }

        public LoadScaleform Create(string text = "Loading...")
        {
            var scaleform = new Scaleform("instructional_buttons");
            var loadScaleform = new LoadScaleform(false, text, scaleform);
            loadScaleform.Scaleform.CallFunction("CLEAR_ALL");
            loadScaleform.Scaleform.CallFunction("TOGGLE_MOUSE_BUTTONS", 0);
            loadScaleform.Scaleform.CallFunction("CREATE_CONTAINER");
            loadScaleform.Text = text;
            loadScaleform.Scaleform.CallFunction("DRAW_INSTRUCTIONAL_BUTTONS", -1);
            _loadScaleforms.Add(loadScaleform);
            return loadScaleform;
        }

        public void RemoveLoadScaleform(LoadScaleform scaleform)
        {
            _loadScaleforms.Remove(scaleform);
        }
    }
}