using ARMeilleure.State;
using Ryujinx.Common.Memory;
using Ryujinx.Memory;
using System;
using System.Runtime.CompilerServices;

namespace Ryujinx.Cpu.Nce
{
    class NceNativeContext : IDisposable
    {
        public struct NativeCtxStorage
        {
            public Array32<ulong> X;
            public Array32<V128> V;
            public ulong TpidrEl0; // 0x300
            public ulong TpidrroEl0; // 0x308
            public uint Pstate; // 0x310
            public uint Fpcr; // 0x314
            public uint Fpsr; // 0x318
            public uint InManaged; // 0x31C
            public ulong HostSp; // 0x320
            public IntPtr HostThreadHandle; // 0x328
            public ulong HostX30; // 0x330
            public ulong CtrEl0; // 0x338
            public ulong Reserved340; // 0x340
            public ulong Reserved348; // 0x348
            public IntPtr SvcCallHandler; // 0x350
        }

        private readonly MemoryBlock _block;

        public IntPtr BasePtr => _block.Pointer;

        public NceNativeContext()
        {
            _block = new MemoryBlock((ulong)Unsafe.SizeOf<NativeCtxStorage>());
        }

        public unsafe ref NativeCtxStorage GetStorage() => ref Unsafe.AsRef<NativeCtxStorage>((void*)_block.Pointer);

        public void Dispose() => _block.Dispose();
    }
}