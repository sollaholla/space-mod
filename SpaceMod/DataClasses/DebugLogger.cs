using System;
using System.IO;
using GTA;
using GTA.Native;

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
            const string path = "./scripts/SpaceModLog.log";
            var originalText = File.Exists(path) ? File.ReadAllText(path) : string.Empty;
            File.WriteAllText(path, $"{(originalText != string.Empty ? originalText + "\n" : string.Empty)}" +
                                    $"[{DateTime.Now}] [{(type == MessageType.Debug ? "DEBUG" : "ERROR")}] {message}");
        }

        public static void LogPedData(Ped ped)
        {
            Log($"\nPosition = {ped.Position}" +
                $"\nHeading = {ped.Heading}" +
                $"\nRotation = {ped.Rotation}" +
                $"\nHash = {ped.Model.Hash}" +
                $"\nModel = {(PedHash)ped.Model.Hash}", 
                MessageType.Debug);

            UI.Notify("Logged ped data.");
        }
    }
}
