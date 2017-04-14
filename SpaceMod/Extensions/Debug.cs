using System;
using System.Drawing;
using System.IO;
using GTA;
using GTA.Math;
using GTA.Native;

namespace SpaceMod.Extensions
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
            Function.Call(Hash.DRAW_DEBUG_SPHERE, position.X, position.Y, position.Z, radius, color.R, color.G, color.B, color.A);
		}
		public static void Log(object message, DebugMessageType type = DebugMessageType.Debug)
		{
			const string path = "./scripts/SpaceMod.log";
			var originalText = File.Exists(path) ? File.ReadAllText(path) : string.Empty;
			File.WriteAllText(path, $"{(originalText != string.Empty ? originalText + "\n" : string.Empty)}" +
									$"[{(type == DebugMessageType.Debug ? "DEBUG" : "ERROR")}] " +
									$"[{DateTime.Now.Hour:00}:{DateTime.Now.Minute:00}:{DateTime.Now.Second:00}] {message}");
		}

		public static void LogEntityData(Entity entity)
		{
			Log("Logging entity data: " +
				$"\nPosition = {entity.Position.X}f, {entity.Position.Y}f, {entity.Position.Z}f" +
				$"\nHeading = {entity.Heading}" +
				$"\nRotation = {entity.Rotation.X}f, {entity.Rotation.Y}f, {entity.Rotation.Z}f" +
				$"\nQuaternion = {entity.Quaternion.X}f, {entity.Quaternion.Y}f, {entity.Quaternion.Z}f {entity.Quaternion.Z}f" +
				$"\nHash = {entity.Model.Hash}");

			UI.Notify("Logged entity data.");
		}

	}
}
