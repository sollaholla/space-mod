using GTA;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace GTS
{
    public class LoadScaleformDrawer : Script
    {
        private readonly List<LoadScaleform> _loadScaleforms;

        private bool _refreshedScaleforms;

        private readonly object _tickLock = new object();

        public LoadScaleformDrawer()
        {
            _loadScaleforms = new List<LoadScaleform>();
            Tick += OnTick;
            Instance = this;
        }

        public static LoadScaleformDrawer Instance { get; private set; }

        public bool DrawingScaleforms => _loadScaleforms != null && _loadScaleforms.Any(x => x.Draw);

        private void OnTick(object sender, System.EventArgs e)
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

        public void RefreshScaleforms()
        {

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

    public class LoadScaleform
    {
        public LoadScaleform(bool draw, string text, Scaleform scaleform)
        {
            Draw = draw;
            Text = text;
            Scaleform = scaleform;
        }

        public bool Draw { get; set; }
        public Scaleform Scaleform { get; set; }

        public string Text
        {
            set
            {
                Scaleform?.CallFunction("SET_DATA_SLOT", 0, "b_50", value);
            }
        }
    }
}
