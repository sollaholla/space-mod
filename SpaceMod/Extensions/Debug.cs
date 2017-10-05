using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using GTA;
using GTA.Math;
using GTA.Native;
using GTSCommon;

namespace GTS.Extensions
{
    public enum DebugMessageType
    {
        Error,
        Debug
    }

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
            Function.Call(Hash.DRAW_DEBUG_SPHERE, position.X, position.Y, position.Z, radius, color.R, color.G, color.B,
                color.A);
        }

        public static void ClearLog()
        {
            if (File.Exists(Settings.LogPath))
                File.WriteAllText(Settings.LogPath, string.Empty);
        }

        public static void Log(object message, DebugMessageType type = DebugMessageType.Debug)
        {
            var originalText = File.Exists(Settings.LogPath) ? File.ReadAllText(Settings.LogPath) : string.Empty;

            var t = new StackTrace().GetFrame(1).GetMethod().ReflectedType;
            var nmspc = t.Namespace + "." + t.Name;


            File.WriteAllText(Settings.LogPath,
                $"{(originalText != string.Empty ? originalText + Environment.NewLine : string.Empty)}" +
                $"[{(type == DebugMessageType.Debug ? "DEBUG" : "ERROR")}] " +
                $"[{DateTime.Now:MM-dd-yyyy}] [{DateTime.Now:hh:mm:ss}] {nmspc} => {message}");

            if (type == DebugMessageType.Error)
                UI.Notify(Database.NotifyHeader + "An error has occured. You can find more information in Space.log");
        }

        public static void LogEntityData(Entity entity)
        {
            Log($"Logging entity data:{Environment.NewLine}" +
                $"\tGame Position: {entity.Position}{Environment.NewLine}" +
                $"\tSpace Position: {(Core.CurrentScene != null ? Core.CurrentScene.SimulatedPosition : Vector3.Zero)}{Environment.NewLine}" +
                $"\tHeading: {entity.Heading}{Environment.NewLine}" +
                $"\tRotation: {entity.Rotation}{Environment.NewLine}" +
                $"\tQuaternion: {entity.Quaternion}{Environment.NewLine}" +
                $"\tHash: {entity.Model.Hash}{Environment.NewLine}");
            UI.Notify(Database.NotifyHeader + "Logged entity position rotation etc...");
        }
    }
}