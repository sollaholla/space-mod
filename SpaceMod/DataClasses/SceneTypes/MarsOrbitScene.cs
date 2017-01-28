using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SpaceMod.DataClasses.SceneTypes
{
    public class MarsOrbitScene : Scene
    {
        public static Vector3[] Positions => new[]
        {
            new Vector3(-6870.744f, -12107.31f, 8620.764f), /*Earth*/
            new Vector3(-15370.74f, -12107.31f, 8620.764f) /*Moon*/
        };
    }
}
