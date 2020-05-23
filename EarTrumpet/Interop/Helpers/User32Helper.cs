using EarTrumpet.Extensions;
using System;
using System.Diagnostics;

namespace EarTrumpet.Interop.Helpers
{
    class User32Helper
    {
        public static bool IsImmersiveProcess(int processId)
        {
            var processHandle = Kernel32.OpenProcess(Kernel32.ProcessFlags.PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
            if (processHandle == IntPtr.Zero)
            {
                return false;
            }

            try
            {
                return User32.IsImmersiveProcess(processHandle) != 0;
            }
            finally
            {
                Kernel32.CloseHandle(processHandle);
            }
        }

        public static bool Is64BitOperatingSystem()
        {
            if (Environment.Is64BitOperatingSystem)
            {
                return true; // Shortcut for AMD64 machines
            }

            bool is64bit = false;
            if (Environment.OSVersion.IsAtLeast(OSVersions.RS3))
            {
                if (Kernel32.IsWow64Process2(Process.GetCurrentProcess().Handle,
                    out Kernel32.IMAGE_FILE_MACHINE processMachine,
                    out Kernel32.IMAGE_FILE_MACHINE nativeMachine))
                {
                    is64bit =
                        nativeMachine == Kernel32.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_AMD64 ||
                        nativeMachine == Kernel32.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_ARM64;
                }
            }

            return is64bit;
        }
    }


}
