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

        const string path = "./scripts/SpaceMod.log";

        public static void ClearLog()
        {
            if (File.Exists(path))
            {
                File.WriteAllText(path, string.Empty);
            }
        }

		public static void Log(object message, DebugMessageType type = DebugMessageType.Debug)
		{
			var originalText = File.Exists(path) ? File.ReadAllText(path) : string.Empty;
			File.WriteAllText(path, $"{(originalText != string.Empty ? originalText + Environment.NewLine : string.Empty)}" +
									$"[{(type == DebugMessageType.Debug ? "DEBUG" : "ERROR")}] " +
									$"[{DateTime.Now.Hour:00}:{DateTime.Now.Minute:00}:{DateTime.Now.Second:00}] {message}");

			if (type == DebugMessageType.Error)
			{
				UI.Notify("An error has occured. You can find more information in SpaceMod.log");
			}
		}

		public static void LogEntityData(Entity entity)
		{
			Log($"Logging entity data:{Environment.NewLine}" +
				$"\tPosition: {entity.Position}{Environment.NewLine}" +
				$"\tHeading: {entity.Heading}{Environment.NewLine}" +
				$"\tRotation: {entity.Rotation}{Environment.NewLine}" +
				$"\tQuaternion: {entity.Quaternion}{Environment.NewLine}" +
				$"\tHash: {entity.Model.Hash}{Environment.NewLine}");

			UI.Notify("Logged entity data.");
		}

	}
}
