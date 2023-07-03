using ARMeilleure.State;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Cpu.Nce
{
    class NceExecutionContext : IExecutionContext
    {
        private readonly NceNativeContext _context;
        private readonly ExceptionCallbacks _exceptionCallbacks;

        internal IntPtr NativeContextPtr => _context.BasePtr;

        public ulong Pc => 0UL;

        public long TpidrEl0
        {
            get => (long)_context.GetStorage().TpidrEl0;
            set => _context.GetStorage().TpidrEl0 = (ulong)value;
        }

        public long TpidrroEl0
        {
            get => (long)_context.GetStorage().TpidrroEl0;
            set => _context.GetStorage().TpidrroEl0 = (ulong)value;
        }

        public uint Pstate
        {
            get => _context.GetStorage().Pstate;
            set => _context.GetStorage().Pstate = value;
        }

        public uint Fpcr
        {
            get => _context.GetStorage().Fpcr;
            set => _context.GetStorage().Fpcr = value;
        }

        public uint Fpsr
        {
            get => _context.GetStorage().Fpsr;
            set => _context.GetStorage().Fpsr = value;
        }

        public bool IsAarch32
        {
            get => false;
            set
            {
                if (value)
                {
                    throw new NotSupportedException();
                }
            }
        }

        public bool Running { get; private set; }

        private delegate bool SupervisorCallHandler(int imm);
        private SupervisorCallHandler _svcHandler;

        public NceExecutionContext(ExceptionCallbacks exceptionCallbacks)
        {
            _svcHandler = OnSupervisorCall;
            IntPtr svcHandlerPtr = Marshal.GetFunctionPointerForDelegate(_svcHandler);

            _context = new NceNativeContext();

            ref var storage = ref _context.GetStorage();
            storage.SvcCallHandler = svcHandlerPtr;
            storage.InManaged = 1u;
            storage.CtrEl0 = 0x8444c004; // TODO: Get value from host CPU instead of using guest one?

            Running = true;
            _exceptionCallbacks = exceptionCallbacks;
        }

        public ulong GetX(int index) => _context.GetStorage().X[index];
        public void SetX(int index, ulong value) => _context.GetStorage().X[index] = value;

        public V128 GetV(int index) => _context.GetStorage().V[index];
        public void SetV(int index, V128 value) => _context.GetStorage().V[index] = value;

        // TODO
        public bool GetPstateFlag(PState flag) => false;
        public void SetPstateFlag(PState flag, bool value) { }

        // TODO
        public bool GetFPstateFlag(FPState flag) => false;
        public void SetFPstateFlag(FPState flag, bool value) { }

        public void SetStartAddress(ulong address)
        {
            ref var storage = ref _context.GetStorage();
            storage.X[30] = address;
            storage.HostThreadHandle = NceThreadPal.GetCurrentThreadHandle();
        }

        public bool OnSupervisorCall(int imm)
        {
            _exceptionCallbacks.SupervisorCallback?.Invoke(this, 0UL, imm);
            return Running;
        }

        public bool OnInterrupt()
        {
            _exceptionCallbacks.InterruptCallback?.Invoke(this);
            return Running;
        }

        public void RequestInterrupt()
        {
            IntPtr threadHandle = _context.GetStorage().HostThreadHandle;
            if (threadHandle != IntPtr.Zero)
            {
                NceThreadPal.SuspendThread(threadHandle);
            }
        }

        public void StopRunning()
        {
            Running = false;
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}