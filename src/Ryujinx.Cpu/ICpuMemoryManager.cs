using ARMeilleure.Memory;
using Ryujinx.Memory;

namespace Ryujinx.Cpu
{
    /// <summary>
    /// CPU memory manager interface.
    /// </summary>
    public interface ICpuMemoryManager : IMemoryManager
    {
        /// <summary>
        /// Reprotects a previously mapped range of virtual memory.
        /// </summary>
        /// <param name="va">Virtual address of the range to be reprotected</param>
        /// <param name="size">Size of the range to be reprotected</param>
        /// <param name="permission">New protection of the memory range</param>
        void Reprotect(ulong va, ulong size, MemoryPermission permission);
    }
}