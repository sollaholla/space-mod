using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;

namespace BaseBuilding
{
    public class BuildableObject : Entity
    {
        private Vector3 _snapBack;
        private Vector3 _snapBottom;
        private Vector3 _snapFront;
        private Vector3 _snapLeft;
        private Vector3 _snapRight;
        private Vector3 _snapTop;

        public BuildableObject(int handle) : base(handle)
        {
        }

        public Vector3 RotationOffset { get; set; }

        public void Init()
        {
            GenerateSnapPoints();
        }

        private void GenerateSnapPoints()
        {
            Model.GetDimensions(out Vector3 min, out Vector3 max);
            _snapLeft = new Vector3(min.X, min.Y + max.Y, min.Z);
            _snapRight = new Vector3(max.X, min.Y + max.Y, min.Z);
            _snapFront = new Vector3(min.X + max.X, max.Y, min.Z);
            _snapBack = new Vector3(min.X + max.X, min.Y, min.Z);
            _snapTop = new Vector3(min.X + max.X, min.Y + max.Y, max.Z);
            _snapBottom = new Vector3(min.X + max.X, min.Y + max.Y, min.Z);
        }

        public void DrawSnapPoints()
        {
            World.DrawMarker(MarkerType.DebugSphere, Position + Quaternion * _snapLeft, Vector3.Zero, Vector3.Zero,
                new Vector3(.1f, .1f, .1f), Color.Red);
            World.DrawMarker(MarkerType.DebugSphere, Position + Quaternion * _snapRight, Vector3.Zero, Vector3.Zero,
                new Vector3(.1f, .1f, .1f), Color.Red);
            World.DrawMarker(MarkerType.DebugSphere, Position + Quaternion * _snapFront, Vector3.Zero, Vector3.Zero,
                new Vector3(.1f, .1f, .1f), Color.Red);
            World.DrawMarker(MarkerType.DebugSphere, Position + Quaternion * _snapBack, Vector3.Zero, Vector3.Zero,
                new Vector3(.1f, .1f, .1f), Color.Red);
            World.DrawMarker(MarkerType.DebugSphere, Position + Quaternion * _snapTop, Vector3.Zero, Vector3.Zero,
                new Vector3(.1f, .1f, .1f), Color.Red);
            World.DrawMarker(MarkerType.DebugSphere, Position + Quaternion * _snapBottom, Vector3.Zero, Vector3.Zero,
                new Vector3(.1f, .1f, .1f), Color.Red);
        }

        public Vector3 GetClosestSnapPoint(Vector3 coord, out SnapPointDirection dir)
        {
            var cast = new List<Vector3>
            {
                Position + Quaternion * _snapLeft,
                Position + Quaternion * _snapRight,
                Position + Quaternion * _snapFront,
                Position + Quaternion * _snapBack,
                Position + Quaternion * _snapTop,
                Position + Quaternion * _snapBottom
            }.Select(x => new SpatialPlacement(Vector3.Zero, x)).ToArray();

            var closest = World.GetClosest(coord, cast)?.Position ?? Vector3.Zero;

            dir = SnapPointDirection.Front;

            if (closest == cast[0].Position)
                dir = SnapPointDirection.Left;
            else if (closest == cast[1].Position)
                dir = SnapPointDirection.Right;
            else if (closest == cast[2].Position)
                dir = SnapPointDirection.Front;
            else if (closest == cast[3].Position)
                dir = SnapPointDirection.Back;
            else if (closest == cast[4].Position)
                dir = SnapPointDirection.Up;
            else if (closest == cast[5].Position)
                dir = SnapPointDirection.Down;

            return closest;
        }

        public Vector3 GetSnapPointPosition(SnapPointDirection snap)
        {
            switch (snap)
            {
                case SnapPointDirection.Front:
                    return Position + Quaternion * _snapFront;
                case SnapPointDirection.Back:
                    return Position + Quaternion * _snapBack;
                case SnapPointDirection.Left:
                    return Position + Quaternion * _snapLeft;
                case SnapPointDirection.Right:
                    return Position + Quaternion * _snapRight;
                case SnapPointDirection.Up:
                    return Position + Quaternion * _snapTop;
                case SnapPointDirection.Down:
                    return Position + Quaternion * _snapBottom;
                default:
                    throw new ArgumentOutOfRangeException(nameof(snap), snap, null);
            }
        }

        public static BuildableObject PlaceBuildable(
            string modelName,
            IReadOnlyCollection<BuildableObject> others,
            params string[] ignoreModels)
        {
            var b = CreateBuildable(modelName);
            b.Alpha = 100;
            b.HasCollision = false;

            Game.Player.Character.Weapons.Select(WeaponHash.Unarmed, true);

            var buttons = new List<InstructionalButton>
            {
                new InstructionalButton(Control.Context, string.Empty),
                new InstructionalButton(Control.Cover, "Rotate"),
                new InstructionalButton(Control.Sprint, "Rotation Speed"),
                new InstructionalButton(Control.Aim, "Cancel"),
                new InstructionalButton(Control.LookBehind, "Place")
            };

            while (true)
            {
                DrawButtons(buttons);

                if (Game.IsDisabledControlPressed(2, Control.Aim))
                {
                    b.Delete();
                    return null;
                }

                var ray = World.Raycast(GameplayCamera.Position, GameplayCamera.Direction, 50f,
                    IntersectOptions.Everything, Game.Player.Character);

                var cameraPoint = ray.HitCoords;

                var closest = World.GetClosest(cameraPoint, others.ToArray());

                if (Exists(closest) && ignoreModels != null &&
                    ignoreModels.Any(x => Game.GenerateHash(x) == closest.Model.Hash))
                    closest = null;

                if (!Exists(closest) || closest.Position.DistanceTo(cameraPoint) >
                    closest.Model.GetDimensions().Length())
                {
                    b.PositionNoOffset = cameraPoint;

                    if (Game.IsDisabledControlPressed(2, Control.LookBehind))
                        break;

                    var speed = 25f;

                    if (Game.IsDisabledControlPressed(0, Control.Sprint))
                        speed = 75f;

                    if (Game.IsDisabledControlPressed(0, Control.Cover))
                        b.Rotation += new Vector3(0, 0, 1) * Game.LastFrameTime * speed;
                    else if (Game.IsDisabledControlPressed(2, Control.Context))
                        b.Rotation -= new Vector3(0, 0, 1) * Game.LastFrameTime * speed;
                }
                else if (Exists(closest))
                {
                    closest.DrawSnapPoints();

                    b.DrawSnapPoints();

                    var closestSnapPoint = closest.GetClosestSnapPoint(cameraPoint, out SnapPointDirection dir);

                    var opposingSnapPoint = GetOppositeSnapPoint(dir);

                    if (Game.IsDisabledControlJustPressed(0, Control.Cover))
                        b.RotationOffset += new Vector3(0, 0, 90);
                    else if (Game.IsDisabledControlJustPressed(2, Control.Context))
                        b.RotationOffset -= new Vector3(0, 0, 90);

                    b.Rotation = closest.Rotation + b.RotationOffset;

                    var osPos = b.GetSnapPointPosition(opposingSnapPoint);

                    var offsetToPos = b.Position - osPos;

                    b.Position = closestSnapPoint + offsetToPos;

                    if (Game.IsDisabledControlPressed(2, Control.LookBehind))
                        break;
                }

                Script.Yield();
            }

            if (!Exists(b)) return null;

            b.ResetAlpha();
            b.HasCollision = true;
            return b;
        }

        private static void DrawButtons(IReadOnlyCollection<InstructionalButton> buttons)
        {
            Function.Call(Hash.HIDE_HUD_AND_RADAR_THIS_FRAME);
            InstructionalButton.Draw(buttons);
            foreach (var instructionalButton in buttons)
                instructionalButton.DisableControl(0);
        }

        public static SnapPointDirection GetOppositeSnapPoint(SnapPointDirection dir)
        {
            var opposingSnapPoint = SnapPointDirection.Front;

            switch (dir)
            {
                case SnapPointDirection.Front:
                    opposingSnapPoint = SnapPointDirection.Back;
                    break;
                case SnapPointDirection.Back:
                    opposingSnapPoint = SnapPointDirection.Front;
                    break;
                case SnapPointDirection.Left:
                    opposingSnapPoint = SnapPointDirection.Right;
                    break;
                case SnapPointDirection.Right:
                    opposingSnapPoint = SnapPointDirection.Left;
                    break;
                case SnapPointDirection.Up:
                    opposingSnapPoint = SnapPointDirection.Down;
                    break;
                case SnapPointDirection.Down:
                    opposingSnapPoint = SnapPointDirection.Up;
                    break;
            }

            return opposingSnapPoint;
        }

        public static BuildableObject CreateBuildable(string modelName)
        {
            var model = new Model(modelName);
            model.Request();
            while (!model.IsLoaded)
                Script.Yield();
            var p = World.CreateProp(model,
                Game.Player.Character.Position + Game.Player.Character.ForwardVector * 5, Vector3.Zero, true,
                false);
            p.FreezePosition = true;
            var b = new BuildableObject(p.Handle);
            b.Init();
            model.MarkAsNoLongerNeeded();
            return b;
        }
    }
}