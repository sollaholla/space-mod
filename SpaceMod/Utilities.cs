using System;
using GTA;
using GTA.Math;
using GTA.Native;

namespace SpaceMod
{
    public static class Utilities
    {
        public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angle)
        {
            var dir = point - pivot;
            dir = Quaternion.Euler(angle) * dir;
            point = dir + pivot;
            return point;
        }

        public static float VDist(this Vector3 a, Vector3 b)
        {
            return Function.Call<float>(Hash.VDIST, a.X, a.Y, a.Z, b.X, b.Y, b.Z);
        }

        public static void AttachTo(this Entity entity1, Entity entity2, Vector3 position = default(Vector3), Vector3 rotation = default(Vector3))
        {
            Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY, entity1.Handle, entity2.Handle, 0, position.X, position.Y, position.Z, rotation.X, rotation.Y, rotation.Z, 0, 0, 0, 0, 2, 1);
        }

        public static bool IsHelpMessageBeingDisplayed()
        {
            return Function.Call<bool>(Hash.IS_HELP_MESSAGE_BEING_DISPLAYED);
        }

        public static void DisplayHelpTextThisFrame(string helpText)
        {
            Function.Call(Hash._SET_TEXT_COMPONENT_FORMAT, "CELL_EMAIL_BCON");

            const int maxStringLength = 99;

            for (var i = 0; i < helpText.Length; i += maxStringLength)
            {
                Function.Call(Hash._0x6C188BE134E074AA, helpText.Substring(i, Math.Min(maxStringLength, helpText.Length - i)));
            }

            Function.Call(Hash._DISPLAY_HELP_TEXT_FROM_STRING_LABEL, 0, 0, IsHelpMessageBeingDisplayed() ? 0 : 1, -1);
        }

        public static Vector3 MoveToGroundArtificial(this Vector3 v3)
        {
            var origin = new Vector3(v3.X, v3.Y, v3.Z + 1000);
            var direction = Vector3.WorldDown;
            var ray = World.Raycast(origin, direction, int.MaxValue, IntersectOptions.Everything);
            return ray.HitCoords;
        }
    }
}
