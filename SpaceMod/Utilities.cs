using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using GTA;
using GTA.Math;
using GTA.Native;

namespace SpaceMod
{
    public static class Mathf
    {
        /// <summary>
        /// Clamp the value "value" between min, and max.
        /// </summary>
        /// <param name="value">The value we wish to clamp.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <returns></returns>
        public static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                value = min;
            }
            else if (value > max)
            {
                value = max;
            }
            return value;
        }

        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }
    }

    public enum RagdollType
    {
        Normal = 0,
        Stiff = 1,
        NarrowLegStumble = 2,
        WideLegStumble = 3
    }

    public static class Utilities
    {
        public static readonly Random Random = new Random();

        public static void ArtificalDamage(Ped ped, Ped target, float damageDistance, float damageMultiplier)
        {
            var impCoords = ped.GetLastWeaponImpactCoords();
            if (impCoords == Vector3.Zero) return;
            var distanceTo = impCoords.DistanceTo(target.Position);
            if (distanceTo < damageDistance)
                target.ApplyDamage((int)(1 / distanceTo * damageMultiplier));
        }

        public static bool IsCloseToAnyEntity(Vector3 position, IReadOnlyCollection<Entity> collection, float distance)
        {
            if (collection == null) return false;
            if (collection.Count <= 0) return false;

            return
                collection.Where(entity1 => entity1 != null)
                    .Any(entity1 => entity1.Position.DistanceTo(position) < distance);
        }

        public static Ped CreateAlien(Vector3 position, WeaponHash weaponHash)
        {
            var ped = World.CreatePed(PedHash.MovAlien01, position);
            ped.Accuracy = 50;
            ped.Weapons.Give(WeaponHash.Railgun, 15, true, true);
            ped.IsPersistent = true;
            ped.RelationshipGroup = Database.AlienRelationship;
            ped.Voice = "ALIENS";
            ped.Accuracy = 15;
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, 46, true);
            Function.Call(Hash.SET_PED_COMBAT_RANGE, ped.Handle, 2);
            ped.IsFireProof = true;
            ped.Money = 0;
            return ped;
        }

        public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angle)
        {
            var dir = point - pivot;
            dir = Quaternion.Euler(angle) * dir;
            point = dir + pivot;
            return point;
        }
        public static void TerminateScriptByName(string name)
        {
            if (!Function.Call<bool>(Hash.DOES_SCRIPT_EXIST, name))
                return;

            Function.Call(Hash.TERMINATE_ALL_SCRIPTS_WITH_THIS_NAME, name);
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
            var ray = World.Raycast(origin, direction, 10000, IntersectOptions.Everything, ignorEntity);
            return ray.HitCoords;
        }

        public static void TaskUseNearestScenarioToCoordWarp(this Ped ped, float radius, int duration)
        {
            Function.Call(Hash.TASK_USE_NEAREST_SCENARIO_TO_COORD_WARP, ped, ped.Position.X, ped.Position.Y,
                ped.Position.Z, radius, duration);
        }

        public static void TaskUseNearestScenarioToCoord(this Ped ped, float radius, int duration)
        {
            Function.Call(Hash.TASK_USE_NEAREST_SCENARIO_TO_COORD, ped.Handle, ped.Position.X, ped.Position.Y,
                ped.Position.Z, radius, duration);
        }

        public static Prop CreatePropNoOffset(Model model, Vector3 position, bool dynamic)
        {
            var prop = new Prop(Function.Call<int>(Hash.CREATE_OBJECT_NO_OFFSET, model.Hash, position.X, position.Y, position.Z, true, true, dynamic));
            return prop;
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
            ApplyJumpForce(ped, jumpForce);
            if (!useRoll) return;
            if (!(ped.HeightAboveGround <= rollHeight) || !ped.IsInAir) return;
            ped.Task.ClearAll();
            ped.Task.PlayAnimation("skydive@parachute@", "land_roll", 8.0f, -1.0f, 500, (AnimationFlags)37,
                0.0f);
        }

        private static void ApplyJumpForce(Ped ped, float jumpForce)
        {
            if (!JumpFlag(ped)) return;
            ped.CanRagdoll = false;
            var direction = ped.UpVector + ped.ForwardVector;
            var force = direction * jumpForce;
            ped.ApplyForce(force);
            ped.CanRagdoll = false;
        }

        private static bool JumpFlag(Ped ped)
        {
            return ped.IsJumping && !ped.IsInAir && ped.IsOnFoot && (ped.IsRunning || ped.IsSprinting || ped.IsWalking) && !ped.IsRagdoll &&
                            !ped.IsGettingUp && !ped.IsGettingIntoAVehicle
                            && !ped.IsInCover() && !ped.IsShooting && !ped.IsFalling && !ped.IsBeingJacked &&
                            !ped.IsBeingStealthKilled && !ped.IsBeingStunned &&
                            !ped.IsInVehicle() && !ped.IsSwimming;
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

        public static void SetGravityLevel(int level)
        {
            Function.Call(Hash.SET_GRAVITY_LEVEL, level);
        }

        public static void Ragdoll(this Ped ped, int duration, RagdollType type)
        {
            Function.Call(Hash.SET_PED_TO_RAGDOLL, ped, duration, 0, (int)type, false, false, false);
        }
    }

    public class LoopedPTFX
    {
        /// <summary>
        /// Initialize the class
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="fxName"></param>
        public LoopedPTFX(string assetName, string fxName)
        {
            Handle = -1;
            AssetName = assetName;
            FXName = fxName;
        }

        public int Handle { get; private set; }
        public string AssetName { get; }
        public string FXName { get; }
        public Color Color {
            set {
                Function.Call(Hash.SET_PARTICLE_FX_LOOPED_COLOUR, Handle, value.R / 255, value.G / 255, value.B / 255, false);
            }
        }
        public int Alpha {
            set { Function.Call(Hash.SET_PARTICLE_FX_LOOPED_ALPHA, Handle, value / 255); }
        }

        /// <summary>
        /// If the particle FX is spawned.
        /// </summary>
        public bool Exists => Handle != -1 && Function.Call<bool>(Hash.DOES_PARTICLE_FX_LOOPED_EXIST, Handle);

        /// <summary>
        /// If the particle FX asset is loaded.
        /// </summary>
        public bool IsLoaded => Function.Call<bool>(Hash.HAS_NAMED_PTFX_ASSET_LOADED, AssetName);

        /// <summary>
        /// Load the particle FX asset
        /// </summary>
        public void Load()
        {
            Function.Call(Hash.REQUEST_NAMED_PTFX_ASSET, AssetName);
            while (!IsLoaded)
                Script.Yield();
        }

        /// <summary>
        /// Start particle FX on the specified entity.
        /// </summary>
        /// <param name="entity">Entity to attach to.</param>
        /// <param name="scale">Scale of the fx.</param>
        /// <param name="offset">Optional offset.</param>
        /// <param name="rotation">Optional rotation.</param>
        /// <param name="bone">Entity bone.</param>
        public void Start(Entity entity, float scale, Vector3 offset, Vector3 rotation, Bone? bone)
        {
            if (Handle != -1) return;

            Function.Call(Hash._SET_PTFX_ASSET_NEXT_CALL, AssetName);

            Handle = bone == null ?
                Function.Call<int>(Hash.START_PARTICLE_FX_LOOPED_ON_ENTITY, FXName,
                entity, offset.X, offset.Y, offset.Z, rotation.X, rotation.Y, rotation.Z, scale, 0, 0, 1) :
                Function.Call<int>(Hash._START_PARTICLE_FX_LOOPED_ON_ENTITY_BONE, FXName,
                entity, offset.X, offset.Y, offset.Z, rotation.X, rotation.Y, rotation.Z, (int)bone, scale, 0, 0, 0);
        }

        /// <summary>
        /// Start particle FX on the specified entity.
        /// </summary>
        /// <param name="entity">Entity to attach to.</param>
        /// <param name="scale">Scale of the fx.</param>
        public void Start(Entity entity, float scale)
        {
            Start(entity, scale, Vector3.Zero, Vector3.Zero, null);
        }

        /// <summary>
        /// Start particle FX at the specified position.
        /// </summary>
        /// <param name="position">Position in world space.</param>
        /// <param name="scale">Scale of the fx.</param>
        /// <param name="rotation">Optional rotation.</param>
        public void Start(Vector3 position, float scale, Vector3 rotation)
        {
            if (Handle != -1) return;

            Function.Call(Hash._SET_PTFX_ASSET_NEXT_CALL, AssetName);

            Handle = Function.Call<int>(Hash.START_PARTICLE_FX_LOOPED_AT_COORD, FXName,
             position.X, position.Y, position.Z, rotation.X, rotation.Y, rotation.Z, scale, 0, 0, 0, 0);
        }

        /// <summary>
        /// Start particle FX at the specified position.
        /// </summary>
        /// <param name="position">Position in world space.</param>
        /// <param name="scale">Scale of the fx.</param>
        public void Start(Vector3 position, float scale)
        {
            Start(position, scale, Vector3.Zero);
        }

        /// <summary>
        /// Remove the particle FX
        /// </summary>
        public void Remove()
        {
            if (Handle == -1) return;

            Function.Call(Hash.REMOVE_PARTICLE_FX, Handle, 0);
            Handle = -1;
        }

        /// <summary>
        /// Remove the particle FX in range
        /// </summary>
        public void Remove(Vector3 position, float radius)
        {
            if (Handle == -1) return;

            Function.Call(Hash.REMOVE_PARTICLE_FX_IN_RANGE, position.X, position.Y, position.Z, radius);
            Handle = -1;
        }

        /// <summary>
        /// Unload the loaded particle FX asset
        /// </summary>
        public void Unload()
        {
            if (IsLoaded)
                Function.Call((Hash)0x5F61EBBE1A00F96D, AssetName);
        }
    }

    public enum FollowCamViewMode
    {
        ThirdPersonNear,
        ThirdPersonMed,
        ThirdPersonFar,
        FirstPerson = 4
    }

    public static class FollowCam
    {
        public static FollowCamViewMode ViewMode {
            get {
                if (IsFollowingVehicle)
                    return (FollowCamViewMode)Function.Call<int>(Hash.GET_FOLLOW_VEHICLE_CAM_VIEW_MODE);
                return (FollowCamViewMode)Function.Call<int>(Hash.GET_FOLLOW_PED_CAM_VIEW_MODE);
            }
            set {
                if (IsFollowingVehicle)
                {
                    Function.Call(Hash.SET_FOLLOW_VEHICLE_CAM_VIEW_MODE, (int)value);
                    return;
                }
                Function.Call(Hash.SET_FOLLOW_PED_CAM_VIEW_MODE, (int)value);
            }
        }

        public static bool IsFollowingVehicle => Function.Call<bool>(Hash.IS_FOLLOW_VEHICLE_CAM_ACTIVE);
        public static bool IsFollowingPed => Function.Call<bool>(Hash.IS_FOLLOW_PED_CAM_ACTIVE);

        public static void DisableFirstPerson()
        {
            Function.Call(ViewMode == FollowCamViewMode.FirstPerson
                ? Hash._DISABLE_FIRST_PERSON_CAM_THIS_FRAME
                : Hash._DISABLE_VEHICLE_FIRST_PERSON_CAM_THIS_FRAME);
        }
    }

}
