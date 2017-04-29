using System;
using System.Runtime.InteropServices;
using GTA;
using GTA.Math;

namespace SpaceMod.Lib
{
	internal static class CppLib
	{
		/// <summary>
		/// This must be called exactly once before calling any other CppLib function.
		/// Returns true if the initialization was successful, false otherwise.
		/// It's best to place CppLib.dll where GTA5.exe is located, not in the scripts folder.
		/// </summary>
		/// <returns></returns>
		[DllImport("CppLib.dll")]
		public static extern bool CppLib_Init();

		/// <summary>
		/// Unloads the library.
		/// This might take a few seconds.
		/// </summary>
		[DllImport("CppLib.dll")]
		public static extern void CppLib_Unload();
		
		[DllImport("CppLib.dll")]
		public static extern void CppLib_SetEntityScale(int handle, float scaleX, float scaleY, float scaleZ);

		public static void Init()
		{
			DateTime timeout = DateTime.UtcNow + new TimeSpan(0, 0, 0, 5);

			while (!CppLib_Init())
			{
				Script.Yield();

				if (DateTime.UtcNow > timeout)
				{
					break;
				}
			}
		}

		/// <summary>
		/// Scale the entity. The default scale is 1.0f.
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="scale">The scale.</param>
		public static void Scale(this Entity entity, Vector3 scale)
		{
			Init();

			CppLib_SetEntityScale(entity.Handle, scale.X, scale.Y, scale.Z);
		}
	}
}
