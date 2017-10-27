﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using BaseBuilding.Helpers;
using GTA;
using GTA.Math;
using GTA.Native;

namespace BaseBuilding.ObjectTypes
{
    public class BuildableObject : Entity
    {
        private Vector3 _fbl;
        private Vector3 _fbr;
        private Vector3 _bbl;
        private Vector3 _bbr;
        private Vector3 _ftl;
        private Vector3 _ftr;
        private Vector3 _btl;
        private Vector3 _btr;

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

            // bottom
            _fbl = new Vector3(min.X, max.Y, min.Z);
            _fbr = new Vector3(max.X, max.Y, min.Z);
            _bbl = new Vector3(min.X, min.Y, min.Z);
            _bbr = new Vector3(max.X, min.Y, min.Z);

            // top
            _ftl = new Vector3(min.X, max.Y, max.Z);
            _ftr = new Vector3(max.X, max.Y, max.Z);
            _btl = new Vector3(min.X, min.Y, max.Z);
            _btr = new Vector3(max.X, min.Y, max.Z);
        }

        public void DrawSnapPoints()
        {
            World.DrawMarker(MarkerType.DebugSphere, Position + Quaternion * _ftl, Vector3.Zero, Vector3.Zero,
                new Vector3(.1f, .1f, .1f), Color.Red);
            World.DrawMarker(MarkerType.DebugSphere, Position + Quaternion * _ftr, Vector3.Zero, Vector3.Zero,
                new Vector3(.1f, .1f, .1f), Color.Red);
            World.DrawMarker(MarkerType.DebugSphere, Position + Quaternion * _btl, Vector3.Zero, Vector3.Zero,
                new Vector3(.1f, .1f, .1f), Color.Red);
            World.DrawMarker(MarkerType.DebugSphere, Position + Quaternion * _btr, Vector3.Zero, Vector3.Zero,
                new Vector3(.1f, .1f, .1f), Color.Red);
            World.DrawMarker(MarkerType.DebugSphere, Position + Quaternion * _fbl, Vector3.Zero, Vector3.Zero,
                new Vector3(.1f, .1f, .1f), Color.Red);
            World.DrawMarker(MarkerType.DebugSphere, Position + Quaternion * _fbr, Vector3.Zero, Vector3.Zero,
                new Vector3(.1f, .1f, .1f), Color.Red);
            World.DrawMarker(MarkerType.DebugSphere, Position + Quaternion * _bbl, Vector3.Zero, Vector3.Zero,
                new Vector3(.1f, .1f, .1f), Color.Red);
            World.DrawMarker(MarkerType.DebugSphere, Position + Quaternion * _bbr, Vector3.Zero, Vector3.Zero,
                new Vector3(.1f, .1f, .1f), Color.Red);
        }

        public Vector3 GetClosestSnapPoint(Vector3 coord, out SnapPointDirection dir)
        {
            var cast = new List<Vector3>
            {
                Position + Quaternion * _fbl,
                Position + Quaternion * _fbr,
                Position + Quaternion * _bbl,
                Position + Quaternion * _bbr,
                Position + Quaternion * _ftl,
                Position + Quaternion * _ftr,
                Position + Quaternion * _btl,
                Position + Quaternion * _btr
            }.Select(x => new SpatialPlacement(Vector3.Zero, x)).ToArray();

            var closest = World.GetClosest(coord, cast)?.Position ?? Vector3.Zero;

            dir = SnapPointDirection.FrontTopLeft;

            if (closest == cast[0].Position)
            {
                dir = SnapPointDirection.FrontBottomLeft;
            }
            else if (closest == cast[1].Position)
            {
                dir = SnapPointDirection.FrontBottomRight;
            }
            else if (closest == cast[2].Position)
            {
                dir = SnapPointDirection.BackBottomLeft;
            }
            else if (closest == cast[3].Position)
            {
                dir = SnapPointDirection.BackBottomRight;
            }
            else if (closest == cast[4].Position)
            {
                dir = SnapPointDirection.FrontTopLeft;
            }
            else if (closest == cast[5].Position)
            {
                dir = SnapPointDirection.FrontTopRight;
            }
            else if (closest == cast[6].Position)
            {
                dir = SnapPointDirection.BackTopLeft;
            }
            else if (closest == cast[7].Position)
            {
                dir = SnapPointDirection.BackTopRight;
            }

            return closest;
        }

        public Vector3 GetSnapPointPosition(SnapPointDirection snap)
        {
            switch (snap)
            {
                case SnapPointDirection.FrontTopLeft:
                    return Position + Quaternion * _ftl;
                case SnapPointDirection.FrontBottomLeft:
                    return Position + Quaternion * _fbl;
                case SnapPointDirection.FrontBottomRight:
                    return Position + Quaternion * _fbr;
                case SnapPointDirection.BackBottomLeft:
                    return Position + Quaternion * _bbl;
                case SnapPointDirection.BackBottomRight:
                    return Position + Quaternion * _bbr;
                case SnapPointDirection.FrontTopRight:
                    return Position + Quaternion * _ftr;
                case SnapPointDirection.BackTopLeft:
                    return Position + Quaternion * _btl;
                case SnapPointDirection.BackTopRight:
                    return Position + Quaternion * _btr;
                default:
                    throw new ArgumentOutOfRangeException();
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
            var opposingSnapPoint = SnapPointDirection.FrontTopLeft;

            switch (dir)
            {
                case SnapPointDirection.FrontBottomLeft:
                    opposingSnapPoint = SnapPointDirection.FrontBottomRight;
                    break;
                case SnapPointDirection.FrontBottomRight:
                    opposingSnapPoint = SnapPointDirection.FrontBottomLeft;
                    break;
                case SnapPointDirection.BackBottomLeft:
                    opposingSnapPoint = SnapPointDirection.BackBottomRight;
                    break;
                case SnapPointDirection.BackBottomRight:
                    opposingSnapPoint = SnapPointDirection.BackBottomLeft;
                    break;
                case SnapPointDirection.FrontTopLeft:
                    opposingSnapPoint = SnapPointDirection.FrontTopRight;
                    break;
                case SnapPointDirection.FrontTopRight:
                    opposingSnapPoint = SnapPointDirection.FrontTopLeft;
                    break;
                case SnapPointDirection.BackTopLeft:
                    opposingSnapPoint = SnapPointDirection.BackTopRight;
                    break;
                case SnapPointDirection.BackTopRight:
                    opposingSnapPoint = SnapPointDirection.BackTopLeft;
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