﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;

namespace SpaceMod.Lib
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

    public enum ScreenEffect
    {
        SwitchHudIn,
        SwitchHudOut,
        FocusIn,
        FocusOut,
        MinigameEndNeutral,
        MinigameEndTrevor,
        MinigameEndFranklin,
        MinigameEndMichael,
        MinigameTransitionOut,
        MinigameTransitionIn,
        SwitchShortNeutralIn,
        SwitchShortFranklinIn,
        SwitchShortTrevorIn,
        SwitchShortMichaelIn,
        SwitchOpenMichaelIn,
        SwitchOpenFranklinIn,
        SwitchOpenTrevorIn,
        SwitchHudMichaelOut,
        SwitchHudFranklinOut,
        SwitchHudTrevorOut,
        SwitchShortFranklinMid,
        SwitchShortMichaelMid,
        SwitchShortTrevorMid,
        DeathFailOut,
        CamPushInNeutral,
        CamPushInFranklin,
        CamPushInMichael,
        CamPushInTrevor,
        SwitchSceneFranklin,
        SwitchSceneTrevor,
        SwitchSceneMichael,
        SwitchSceneNeutral,
        MpCelebWin,
        MpCelebWinOut,
        MpCelebLose,
        MpCelebLoseOut,
        DeathFailNeutralIn,
        DeathFailMpDark,
        DeathFailMpIn,
        MpCelebPreloadFade,
        PeyoteEndOut,
        PeyoteEndIn,
        PeyoteIn,
        PeyoteOut,
        MpRaceCrash,
        SuccessFranklin,
        SuccessTrevor,
        SuccessMichael,
        DrugsMichaelAliensFightIn,
        DrugsMichaelAliensFight,
        DrugsMichaelAliensFightOut,
        DrugsTrevorClownsFightIn,
        DrugsTrevorClownsFight,
        DrugsTrevorClownsFightOut,
        HeistCelebPass,
        HeistCelebPassBw,
        HeistCelebEnd,
        HeistCelebToast,
        MenuMgHeistIn,
        MenuMgTournamentIn,
        MenuMgSelectionIn,
        ChopVision,
        DmtFlightIntro,
        DmtFlight,
        DrugsDrivingIn,
        DrugsDrivingOut,
        SwitchOpenNeutralFib5,
        HeistLocate,
        MpJobLoad,
        RaceTurbo,
        MpIntroLogo,
        HeistTripSkipFade,
        MenuMgHeistOut,
        MpCoronaSwitch,
        MenuMgSelectionTint,
        SuccessNeutral,
        ExplosionJosh3,
        SniperOverlay,
        RampageOut,
        Rampage,
        DontTazemeBro,
    }

    public enum CombatAttributes
    {
        CanUseCover = 0,
        CanUseVehicles = 1,
        CanDoDrivebys = 2,
        CanLeaveVehicle = 3,
        CanFightArmedPedsWhenNotArmed = 5,
        CanTauntInVehicle = 20,
        AlwaysFight = 46,
        IgnoreTrafficWhenDriving = 52
    }

    public enum CPlaneMission
    {
        None = 0,
        Unk = 1,
        CTaskVehicleRam = 2,
        CTaskVehicleBlock = 3,
        CTaskVehicleGoToPlane = 4,
        CTaskVehicleStop = 5,
        CTaskVehicleAttack = 6,
        CTaskVehicleFollow = 7,
        CTaskVehicleFleeAirborne = 8,
        CTaskVehicleCircle = 9,
        CTaskVehicleEscort = 10,
        CTaskVehicleFollowRecording = 15,
        CTaskVehiclePoliceBehaviour = 16,
        CTaskVehicleCrash = 17
    }

    public static class SpaceModLib
    {
        public static Quaternion LookRotation(Vector3 forward)
        {
            Vector3 up = Vector3.WorldUp;
            return INTERNAL_CALL_LookRotation(ref forward, ref up);
        }

        // from http://answers.unity3d.com/questions/467614/what-is-the-source-code-of-quaternionlookrotation.html
        private static Quaternion INTERNAL_CALL_LookRotation(ref Vector3 forward, ref Vector3 up)
        {

            forward = Vector3.Normalize(forward);
            Vector3 right = Vector3.Normalize(Vector3.Cross(up, forward));
            up = Vector3.Cross(forward, right);
            var m00 = right.X;
            var m01 = right.Y;
            var m02 = right.Z;
            var m10 = up.X;
            var m11 = up.Y;
            var m12 = up.Z;
            var m20 = forward.X;
            var m21 = forward.Y;
            var m22 = forward.Z;


            float num8 = (m00 + m11) + m22;
            var quaternion = new Quaternion();
            if (num8 > 0f)
            {
                var num = (float)Math.Sqrt(num8 + 1f);
                quaternion.W = num * 0.5f;
                num = 0.5f / num;
                quaternion.X = (m12 - m21) * num;
                quaternion.Y = (m20 - m02) * num;
                quaternion.Z = (m01 - m10) * num;
                return quaternion;
            }
            if ((m00 >= m11) && (m00 >= m22))
            {
                var num7 = (float)Math.Sqrt(((1f + m00) - m11) - m22);
                var num4 = 0.5f / num7;
                quaternion.X = 0.5f * num7;
                quaternion.Y = (m01 + m10) * num4;
                quaternion.Z = (m02 + m20) * num4;
                quaternion.W = (m12 - m21) * num4;
                return quaternion;
            }
            if (m11 > m22)
            {
                var num6 = (float)Math.Sqrt(((1f + m11) - m00) - m22);
                var num3 = 0.5f / num6;
                quaternion.X = (m10 + m01) * num3;
                quaternion.Y = 0.5f * num6;
                quaternion.Z = (m21 + m12) * num3;
                quaternion.W = (m20 - m02) * num3;
                return quaternion;
            }
            var num5 = (float)Math.Sqrt(((1f + m22) - m00) - m11);
            var num2 = 0.5f / num5;
            quaternion.X = (m20 + m02) * num2;
            quaternion.Y = (m21 + m12) * num2;
            quaternion.Z = 0.5f * num5;
            quaternion.W = (m01 - m10) * num2;
            return quaternion;
        }

        public static void ArtificialDamage(Ped ped, Ped target, float damageDistance, float damageMultiplier)
        {
            if (target.IsInvincible) return;
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

        public static Ped CreateAlien(Vector3 position, WeaponHash weaponHash, int accuracy = 50, float heading = 0)
        {
            var ped = World.CreatePed(PedHash.MovAlien01, position, heading);
            ped.Accuracy = 50;
            ped.Weapons.Give(weaponHash, 15, true, true);
            ped.IsPersistent = true;
            ped.RelationshipGroup = SpaceModDatabase.AlienRelationship;
            ped.Voice = "ALIENS";
            ped.Accuracy = 15;
            ped.SetDefaultClothes();
            ped.RelationshipGroup = SpaceModDatabase.AlienRelationship;
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

        public static void RequestScript(string name)
        {
            if (Function.Call<bool>(Hash.DOES_SCRIPT_EXIST, name))
                return;

            Function.Call(Hash.REQUEST_SCRIPT, name);
            var timeout = DateTime.UtcNow + new TimeSpan(0, 0, 5);
            while(DateTime.UtcNow < timeout)
            {
                if (Function.Call<bool>(Hash.HAS_SCRIPT_LOADED, name))
                    break;

                Script.Yield();
            }
        }

        public static bool IsPlayingAnim(this Entity entity, string animDict, string animName)
        {
            return Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, entity, animDict, animName, 3);
        }

        public static void SetAnimSpeed(this Entity entity, string animDict, string animName, float multiplier)
        {
            Function.Call(Hash.SET_ENTITY_ANIM_SPEED, entity, animDict, animName, multiplier);
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
            Function.Call(Hash._SET_TEXT_COMPONENT_FORMAT, "STRING");
            Function.Call(Hash._0x6C188BE134E074AA, helpText);
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

        public static void TaskStartScenarioInPlace(this Ped ped, string scenario)
        {
            Function.Call(Hash.TASK_START_SCENARIO_IN_PLACE, ped, scenario, 0, 0);
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

        internal static void ShowUIPosition(Entity entity, int index, Vector3 position, string pathToFile, string objectName, UIText nameResText)
        {
            if (entity != null)
            {
                if (entity.IsOccluded) return;
                if (!entity.IsOnScreen || !IsOnScreen(entity.Position)) return;
            }
            else
            {
                if (!IsOnScreen(position)) return;
            }

            var point = UI.WorldToScreen(position);
            var filename = Path.Combine(pathToFile, "Reticle.png");
            var size = new Size(125 / 2, 125 / 2);
            var halfWidth = size.Width / 2;
            var halfHeight = size.Height / 2;
            var imageUpperBound = point.Y - halfHeight;
            var textColor = Color.DarkMagenta;
            const int upperTextOffset = 25;

            point = new Point(point.X - halfWidth, imageUpperBound);
            nameResText.Caption = objectName;
            nameResText.Position = new Point(point.X + halfWidth, imageUpperBound - upperTextOffset
                /*we offset the y position so that it sits above the image*/);
            nameResText.Draw();
            nameResText.Color = textColor;

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
            Point worldToScreen = UI.WorldToScreen(vector3);
            if (worldToScreen.X == 0 && worldToScreen.Y == 0)
                return false;

            return true;
        }

        public static void SetGravityLevel(int level)
        {
            Function.Call(Hash.SET_GRAVITY_LEVEL, level);
        }

        public static void Ragdoll(this Ped ped, int duration, RagdollType type)
        {
            Function.Call(Hash.SET_PED_TO_RAGDOLL, ped, duration, 0, (int)type, false, false, false);
        }
        
        internal static void PlaneMission(this Ped pilot, Vehicle plane, Vehicle targetVehicle, Ped targetPed, Vector3 destination, CPlaneMission mission, float physicsSpeed, float p9, float heading, float maxAltitude, float minAltitude)
        {
            /*void TASK_PLANE_MISSION(Ped pilot, Vehicle plane, Vehicle targetVehicle, Ped targetPed, float destinationX, 
			 * float destinationY, float destinationZ, int missionType, float physicsSpeed, float p9, 
			 * float heading, float maxAltitude, float minAltitude)*/

            Function.Call(Hash.TASK_PLANE_MISSION, pilot, plane, targetVehicle, targetPed, destination.X, destination.Y, destination.Z, (int)mission,
                physicsSpeed, p9, heading, maxAltitude, minAltitude);
        }

        public static void SetCombatAttributes(this Ped ped, CombatAttributes attribute, bool enabled)
        {
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, (int)attribute, enabled);
        }

        public static void NotifyWithGXT(string text, bool blinking = false)
        {
            string gxt = Game.GetGXTEntry(text);
            UI.Notify(gxt, blinking);
        }

        public static void ShowSubtitleWithGXT(string text, int time = 7000)
        {
            Function.Call(Hash._SET_TEXT_ENTRY_2, text);
            Function.Call(Hash._DRAW_SUBTITLE_TIMED, time, true);
        }

        public static void DisplayHelpTextWithGXT(string gxtEntry)
        {
            Function.Call(Hash._SET_TEXT_COMPONENT_FORMAT, gxtEntry);
            Function.Call(Hash._DISPLAY_HELP_TEXT_FROM_STRING_LABEL, 0, 0, IsHelpMessageBeingDisplayed() ? 0 : 1, -1);
        }
    }

    public class LoopedPtfx
    {
        /// <summary>
        /// Initialize the class
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="fxName"></param>
        public LoopedPtfx(string assetName, string fxName)
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

    public static class TimeCycleModifier
    {
        public static void Set(string name, float strength)
        {
            Function.Call(Hash.SET_TIMECYCLE_MODIFIER, name);
            Function.Call(Hash.SET_TIMECYCLE_MODIFIER_STRENGTH, strength);
        }

        public static void Clear()
        {
            Function.Call(Hash.CLEAR_TIMECYCLE_MODIFIER);
        }
    }

    /// <summary>
    /// Original source: Guad Maz
    /// </summary>
    public class ScaleFormMessage
    {
        private Scaleform _sc;
        private int _start;
        private int _timer;

        public ScaleFormMessage()
        {

        }

        internal void Load()
        {
            if (_sc != null) return;
            _sc = new Scaleform("MP_BIG_MESSAGE_FREEMODE");
            var timeout = 1000;
            var start = DateTime.Now;
            while (!Function.Call<bool>(Hash.HAS_SCALEFORM_MOVIE_LOADED, _sc.Handle) && DateTime.Now.Subtract(start).TotalMilliseconds < timeout) Script.Yield();
        }

        internal void Dispose()
        {
            Function.Call(Hash.SET_SCALEFORM_MOVIE_AS_NO_LONGER_NEEDED, new OutputArgument(_sc.Handle));
            _sc = null;
        }

        public void SHOW_MISSION_PASSED_MESSAGE(string msg, int time = 5000)
        {
            Load();
            _start = Game.GameTime;
            _sc.CallFunction("SHOW_MISSION_PASSED_MESSAGE", msg, "", 100, true, 0, true);
            _timer = time;
        }

        public void SHOW_SHARD_CENTERED_MP_MESSAGE(string msg, string desc, HudColor textColor, HudColor bgColor, int time = 5000)
        {
            Load();
            _start = Game.GameTime;
            _sc.CallFunction("SHOW_SHARD_CENTERED_MP_MESSAGE", msg, desc, (int)bgColor, (int)textColor);
            _timer = time;
        }

        public void SHOW_SHARD_CREW_RANKUP_MP_MESSAGE(string title, string subtitle, int time = 5000)
        {
            Load();
            _start = Game.GameTime;
            _sc.CallFunction("SHOW_SHARD_CREW_RANKUP_MP_MESSAGE", title, subtitle);
            _timer = time;
        }

        public void SHOW_BIG_MP_MESSAGE(string msg, string subtitle, int rank, int time = 5000)
        {
            Load();
            _start = Game.GameTime;
            _sc.CallFunction("SHOW_BIG_MP_MESSAGE", msg, subtitle, rank, "", "");
            _timer = time;
        }

        public void SHOW_WEAPON_PURCHASED(string msg, string weaponName, WeaponHash weapon, int time = 5000)
        {
            Load();
            _start = Game.GameTime;
            _sc.CallFunction("SHOW_WEAPON_PURCHASED", msg, weaponName, unchecked((int)weapon), "", 100);
            _timer = time;
        }

        public void SHOW_CENTERED_MP_MESSAGE_LARGE(string msg, int time = 5000)
        {
            Load();
            _start = Game.GameTime;
            _sc.CallFunction("SHOW_CENTERED_MP_MESSAGE_LARGE", msg, "test", 100, true, 100);
            _sc.CallFunction("TRANSITION_IN");
            _timer = time;
        }

        public void CALL_FUNCTION(string funcName, params object[] paremeters)
        {
            Load();
            _sc.CallFunction(funcName, paremeters);
        }

        internal void DoTransition()
        {
            if (_sc == null) return;
            _sc.Render2D();
            if (_start != 0 && Game.GameTime - _start > _timer)
            {
                _sc.CallFunction("TRANSITION_OUT");
                _start = 0;
                Dispose();
            }

        }
    }

    public class ScaleFormMessages : Script
    {
        public static ScaleFormMessage Message { get; set; }
        public ScaleFormMessages()
        {
            Message = new ScaleFormMessage();

            Tick += (sender, args) =>
            {
                Message.DoTransition();
            };
        }
    }

    public enum HudColor
    {
        HudColourPureWhite = 0,
        HudColourWhite = 1,
        HudColourBlack = 2,
        HudColourGrey = 3,
        HudColourGreylight = 4,
        HudColourGreydark = 5,
        HudColourRed = 6,
        HudColourRedlight = 7,
        HudColourReddark = 8,
        HudColourBlue = 9,
        HudColourBluelight = 10,
        HudColourBluedark = 11,
        HudColourYellow = 12,
        HudColourYellowlight = 13,
        HudColourYellowdark = 14,
        HudColourOrange = 15,
        HudColourOrangelight = 16,
        HudColourOrangedark = 17,
        HudColourGreen = 18,
        HudColourGreenlight = 19,
        HudColourGreendark = 20,
        HudColourPurple = 21,
        HudColourPurplelight = 22,
        HudColourPurpledark = 23,
        HudColourPink = 24,
        HudColourRadarHealth = 25,
        HudColourRadarArmour = 26,
        HudColourRadarDamage = 27,
        HudColourNetPlayer1 = 28,
        HudColourNetPlayer2 = 29,
        HudColourNetPlayer3 = 30,
        HudColourNetPlayer4 = 31,
        HudColourNetPlayer5 = 32,
        HudColourNetPlayer6 = 33,
        HudColourNetPlayer7 = 34,
        HudColourNetPlayer8 = 35,
        HudColourNetPlayer9 = 36,
        HudColourNetPlayer10 = 37,
        HudColourNetPlayer11 = 38,
        HudColourNetPlayer12 = 39,
        HudColourNetPlayer13 = 40,
        HudColourNetPlayer14 = 41,
        HudColourNetPlayer15 = 42,
        HudColourNetPlayer16 = 43,
        HudColourNetPlayer17 = 44,
        HudColourNetPlayer18 = 45,
        HudColourNetPlayer19 = 46,
        HudColourNetPlayer20 = 47,
        HudColourNetPlayer21 = 48,
        HudColourNetPlayer22 = 49,
        HudColourNetPlayer23 = 50,
        HudColourNetPlayer24 = 51,
        HudColourNetPlayer25 = 52,
        HudColourNetPlayer26 = 53,
        HudColourNetPlayer27 = 54,
        HudColourNetPlayer28 = 55,
        HudColourNetPlayer29 = 56,
        HudColourNetPlayer30 = 57,
        HudColourNetPlayer31 = 58,
        HudColourNetPlayer32 = 59,
        HudColourSimpleblipDefault = 60,
        HudColourMenuBlue = 61,
        HudColourMenuGreyLight = 62,
        HudColourMenuBlueExtraDark = 63,
        HudColourMenuYellow = 64,
        HudColourMenuYellowDark = 65,
        HudColourMenuGreen = 66,
        HudColourMenuGrey = 67,
        HudColourMenuGreyDark = 68,
        HudColourMenuHighlight = 69,
        HudColourMenuStandard = 70,
        HudColourMenuDimmed = 71,
        HudColourMenuExtraDimmed = 72,
        HudColourBriefTitle = 73,
        HudColourMidGreyMp = 74,
        HudColourNetPlayer1Dark = 75,
        HudColourNetPlayer2Dark = 76,
        HudColourNetPlayer3Dark = 77,
        HudColourNetPlayer4Dark = 78,
        HudColourNetPlayer5Dark = 79,
        HudColourNetPlayer6Dark = 80,
        HudColourNetPlayer7Dark = 81,
        HudColourNetPlayer8Dark = 82,
        HudColourNetPlayer9Dark = 83,
        HudColourNetPlayer10Dark = 84,
        HudColourNetPlayer11Dark = 85,
        HudColourNetPlayer12Dark = 86,
        HudColourNetPlayer13Dark = 87,
        HudColourNetPlayer14Dark = 88,
        HudColourNetPlayer15Dark = 89,
        HudColourNetPlayer16Dark = 90,
        HudColourNetPlayer17Dark = 91,
        HudColourNetPlayer18Dark = 92,
        HudColourNetPlayer19Dark = 93,
        HudColourNetPlayer20Dark = 94,
        HudColourNetPlayer21Dark = 95,
        HudColourNetPlayer22Dark = 96,
        HudColourNetPlayer23Dark = 97,
        HudColourNetPlayer24Dark = 98,
        HudColourNetPlayer25Dark = 99,
        HudColourNetPlayer26Dark = 100,
        HudColourNetPlayer27Dark = 101,
        HudColourNetPlayer28Dark = 102,
        HudColourNetPlayer29Dark = 103,
        HudColourNetPlayer30Dark = 104,
        HudColourNetPlayer31Dark = 105,
        HudColourNetPlayer32Dark = 106,
        HudColourBronze = 107,
        HudColourSilver = 108,
        HudColourGold = 109,
        HudColourPlatinum = 110,
        HudColourGang1 = 111,
        HudColourGang2 = 112,
        HudColourGang3 = 113,
        HudColourGang4 = 114,
        HudColourSameCrew = 115,
        HudColourFreemode = 116,
        HudColourPauseBg = 117,
        HudColourFriendly = 118,
        HudColourEnemy = 119,
        HudColourLocation = 120,
        HudColourPickup = 121,
        HudColourPauseSingleplayer = 122,
        HudColourFreemodeDark = 123,
        HudColourInactiveMission = 124,
        HudColourDamage = 125,
        HudColourPinklight = 126,
        HudColourPmMitemHighlight = 127,
        HudColourScriptVariable = 128,
        HudColourYoga = 129,
        HudColourTennis = 130,
        HudColourGolf = 131,
        HudColourShootingRange = 132,
        HudColourFlightSchool = 133,
        HudColourNorthBlue = 134,
        HudColourSocialClub = 135,
        HudColourPlatformBlue = 136,
        HudColourPlatformGreen = 137,
        HudColourPlatformGrey = 138,
        HudColourFacebookBlue = 139,
        HudColourIngameBg = 140,
        HudColourDarts = 141,
        HudColourWaypoint = 142,
        HudColourMichael = 143,
        HudColourFranklin = 144,
        HudColourTrevor = 145,
        HudColourGolfP1 = 146,
        HudColourGolfP2 = 147,
        HudColourGolfP3 = 148,
        HudColourGolfP4 = 149,
        HudColourWaypointlight = 150,
        HudColourWaypointdark = 151,
        HudColourPanelLight = 152,
        HudColourMichaelDark = 153,
        HudColourFranklinDark = 154,
        HudColourTrevorDark = 155,
        HudColourObjectiveRoute = 156,
        HudColourPausemapTint = 157,
        HudColourPauseDeselect = 158,
        HudColourPmWeaponsPurchasable = 159,
        HudColourPmWeaponsLocked = 160,
        HudColourEndScreenBg = 161,
        HudColourChop = 162,
        HudColourPausemapTintHalf = 163,
        HudColourNorthBlueOfficial = 164,
        HudColourScriptVariable2 = 165,
        HudColourH = 166,
        HudColourHdark = 167,
        HudColourT = 168,
        HudColourTdark = 169,
        HudColourHshard = 170,
        HudColourControllerMichael = 171,
        HudColourControllerFranklin = 172,
        HudColourControllerTrevor = 173,
        HudColourControllerChop = 174,
        HudColourVideoEditorVideo = 175,
        HudColourVideoEditorAudio = 176,
        HudColourVideoEditorText = 177,
        HudColourHbBlue = 178,
        HudColourHbYellow = 179,
    }

    public static class Effects
    {
        private static readonly string[] _effects = {
            "SwitchHUDIn",
            "SwitchHUDOut",
            "FocusIn",
            "FocusOut",
            "MinigameEndNeutral",
            "MinigameEndTrevor",
            "MinigameEndFranklin",
            "MinigameEndMichael",
            "MinigameTransitionOut",
            "MinigameTransitionIn",
            "SwitchShortNeutralIn",
            "SwitchShortFranklinIn",
            "SwitchShortTrevorIn",
            "SwitchShortMichaelIn",
            "SwitchOpenMichaelIn",
            "SwitchOpenFranklinIn",
            "SwitchOpenTrevorIn",
            "SwitchHUDMichaelOut",
            "SwitchHUDFranklinOut",
            "SwitchHUDTrevorOut",
            "SwitchShortFranklinMid",
            "SwitchShortMichaelMid",
            "SwitchShortTrevorMid",
            "DeathFailOut",
            "CamPushInNeutral",
            "CamPushInFranklin",
            "CamPushInMichael",
            "CamPushInTrevor",
            "SwitchSceneFranklin",
            "SwitchSceneTrevor",
            "SwitchSceneMichael",
            "SwitchSceneNeutral",
            "MP_Celeb_Win",
            "MP_Celeb_Win_Out",
            "MP_Celeb_Lose",
            "MP_Celeb_Lose_Out",
            "DeathFailNeutralIn",
            "DeathFailMPDark",
            "DeathFailMPIn",
            "MP_Celeb_Preload_Fade",
            "PeyoteEndOut",
            "PeyoteEndIn",
            "PeyoteIn",
            "PeyoteOut",
            "MP_race_crash",
            "SuccessFranklin",
            "SuccessTrevor",
            "SuccessMichael",
            "DrugsMichaelAliensFightIn",
            "DrugsMichaelAliensFight",
            "DrugsMichaelAliensFightOut",
            "DrugsTrevorClownsFightIn",
            "DrugsTrevorClownsFight",
            "DrugsTrevorClownsFightOut",
            "HeistCelebPass",
            "HeistCelebPassBW",
            "HeistCelebEnd",
            "HeistCelebToast",
            "MenuMGHeistIn",
            "MenuMGTournamentIn",
            "MenuMGSelectionIn",
            "ChopVision",
            "DMT_flight_intro",
            "DMT_flight",
            "DrugsDrivingIn",
            "DrugsDrivingOut",
            "SwitchOpenNeutralFIB5",
            "HeistLocate",
            "MP_job_load",
            "RaceTurbo",
            "MP_intro_logo",
            "HeistTripSkipFade",
            "MenuMGHeistOut",
            "MP_corona_switch",
            "MenuMGSelectionTint",
            "SuccessNeutral",
            "ExplosionJosh3",
            "SniperOverlay",
            "RampageOut",
            "Rampage",
            "Dont_tazeme_bro"
        };

        private static string EffectToString(ScreenEffect screenEffect)
        {
            if (screenEffect >= 0 && (int)screenEffect <= _effects.Length)
            {
                return _effects[(int)screenEffect];
            }
            return "INVALID";
        }

        public static void Start(ScreenEffect effectName, int duration = 0, bool looped = false)
        {
            Function.Call(Hash._START_SCREEN_EFFECT, EffectToString(effectName), duration, looped);
        }

        public static void Stop()
        {
            Function.Call(Hash._STOP_ALL_SCREEN_EFFECTS);
        }

        public static void Stop(ScreenEffect screenEffect)
        {
            Function.Call(Hash._STOP_SCREEN_EFFECT, EffectToString(screenEffect));
        }

        public static bool IsActive(ScreenEffect screenEffect)
        {
            return Function.Call<bool>(Hash._GET_SCREEN_EFFECT_IS_ACTIVE, EffectToString(screenEffect));
        }
    }
}
