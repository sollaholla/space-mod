using System;
using System.IO;
using GTA;

namespace SpaceMod.DataClasses
{
    public enum MessageType
    {
        Error,
        Debug
    }

    public static class DebugLogger
    {
        public static void Log(string message, MessageType type)
        {
            const string path = "./scripts/SpaceMod.log";
            var originalText = File.Exists(path) ? File.ReadAllText(path) : string.Empty;
            File.WriteAllText(path, $"{(originalText != string.Empty ? originalText + "\n" : string.Empty)}" +
                                    $"[{(type == MessageType.Debug ? "DEBUG" : "ERROR")}] [{DateTime.Now.Hour}:{DateTime.Now.Minute}:{DateTime.Now.Second}] {message}");
        }

        public static void LogEntityData(Entity entity)
        {
            Log("Logging entity data: " +
                $"\nPosition = {entity.Position.X}f, {entity.Position.Y}f, {entity.Position.Z}f" +
                $"\nHeading = {entity.Heading}" +
                $"\nRotation = {entity.Rotation.X}f, {entity.Rotation.Y}f, {entity.Rotation.Z}f" +
                $"\nQuaternion = {entity.Quaternion.X}f, {entity.Quaternion.Y}f, {entity.Quaternion.Z}f {entity.Quaternion.Z}f" +
                $"\nHash = {entity.Model.Hash}", 
                MessageType.Debug);

            UI.Notify("Logged entity data.");
        }
    }
}
