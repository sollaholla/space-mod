using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GTA;
using GTA.Math;
using GTA.Native;
using GTS.Utility;

namespace GTS.Library
{
    public static class GtsLibNet
    {
        private const string AlienModelsTextFile = "./scripts/Space/Aliens.txt";
        private const string DefaultAlienModel = "S_M_M_MovAlien_01";
        private static readonly string[] AlienModels;
        private static readonly string[] StartingIpls;
        private static readonly string[] AllIpls;

        static GtsLibNet()
        {
            AlienModels = new string[0];

            if (File.Exists(AlienModelsTextFile))
            {
                var text = File.ReadAllLines(AlienModelsTextFile).Select(x => x.Trim()).ToArray();
                AlienModels = text;
            }

            AllIpls = GetIplsToLoad()?.ToArray();
            StartingIpls = AllIpls?.Where(x => Function.Call<bool>(Hash.IS_IPL_ACTIVE, x)).ToArray() ?? new string[0];
        }

        public static string GetAlienModel()
        {
            var rand = new Random();
            return AlienModels.Length > 0 ? AlienModels[rand.Next(AlienModels.Length)] : DefaultAlienModel;
        }

        /// <summary>
        ///     Create a ped with alien presets.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="position"></param>
        /// <param name="heading"></param>
        /// <returns>
        /// </returns>
        public static Ped CreateAlien(Model model, Vector3 position, float heading)
        {
            if (model == null)
                model = new Model(GetAlienModel());
            if (!model.IsPed)
                return new Ped(0);
            model.Request();
            while (!model.IsLoaded)
                Script.Yield();
            var p = new Ped(Function.Call<int>(Hash.CREATE_PED, 26, model.Hash, position.X, position.Y, position.Z,
                heading, false, false));
            GivePedAlienAttributes(p);
            model.MarkAsNoLongerNeeded();
            return p;
        }

        public static Quaternion AngleAxis(float angle, Vector3 axis)
        {
            return AngleAxis(angle, ref axis);
        }

        private static Quaternion AngleAxis(float degress, ref Vector3 axis)
        {
            if (Math.Abs(axis.LengthSquared()) < 0.00001)
                return Quaternion.Identity;
            const float degToRad = (float) (Math.PI / 180.0);
            var result = Quaternion.Identity;
            var radians = degress * degToRad;
            radians *= 0.5f;
            axis.Normalize();
            axis = axis * (float) Math.Sin(radians);
            result.X = axis.X;
            result.Y = axis.Y;
            result.Z = axis.Z;
            result.W = (float) Math.Cos(radians);
            return Quaternion.Normalize(result);
        }

        public static void GivePedAlienAttributes(Ped p)
        {
            p.Accuracy = 50;
            p.Voice = "ALIENS";
            p.RelationshipGroup = Database.AlienRelationshipGroup;
            p.SetDefaultClothes();
            p.SetCombatAttributes(CombatAttributes.AlwaysFight, true);
            p.SetCombatAttributes(CombatAttributes.CanFightArmedPedsWhenNotArmed, true);
            Function.Call(Hash.DISABLE_PED_PAIN_AUDIO, p, true);
        }

        public static void TerminateScript(string name)
        {
            GtsLib.GetScriptStackSize(name);
            Function.Call(Hash.TERMINATE_ALL_SCRIPTS_WITH_THIS_NAME, name);
        }

        public static void StartScript(string name, uint stackSize)
        {
            Function.Call(Hash.REQUEST_SCRIPT, name);
            while (!Function.Call<bool>(Hash.HAS_SCRIPT_LOADED, name))
                Script.Yield();
            if (!Function.Call<bool>(Hash.HAS_SCRIPT_LOADED, name)) return;
            Function.Call(Hash.START_NEW_SCRIPT, name, stackSize);
            Function.Call(Hash.SET_SCRIPT_AS_NO_LONGER_NEEDED, name);
        }

        public static bool IsPlayingAnim(this Entity entity, string animDict, string animName)
        {
            return Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, entity, animDict, animName, 3);
        }

        public static void SetAnimSpeed(this Entity entity, string animDict, string animName, float multiplier)
        {
            Function.Call(Hash.SET_ENTITY_ANIM_SPEED, entity, animDict, animName, multiplier);
        }

        public static void AttachTo(this Entity entity1, Entity entity2, Vector3 position = default(Vector3),
            Vector3 rotation = default(Vector3))
        {
            Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY, entity1.Handle, entity2.Handle, 0, position.X, position.Y,
                position.Z, rotation.X, rotation.Y, rotation.Z, 0, 0, 0, 0, 2, 1);
        }

        public static bool IsHelpMessageBeingDisplayed()
        {
            return Function.Call<bool>(Hash.IS_HELP_MESSAGE_BEING_DISPLAYED);
        }

        public static void DisplayHelpTextThisFrame(string helpText, string format = "STRING")
        {
            Function.Call(Hash._SET_TEXT_COMPONENT_FORMAT, format);

            const int maxLen = 99;

            for (var i = 0; i < helpText.Length; i += maxLen)
                Function.Call(Hash._0x6C188BE134E074AA, helpText.Substring(i, Math.Min(maxLen, helpText.Length - i)));

            Function.Call(Hash._DISPLAY_HELP_TEXT_FROM_STRING_LABEL, 0, 0, 1, -1);
        }

        public static Vector3 GetGroundHeightRay(Vector3 position, Entity ignorEntity = null)
        {
            var origin = new Vector3(position.X, position.Y, position.Z + 1000);
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
            var prop = new Prop(Function.Call<int>(Hash.CREATE_OBJECT_NO_OFFSET, model.Hash, position.X, position.Y,
                position.Z, true, true, dynamic));

            return prop;
        }

        public static float GetGroundZ(this Entity entity)
        {
            return GetGroundHeightRay(entity.Position, entity).Z;
        }

        public static void SetGravityLevel(float level)
        {
            GtsLib.SetGravityLevel(level);
        }

        public static void Ragdoll(this Ped ped, int duration, RagdollType type)
        {
            Function.Call(Hash.SET_PED_TO_RAGDOLL, ped, duration, 0, (int) type, false, false, false);
        }

        public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angle)
        {
            var dir = point - pivot;
            dir = Quaternion.Euler(angle) * dir;
            point = dir + pivot;
            return point;
        }

        public static bool IsOnScreen(this Vector3 vector3)
        {
            var worldToScreen = UI.WorldToScreen(vector3);

            if (worldToScreen.X == 0 && worldToScreen.Y == 0)
                return false;

            return true;
        }

        public static void SetCombatAttributes(this Ped ped, CombatAttributes attribute, bool enabled)
        {
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped.Handle, (int) attribute, enabled);
        }

        public static void NotifyWithGxt(string text, bool blinking = false)
        {
            var gxt = Game.GetGXTEntry(text);
            UI.Notify(gxt, blinking);
        }

        public static void ShowSubtitleWithGxt(string text, int time = 7000)
        {
            Function.Call(Hash._SET_TEXT_ENTRY_2, text);
            Function.Call(Hash._DRAW_SUBTITLE_TIMED, time, true);
        }

        public static void DisplayHelpTextWithGxt(string gxtEntry)
        {
            Function.Call(Hash._SET_TEXT_COMPONENT_FORMAT, gxtEntry);
            Function.Call(Hash._DISPLAY_HELP_TEXT_FROM_STRING_LABEL, 0, 0, 1, -1);
        }

        public static void ToggleAllIplsUnchecked(bool remove)
        {
            var lines = GetIplsToLoad(true);
            foreach (var line in lines)
            {
                if (remove)
                {
                    Function.Call(Hash.REMOVE_IPL, line);
                    continue;
                }

                Function.Call(Hash.REQUEST_IPL, line);
            }
        }

        public static void ToggleAllIpls(bool remove)
        {
            var lines = AllIpls ?? new string[0];
            foreach (var line in lines)
            {
                if (remove)
                {
                    Function.Call(Hash.REMOVE_IPL, line);
                    continue;
                }

                if (StartingIpls == null || StartingIpls.Contains(line))
                    Function.Call(Hash.REQUEST_IPL, line);
            }
        }

        private static IEnumerable<string> GetIplsToLoad(bool disregardCurrentIpls = false)
        {
            if (!disregardCurrentIpls)
                if (AllIpls != null)
                    return AllIpls;
            var codebase = Assembly.GetExecutingAssembly().CodeBase;
            var path = Path.GetDirectoryName(new Uri(codebase).LocalPath);
            if (string.IsNullOrEmpty(path)) return new string[0];
            var fileName = Path.Combine(path, "Space\\GameIpls.txt");
            if (!File.Exists(fileName)) return new string[0];
            var lines = File.ReadAllLines(fileName);
            return lines;
        }

        public static Model RequestModel(string modelName)
        {
            var model = new Model(modelName);
            model.Request();
            var timout = DateTime.UtcNow + new TimeSpan(0, 0, 0, 7);
            while (!model.IsLoaded)
            {
                Script.Yield();
                if (DateTime.UtcNow > timout)
                    break;
            }

            return model;
        }
    }
}