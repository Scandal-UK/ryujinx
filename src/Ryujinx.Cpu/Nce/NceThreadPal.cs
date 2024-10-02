using System;

namespace Ryujinx.Cpu.Nce
{
    static class NceThreadPal
    {
        private const int SigUsr2Linux = 12;
        private const int SigUsr2MacOS = 31;

        public static int UnixSuspendSignal => OperatingSystem.IsMacOS() ? SigUsr2MacOS : SigUsr2Linux;

        public static IntPtr GetCurrentThreadHandle()
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || Ryujinx.Common.SystemInfo.SystemInfo.IsBionic)
            {
                return NceThreadPalUnix.GetCurrentThreadHandle();
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        public static void SuspendThread(IntPtr handle)
        {
            if (Ryujinx.Common.SystemInfo.SystemInfo.IsBionic)
            {
                NceThreadPalAndroid.SuspendThread(handle);
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                NceThreadPalUnix.SuspendThread(handle);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }
    }
}