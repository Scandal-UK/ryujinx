using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Cpu.Nce
{
    static class NceThreadPalAndroid
    {
        [DllImport("libc", SetLastError = true)]
        private static extern int pthread_kill(IntPtr thread, int sig);

        public static void SuspendThread(IntPtr handle)
        {
            int result = pthread_kill(handle, NceThreadPal.UnixSuspendSignal);
            if (result != 0)
            {
                throw new Exception($"Thread kill returned error 0x{result:X}.");
            }
        }
    }
}