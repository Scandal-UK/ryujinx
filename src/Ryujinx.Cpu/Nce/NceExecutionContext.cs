using ARMeilleure.State;
using Ryujinx.Cpu.Signal;
using Ryujinx.Memory;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Ryujinx.Cpu.Nce
{
    class NceExecutionContext : IExecutionContext
    {
        private const ulong AlternateStackSize = 0x4000;

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

        private MemoryBlock _alternateStackMemory;

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

            RegisterAlternateStack();
        }

        public void Exit()
        {
            _context.GetStorage().HostThreadHandle = IntPtr.Zero;

            UnregisterAlternateStack();
        }

        private void RegisterAlternateStack()
        {
            // We need to use an alternate stack to handle the suspend signal,
            // as the guest stack may be in a state that is not suitable for the signal handlers.

            _alternateStackMemory = new MemoryBlock(AlternateStackSize);
            NativeSignalHandler.InstallUnixAlternateStackForCurrentThread(_alternateStackMemory.GetPointer(0UL, AlternateStackSize), AlternateStackSize);
        }

        private void UnregisterAlternateStack()
        {
            NativeSignalHandler.UninstallUnixAlternateStackForCurrentThread();
            _alternateStackMemory.Dispose();
            _alternateStackMemory = null;
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
                // Bit 0 set means that the thread is currently running managed code.
                // Bit 1 set means that an interrupt was requested for the thread.
                // This, we only need to send the suspend signal if the value was 0 (not running managed code,
                // and no interrupt was requested before).

                ref uint inManaged = ref _context.GetStorage().InManaged;
                uint oldValue = Interlocked.Or(ref inManaged, 2);

                if (oldValue == 0)
                {
                    NceThreadPal.SuspendThread(threadHandle);
                }
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
