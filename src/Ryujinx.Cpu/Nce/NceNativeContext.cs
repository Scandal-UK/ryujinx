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
            public ulong TpidrEl0;
            public ulong TpidrroEl0;
            public ulong CtrEl0;
            public uint Pstate;
            public uint Fpcr;
            public uint Fpsr;
            public uint InManaged;
            public ulong HostSp;
            public IntPtr HostThreadHandle;
            public ulong TempStorage;
            public IntPtr SvcCallHandler;
        }

        private static NativeCtxStorage _dummyStorage = new();

        private readonly MemoryBlock _block;

        public IntPtr BasePtr => _block.Pointer;

        public NceNativeContext()
        {
            _block = new MemoryBlock((ulong)Unsafe.SizeOf<NativeCtxStorage>());
        }

        public static int GetXOffset(int index)
        {
            return StorageOffset(ref _dummyStorage, ref _dummyStorage.X[index]);
        }

        public static int GetGuestSPOffset()
        {
            return StorageOffset(ref _dummyStorage, ref _dummyStorage.X[31]);
        }

        public static int GetVOffset(int index)
        {
            return StorageOffset(ref _dummyStorage, ref _dummyStorage.V[index]);
        }

        public static int GetTpidrEl0Offset()
        {
            return StorageOffset(ref _dummyStorage, ref _dummyStorage.TpidrEl0);
        }

        public static int GetTpidrroEl0Offset()
        {
            return StorageOffset(ref _dummyStorage, ref _dummyStorage.TpidrroEl0);
        }

        public static int GetInManagedOffset()
        {
            return StorageOffset(ref _dummyStorage, ref _dummyStorage.InManaged);
        }

        public static int GetHostSPOffset()
        {
            return StorageOffset(ref _dummyStorage, ref _dummyStorage.HostSp);
        }

        public static int GetCtrEl0Offset()
        {
            return StorageOffset(ref _dummyStorage, ref _dummyStorage.CtrEl0);
        }

        public static int GetTempStorageOffset()
        {
            return StorageOffset(ref _dummyStorage, ref _dummyStorage.TempStorage);
        }

        public static int GetSvcCallHandlerOffset()
        {
            return StorageOffset(ref _dummyStorage, ref _dummyStorage.SvcCallHandler);
        }

        private static int StorageOffset<T>(ref NativeCtxStorage storage, ref T target)
        {
            return (int)Unsafe.ByteOffset(ref Unsafe.As<NativeCtxStorage, T>(ref storage), ref target);
        }

        public unsafe ref NativeCtxStorage GetStorage() => ref Unsafe.AsRef<NativeCtxStorage>((void*)_block.Pointer);

        public void Dispose() => _block.Dispose();
    }
}