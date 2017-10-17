using GTA;

namespace GTS.Library
{
    public class ScaleFormMessages : Script
    {
        public ScaleFormMessages()
        {
            Message = new ScaleFormMessage();

            Tick += (sender, args) => { Message.DoTransition(); };
        }

        public static ScaleFormMessage Message { get; set; }
    }
}