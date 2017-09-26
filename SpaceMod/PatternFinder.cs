using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace GTS
{
    /// <summary>
    ///     Credits to CamxxCore. IQ 195
    /// </summary>
    internal static class MemoryAccess
    {
        public sealed class Pattern
        {
            private readonly string _bytes, _mask;
            private readonly IntPtr _result;

            public Pattern(string bytes, string mask, string moduleName = null)
            {
                _bytes = bytes;
                _mask = mask;
                _result = FindPattern(moduleName);
            }

            private unsafe IntPtr FindPattern(string moduleName)
            {
                Win32Native.GetModuleInformation(
                    Win32Native.GetCurrentProcess(),
                    Win32Native.GetModuleHandle(moduleName),
                    out Win32Native.MODULEINFO module,
                    sizeof(Win32Native.MODULEINFO));

                var address = module.lpBaseOfDll.ToInt64();
                var end = address + module.SizeOfImage;

                for (; address < end; address++)
                    if (BCompare((byte*) address, _bytes.ToCharArray(), _mask.ToCharArray()))
                        return new IntPtr(address);

                return IntPtr.Zero;
            }

            public IntPtr Get(int offset = 0)
            {
                return _result + offset;
            }

            private static unsafe bool BCompare(byte* pData, IEnumerable<char> bMask, IReadOnlyList<char> szMask)
            {
                return !bMask.Where((t, i) => szMask[i] == 'x' && pData[i] != t).Any();
            }
        }

        public static class Win32Native
        {
            [DllImport("kernel32.dll")]
            public static extern IntPtr GetCurrentProcess();

            [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
            public static extern IntPtr GetModuleHandle(string lpModuleName);

            [DllImport("psapi.dll", SetLastError = true)]
            public static extern bool GetModuleInformation(IntPtr hProcess, IntPtr hModule, out MODULEINFO lpmodinfo,
                int cb);

            [StructLayout(LayoutKind.Sequential)]
            // ReSharper disable once InconsistentNaming
            public struct MODULEINFO
            {
                public IntPtr lpBaseOfDll;
                public uint SizeOfImage;
                public IntPtr EntryPoint;
            }
        }
    }
}