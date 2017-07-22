using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GTS_Scene_Creator
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length > 0)
            {
                string arg = args[0];
                if (File.Exists(arg) && Path.GetExtension(arg).EndsWith("space"))
                {
                    SceneEditorForm f = new SceneEditorForm();
                    f.Deserialize(arg);
                    Application.Run(f);
                    return;
                }
            }

            Application.Run(new SceneEditorForm());
        }
    }
}
