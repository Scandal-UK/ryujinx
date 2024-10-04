using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Cpu.Nce
{
    static class NceThreadPalUnix
    {
        [DllImport("libc", SetLastError = true)]
        private static extern IntPtr pthread_self();

        [DllImport("libc", SetLastError = true)]
        private static extern int pthread_threadid_np(IntPtr arg0, out ulong tid);

        [DllImport("libpthread", SetLastError = true)]
        private static extern int pthread_kill(IntPtr thread, int sig);

        public static IntPtr GetCurrentThreadHandle()
        {
            return pthread_self();
        }

        public static ulong GetCurrentThreadId()
        {
            pthread_threadid_np(IntPtr.Zero, out ulong tid);
            return tid;
        }

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