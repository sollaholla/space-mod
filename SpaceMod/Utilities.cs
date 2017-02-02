using System;
using System.Drawing;
using System.IO;
using GTA;
using GTA.Math;
using GTA.Native;

namespace SpaceMod
{
    public static class Utilities
    {
        public static readonly Random Random = new Random();

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

        public static Vector3 MoveToGroundArtificial(this Vector3 v3, Entity ignorEntity = null)
        {
            var origin = new Vector3(v3.X, v3.Y, v3.Z + 1000);
            var direction = Vector3.WorldDown;
            var ray = World.Raycast(origin, direction, int.MaxValue, IntersectOptions.Everything, ignorEntity);
            return ray.HitCoords;
        }

        public static float GetHeightArtificial(this Entity entity)
        {
            return Vector3.Distance(entity.Position - entity.UpVector, entity.Position.MoveToGroundArtificial(entity));
        }

        public static void ShowUIPosition(Entity entity, int index, Vector3 position, string pathToFile, string objectName,
            UIText nameResText, UIText distanceResText)
        {
            if (entity != null)
            {
                if (entity.IsOccluded) return;
                if (!entity.IsOnScreen) return;
            }

            var point = UI.WorldToScreen(position);
            var filename = Path.Combine(pathToFile, "Reticle.png");
            var size = new Size(125 / 2, 125 / 2);
            var halfWidth = size.Width / 2;
            var halfHeight = size.Height / 2;
            var imageUpperBound = point.Y - halfHeight;
            var imageLowerBount = point.Y + halfHeight;
            var textColor = Color.DarkMagenta;
            const int upperTextOffset = 25;

            point = new Point(point.X - halfWidth, imageUpperBound);
            nameResText.Caption = objectName;
            nameResText.Position = new Point(point.X + halfWidth, imageUpperBound - upperTextOffset
                /*we offset the y position so that it sits above the image*/);
            nameResText.Draw();
            nameResText.Color = textColor;

            var characterPosition = Game.Player.Character.Position;
            distanceResText.Caption = $"{Math.Round(position.DistanceTo(characterPosition), MidpointRounding.AwayFromZero)} M";
            distanceResText.Position = new Point(point.X + halfWidth, imageLowerBount);
            distanceResText.Draw();
            //distanceResText.Color = textColor;

            if (File.Exists(filename))
                UI.DrawTexture(filename, index, 1, 60, point, size);
        }

        public static void SetSuperJumpThisFrame(this Ped ped, float jumpForce, float rollHeight, bool useRoll = true)
        {
            ped.CanRagdoll = false;
            
            if (ped.IsJumping && !ped.IsInAir)
                ped.ApplyForce((ped.UpVector + ped.ForwardVector) * jumpForce);

            if (useRoll)
            {
                if (!ped.IsFalling || !(ped.GetHeightArtificial() < rollHeight)) return;
                ped.Task.ClearAll();
                ped.Task.PlayAnimation("skydive@parachute@", "land_roll", 8.0f, -1.0f, 500, AnimationFlags.None, 0.0f);
            }

            ped.CanRagdoll = true;
        }

        public static bool IsOnScreen(this Vector3 vector3)
        {
            var camPos = GameplayCamera.Position;
            var camDir = GameplayCamera.Direction;
            var fov = GameplayCamera.FieldOfView;
            var dir = vector3 - camPos;
            var angle = Vector3.Angle(dir, camDir);
            var inField = angle < fov;

            return inField;
        }
    }
}
