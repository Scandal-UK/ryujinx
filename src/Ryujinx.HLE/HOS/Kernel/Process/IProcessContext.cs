using Ryujinx.Cpu;
using Ryujinx.Memory;
using System;

namespace Ryujinx.HLE.HOS.Kernel.Process
{
    interface IProcessContext : IDisposable
    {
        IVirtualMemoryManager AddressSpace { get; }
        ulong ReservedSize { get; }

        ulong AddressSpaceSize { get; }

        IExecutionContext CreateExecutionContext(ExceptionCallbacks exceptionCallbacks);
        void Execute(IExecutionContext context, ulong codeAddress);
        void InvalidateCacheRegion(ulong address, ulong size);
        void PatchCodeForNce(ulong textAddress, ulong textSize, ulong patchRegionAddress, ulong patchRegionSize);
    }
}
