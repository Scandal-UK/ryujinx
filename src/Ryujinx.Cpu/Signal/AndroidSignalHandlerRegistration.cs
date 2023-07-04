using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Cpu.Signal
{
    static class AndroidSignalHandlerRegistration
    {
        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public unsafe struct SigSet
        {
            fixed long sa_mask[16];
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct SigAction
        {
            public int sa_flags;
            public IntPtr sa_handler;
            public SigSet sa_mask;
            public IntPtr sa_restorer;
        }

        private const int SIGSEGV = 11;
        private const int SA_SIGINFO = 0x00000004;

        [DllImport("libc", SetLastError = true)]
        private static extern int sigaction(int signum, ref SigAction sigAction, out SigAction oldAction);

        [DllImport("libc", SetLastError = true)]
        private static extern int sigaction(int signum, IntPtr sigAction, out SigAction oldAction);

        [DllImport("libc", SetLastError = true)]
        private static extern int sigemptyset(ref SigSet set);

        public static SigAction GetSegfaultExceptionHandler()
        {
            int result = sigaction(SIGSEGV, IntPtr.Zero, out SigAction old);

            if (result != 0)
            {
                throw new InvalidOperationException($"Could not get SIGSEGV sigaction. Error: {result}");
            }

            return old;
        }

        public static SigAction RegisterExceptionHandler(IntPtr action, int userSignal = -1)
        {
            SigAction sig = new SigAction
            {
                sa_handler = action,
                sa_flags = SA_SIGINFO
            };

            sigemptyset(ref sig.sa_mask);

            int result = sigaction(SIGSEGV, ref sig, out SigAction old);

            if (result != 0)
            {
                throw new InvalidOperationException($"Could not register SIGSEGV sigaction. Error: {result}");
            }

            if (userSignal != -1)
            {
                result = sigaction(userSignal, ref sig, out SigAction oldu);

                if (oldu.sa_handler != IntPtr.Zero)
                {
                    throw new InvalidOperationException($"SIG{userSignal} is already in use.");
                }

                if (result != 0)
                {
                    throw new InvalidOperationException($"Could not register SIG{userSignal} sigaction. Error: {result}");
                }
            }

            return old;
        }

        public static bool RestoreExceptionHandler(SigAction oldAction)
        {
            return sigaction(SIGSEGV, ref oldAction, out SigAction _) == 0;
        }
    }
}
