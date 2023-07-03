using ARMeilleure.Memory;
using Ryujinx.Memory;

namespace Ryujinx.Cpu.Nce
{
    class NceMemoryAllocator : IJitMemoryAllocator
    {
        public IJitMemoryBlock Allocate(ulong size) => new NceMemoryBlock(size, MemoryAllocationFlags.None);
        public IJitMemoryBlock Reserve(ulong size) => new NceMemoryBlock(size, MemoryAllocationFlags.Reserve);

        public ulong GetPageSize() => MemoryBlock.GetPageSize();
    }
}
