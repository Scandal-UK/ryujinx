using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.Cpu;
using Ryujinx.Cpu.AppleHv;
using Ryujinx.Cpu.Jit;
using Ryujinx.Cpu.LightningJit;
using Ryujinx.Cpu.Nce;
using Ryujinx.Graphics.Gpu;
using Ryujinx.HLE.HOS.Kernel;
using Ryujinx.HLE.HOS.Kernel.Process;
using Ryujinx.Memory;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS
{
    class ArmProcessContextFactory : IProcessContextFactory
    {
        private readonly ITickSource _tickSource;
        private readonly GpuContext _gpu;
        private readonly string _titleIdText;
        private readonly string _displayVersion;
        private readonly bool _diskCacheEnabled;
        private readonly ulong _codeAddress;
        private readonly ulong _codeSize;

        public IDiskCacheLoadState DiskCacheLoadState { get; private set; }

        public ArmProcessContextFactory(
            ITickSource tickSource,
            GpuContext gpu,
            string titleIdText,
            string displayVersion,
            bool diskCacheEnabled,
            ulong codeAddress,
            ulong codeSize)
        {
            _tickSource = tickSource;
            _gpu = gpu;
            _titleIdText = titleIdText;
            _displayVersion = displayVersion;
            _diskCacheEnabled = diskCacheEnabled;
            _codeAddress = codeAddress;
            _codeSize = codeSize;
        }

        public static NceCpuCodePatch CreateCodePatchForNce(KernelContext context, bool for64Bit, ReadOnlySpan<byte> textSection)
        {
            if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64 && for64Bit && context.Device.Configuration.UseHypervisor && !OperatingSystem.IsMacOS())
            {
                return NcePatcher.CreatePatch(textSection);
            }

            return null;
        }

        public IProcessContext Create(KernelContext context, ulong pid, ulong addressSpaceSize, InvalidAccessHandler invalidAccessHandler, bool for64Bit)
        {
            IArmProcessContext processContext;

            bool isArm64Host = RuntimeInformation.ProcessArchitecture == Architecture.Arm64;

            if (isArm64Host && for64Bit && context.Device.Configuration.UseHypervisor)
            {
                if (OperatingSystem.IsMacOS())
                {
                    var cpuEngine = new HvEngine(_tickSource);
                    var memoryManager = new HvMemoryManager(context.Memory, addressSpaceSize, invalidAccessHandler);
                    processContext = new ArmProcessContext<HvMemoryManager>(pid, cpuEngine, _gpu, memoryManager, addressSpaceSize, for64Bit);
                }
                else
                {
                    if (!AddressSpace.TryCreateWithoutMirror(addressSpaceSize, out var addressSpace))
                    {
                        throw new Exception("Address space creation failed");
                    }

                    Logger.Info?.Print(LogClass.Cpu, $"NCE Base AS Address: 0x{addressSpace.Pointer.ToInt64():X} Size: 0x{addressSpace.Size:X}");

                    var cpuEngine = new NceEngine(_tickSource);
                    var memoryManager = new MemoryManagerNative(addressSpace, context.Memory, addressSpaceSize, invalidAccessHandler);
                    processContext = new ArmProcessContext<MemoryManagerNative>(pid, cpuEngine, _gpu, memoryManager, addressSpace.Size, for64Bit, memoryManager.ReservedSize);
                }
            }
            else
            {
                MemoryManagerMode mode = context.Device.Configuration.MemoryManagerMode;

                if (!MemoryBlock.SupportsFlags(MemoryAllocationFlags.ViewCompatible))
                {
                    Logger.Warning?.Print(LogClass.Cpu, "Host system doesn't support views, falling back to software page table");

                    mode = MemoryManagerMode.SoftwarePageTable;
                }

                ICpuEngine cpuEngine = isArm64Host && (mode == MemoryManagerMode.HostMapped || mode == MemoryManagerMode.HostMappedUnsafe)
                    ? new LightningJitEngine(_tickSource)
                    : new JitEngine(_tickSource);

                AddressSpace addressSpace = null;
                MemoryBlock asNoMirror = null;

                // We want to use host tracked mode if the host page size is > 4KB.
                if ((mode == MemoryManagerMode.HostMapped || mode == MemoryManagerMode.HostMappedUnsafe) && MemoryBlock.GetPageSize() <= 0x1000)
                {
                    if (!AddressSpace.TryCreate(context.Memory, addressSpaceSize, out addressSpace) &&
                        !AddressSpace.TryCreateWithoutMirror(addressSpaceSize, out asNoMirror))
                    {
                        Logger.Warning?.Print(LogClass.Cpu, "Address space creation failed, falling back to software page table");

                        mode = MemoryManagerMode.SoftwarePageTable;
                    }
                }

                switch (mode)
                {
                    case MemoryManagerMode.SoftwarePageTable:
                        {
                            var mm = new MemoryManager(context.Memory, addressSpaceSize, invalidAccessHandler);
                            processContext = new ArmProcessContext<MemoryManager>(pid, cpuEngine, _gpu, mm, addressSpaceSize, for64Bit);
                        }
                        break;

                    case MemoryManagerMode.HostMapped:
                    case MemoryManagerMode.HostMappedUnsafe:
                        if (addressSpace == null && asNoMirror == null)
                        {
                            var memoryManagerHostTracked = new MemoryManagerHostTracked(context.Memory, addressSpaceSize, mode == MemoryManagerMode.HostMappedUnsafe, invalidAccessHandler);
                            processContext = new ArmProcessContext<MemoryManagerHostTracked>(pid, cpuEngine, _gpu, memoryManagerHostTracked, addressSpaceSize, for64Bit);
                        }
                        else
                        {
                            if (addressSpaceSize != addressSpace.AddressSpaceSize)
                            {
                                Logger.Warning?.Print(LogClass.Emulation, $"Allocated address space (0x{addressSpace.AddressSpaceSize:X}) is smaller than guest application requirements (0x{addressSpaceSize:X})");
                            }

                            bool unsafeMode = mode == MemoryManagerMode.HostMappedUnsafe;

                            if (addressSpace != null)
                            {
                                var mm = new MemoryManagerHostMapped(addressSpace, unsafeMode, invalidAccessHandler);
                                processContext = new ArmProcessContext<MemoryManagerHostMapped>(pid, cpuEngine, _gpu, mm, addressSpace.AddressSpaceSize, for64Bit);
                            }
                            else
                            {
                                var mm = new MemoryManagerHostNoMirror(asNoMirror, context.Memory, unsafeMode, invalidAccessHandler);
                                processContext = new ArmProcessContext<MemoryManagerHostNoMirror>(pid, cpuEngine, _gpu, mm, asNoMirror.Size, for64Bit);
                            }
                        }
                        break;

                    default:
                        throw new InvalidOperationException($"{nameof(mode)} contains an invalid value: {mode}");
                }

                if (addressSpaceSize != processContext.AddressSpaceSize)
                {
                    Logger.Warning?.Print(LogClass.Emulation, $"Allocated address space (0x{processContext.AddressSpaceSize:X}) is smaller than guest application requirements (0x{addressSpaceSize:X})");
                }
            }

            DiskCacheLoadState = processContext.Initialize(_titleIdText, _displayVersion, _diskCacheEnabled, _codeAddress, _codeSize);

            return processContext;
        }
    }
}
