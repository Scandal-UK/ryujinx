using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Cpu.Nce
{
    static class NceNativeInterface
    {
        private delegate ulong GetTickCounterDelegate();
        private delegate bool SuspendThreadHandlerDelegate();
        private static GetTickCounterDelegate _getTickCounter;
        private static SuspendThreadHandlerDelegate _suspendThreadHandler;
        private static IntPtr _getTickCounterPtr;
        private static IntPtr _suspendThreadHandlerPtr;

        [ThreadStatic]
        private static NceExecutionContext _context;

        [ThreadStatic]
        private static ITickSource _tickSource;

        static NceNativeInterface()
        {
            _getTickCounter = GetTickCounter;
            _suspendThreadHandler = SuspendThreadHandler;
            _getTickCounterPtr = Marshal.GetFunctionPointerForDelegate(_getTickCounter);
            _suspendThreadHandlerPtr = Marshal.GetFunctionPointerForDelegate(_suspendThreadHandler);
        }

        public static void RegisterThread(NceExecutionContext context, ITickSource tickSource)
        {
            _context = context;
            _tickSource = tickSource;
        }

        public static ulong GetTickCounter()
        {
            return _tickSource.Counter;
        }

        public static bool SuspendThreadHandler()
        {
            return _context.OnInterrupt();
        }

        public static IntPtr GetTickCounterAccessFunctionPointer()
        {
            return _getTickCounterPtr;
        }

        public static IntPtr GetSuspendThreadHandlerFunctionPointer()
        {
            return _suspendThreadHandlerPtr;
        }
    }
}