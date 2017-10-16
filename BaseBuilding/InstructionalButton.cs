using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Native;

namespace BaseBuilding
{
    /// <summary>
    ///     Credits to GuadMaz
    /// </summary>
    public class InstructionalButton
    {
        private static Scaleform _sc;
        private readonly Control _buttonControl;

        private readonly string _buttonString;
        private readonly bool _usingControls;

        /// <summary>
        ///     Add a dynamic button to the instructional buttons array.
        ///     Changes whether the controller is being used and changes depending on keybinds.
        /// </summary>
        /// <param name="control">GTA.Control that gets converted into a button.</param>
        /// <param name="text">Help text that goes with the button.</param>
        public InstructionalButton(Control control, string text)
        {
            Text = text;
            _buttonControl = control;
            _usingControls = true;
        }


        /// <summary>
        ///     Adds a keyboard button to the instructional buttons array.
        /// </summary>
        /// <param name="keystring">Custom keyboard button, like "I", or "O", or "F5".</param>
        /// <param name="text">Help text that goes with the button.</param>
        public InstructionalButton(string keystring, string text)
        {
            Text = text;
            _buttonString = keystring;
            _usingControls = false;
        }

        public string Text { get; set; }


        public string GetButtonId()
        {
            return _usingControls
                ? Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int) _buttonControl, 0)
                : "t_" + _buttonString;
        }

        public void DisableControl(int index)
        {
            Game.DisableControlThisFrame(index, _buttonControl);
        }

        public static void Draw(IReadOnlyCollection<InstructionalButton> buttons)
        {
            if (_sc == null)
                _sc = new Scaleform("instructional_buttons");

            _sc.CallFunction("CLEAR_ALL");
            _sc.CallFunction("TOGGLE_MOUSE_BUTTONS", 0);
            _sc.CallFunction("CREATE_CONTAINER");

            for (var i = 0; i < buttons.Count; i++)
            {
                var b = buttons.ElementAt(i);
                _sc.CallFunction("SET_DATA_SLOT", i,
                    Function.Call<string>(Hash._0x0499D7B09FC9B407, 2, (int) b._buttonControl, 0),
                    b.Text);
            }

            _sc.CallFunction("DRAW_INSTRUCTIONAL_BUTTONS", -1);
            _sc.Render2D();

            StopDraw();
        }

        public static void StopDraw()
        {
            if (_sc == null) return;
            _sc.Dispose();
            _sc = null;
        }
    }
}