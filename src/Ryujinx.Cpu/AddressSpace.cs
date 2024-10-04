using Ryujinx.Memory;
using System;

namespace Ryujinx.Cpu
{
    public class AddressSpace : IDisposable
    {
        private const MemoryAllocationFlags AsFlags = MemoryAllocationFlags.Reserve | MemoryAllocationFlags.ViewCompatible;

        private readonly MemoryBlock _backingMemory;

        public MemoryBlock Base { get; }
        public MemoryBlock Mirror { get; }

        public ulong AddressSpaceSize { get; }

        public AddressSpace(MemoryBlock backingMemory, MemoryBlock baseMemory, MemoryBlock mirrorMemory, ulong addressSpaceSize)
        {
            _backingMemory = backingMemory;

            Base = baseMemory;
            Mirror = mirrorMemory;
            AddressSpaceSize = addressSpaceSize;
        }

        public static bool TryCreate(MemoryBlock backingMemory, ulong asSize, out AddressSpace addressSpace)
        {
            addressSpace = null;

            MemoryBlock baseMemory = null;
            MemoryBlock mirrorMemory = null;

            try
            {
                baseMemory = new MemoryBlock(asSize, AsFlags);
                mirrorMemory = new MemoryBlock(asSize, AsFlags);
                addressSpace = new AddressSpace(backingMemory, baseMemory, mirrorMemory, asSize);
            }
            catch (SystemException)
            {
                baseMemory?.Dispose();
                mirrorMemory?.Dispose();
            }

            return addressSpace != null;
        }

        public static bool TryCreateWithoutMirror(ulong asSize, out MemoryBlock addressSpace)
        {
            addressSpace = null;

            ulong minAddressSpaceSize = Math.Min(asSize, 1UL << 36);

            // Attempt to create the address space with expected size or try to reduce it until it succeed.
            for (ulong addressSpaceSize = asSize; addressSpaceSize >= minAddressSpaceSize; addressSpaceSize -= 0x100000000UL)
            {
                try
                {
                    MemoryBlock baseMemory = new MemoryBlock(addressSpaceSize, AsFlags);
                    addressSpace = baseMemory;

                    break;
                }
                catch (SystemException)
                {
                }
            }

            return addressSpace != null;
        }

        public void Map(ulong va, ulong pa, ulong size, MemoryMapFlags flags)
        {
            Base.MapView(_backingMemory, pa, va, size);
            Mirror.MapView(_backingMemory, pa, va, size);
        }

        public void Unmap(ulong va, ulong size)
        {
            Base.UnmapView(_backingMemory, va, size);
            Mirror.UnmapView(_backingMemory, va, size);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            Base.Dispose();
            Mirror.Dispose();
        }
    }
}
