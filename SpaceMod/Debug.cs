using System.Drawing;
using GTA.Math;
using GTA.Native;

namespace SpaceMod
{
    public static class Debug
    {
        public static void DrawLine(Vector3 start, Vector3 end, Color color)
        {
            Function.Call(Hash.DRAW_LINE, start.X, start.Y, start.Z, end.X, end.Y, end.Z, color.R, color.G, color.B,
                color.A);
        }

        public static void DrawBox(Vector3 min, Vector3 max, Color color)
        {
            Function.Call(Hash.DRAW_BOX, max.X, max.Y, max.Z, min.X, min.Y, min.Z, color.R, color.G, color.B, color.A);
        }

        public static void DrawSphere(Vector3 position, float radius, Color color)
        {
            Function.Call(Hash.DRAW_DEBUG_SPHERE, position.X, position.Y, position.Z, radius, color.R, color.G, color.B, color.A);
        }
    }
}
