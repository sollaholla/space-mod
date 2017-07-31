using System;
using System.IO;
using System.Windows.Forms;

namespace GTS_Scene_Creator
{
    internal static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length > 0)
            {
                var arg = args[0];
                if (File.Exists(arg) && Path.GetExtension(arg).EndsWith("space"))
                {
                    var f = new SceneEditorForm();
                    f.Deserialize(arg);
                    Application.Run(f);
                    return;
                }
            }

            Application.Run(new SceneEditorForm());
        }
    }
}