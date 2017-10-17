using GTA;

namespace GTS.Utility
{
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
            set => Scaleform?.CallFunction("SET_DATA_SLOT", 0, "b_50", value);
        }
    }
}