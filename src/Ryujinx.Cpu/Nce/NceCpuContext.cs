using Ryujinx.Cpu.Signal;
using Ryujinx.Common;
using Ryujinx.Memory;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Ryujinx.Cpu.Nce
{
    class NceCpuContext : ICpuContext
    {
        private static uint[] _getTpidrEl0Code = new uint[]
        {
            GetMrsTpidrEl0(0), // mrs x0, tpidr_el0
            0xd65f03c0u, // ret
        };

        private static uint GetMrsTpidrEl0(uint rd)
        {
            if (OperatingSystem.IsMacOS())
            {
                return 0xd53bd060u | rd; // TPIDRRO
            }
            else
            {
                return 0xd53bd040u | rd; // TPIDR
            }
        }

        readonly struct CodeWriter
        {
            private readonly List<uint> _fullCode;

            public CodeWriter()
            {
                _fullCode = new List<uint>();
            }

            public ulong Write(uint[] code)
            {
                ulong offset = (ulong)_fullCode.Count * sizeof(uint);
                _fullCode.AddRange(code);

                return offset;
            }

            public MemoryBlock CreateMemoryBlock()
            {
                ReadOnlySpan<byte> codeBytes = MemoryMarshal.Cast<uint, byte>(_fullCode.ToArray());

                MemoryBlock codeBlock = new(BitUtils.AlignUp((ulong)codeBytes.Length, 0x1000UL));

                codeBlock.Write(0, codeBytes);
                codeBlock.Reprotect(0, (ulong)codeBytes.Length, MemoryPermission.ReadAndExecute, true);

                return codeBlock;
            }
        }

        private delegate void ThreadStart(IntPtr nativeContextPtr);
        private delegate IntPtr GetTpidrEl0();
        private static MemoryBlock _codeBlock;
        private static ThreadStart _threadStart;
        private static GetTpidrEl0 _getTpidrEl0;

        private readonly ITickSource _tickSource;
        private readonly ICpuMemoryManager _memoryManager;

        static NceCpuContext()
        {
            CodeWriter codeWriter = new();

            uint[] threadStartCode = NcePatcher.GenerateThreadStartCode();
            uint[] ehSuspendCode = NcePatcher.GenerateSuspendExceptionHandler();

            ulong threadStartCodeOffset = codeWriter.Write(threadStartCode);
            ulong getTpidrEl0CodeOffset = codeWriter.Write(_getTpidrEl0Code);
            ulong ehSuspendCodeOffset = codeWriter.Write(ehSuspendCode);

            MemoryBlock codeBlock = null;

            NativeSignalHandler.InitializeSignalHandler((IntPtr oldSignalHandlerSegfaultPtr, IntPtr signalHandlerPtr) =>
            {
                uint[] ehWrapperCode = NcePatcher.GenerateWrapperExceptionHandler(oldSignalHandlerSegfaultPtr, signalHandlerPtr);
                ulong ehWrapperCodeOffset = codeWriter.Write(ehWrapperCode);
                codeBlock = codeWriter.CreateMemoryBlock();
                return codeBlock.GetPointer(ehWrapperCodeOffset, (ulong)ehWrapperCode.Length * sizeof(uint));
            });

            NativeSignalHandler.InstallUnixSignalHandler(NceThreadPal.UnixSuspendSignal, codeBlock.GetPointer(ehSuspendCodeOffset, (ulong)ehSuspendCode.Length * sizeof(uint)));

            _threadStart = Marshal.GetDelegateForFunctionPointer<ThreadStart>(codeBlock.GetPointer(threadStartCodeOffset, (ulong)threadStartCode.Length * sizeof(uint)));
            _getTpidrEl0 = Marshal.GetDelegateForFunctionPointer<GetTpidrEl0>(codeBlock.GetPointer(getTpidrEl0CodeOffset, (ulong)_getTpidrEl0Code.Length * sizeof(uint)));
            _codeBlock = codeBlock;
        }

        public NceCpuContext(ITickSource tickSource, ICpuMemoryManager memory, bool for64Bit)
        {
            _tickSource = tickSource;
            _memoryManager = memory;
        }

        /// <inheritdoc/>
        public IExecutionContext CreateExecutionContext(ExceptionCallbacks exceptionCallbacks)
        {
            return new NceExecutionContext(exceptionCallbacks);
        }

        /// <inheritdoc/>
        public void Execute(IExecutionContext context, ulong address)
        {
            NceExecutionContext nec = (NceExecutionContext)context;
            NceNativeInterface.RegisterThread(nec, _tickSource);
            int tableIndex = NceThreadTable.Register(_getTpidrEl0(), nec.NativeContextPtr);

            nec.SetStartAddress(address);
            _threadStart(nec.NativeContextPtr);
            nec.Exit();

            NceThreadTable.Unregister(tableIndex);
        }

        /// <inheritdoc/>
        public void InvalidateCacheRegion(ulong address, ulong size)
        {
        }

        /// <inheritdoc/>
        public IDiskCacheLoadState LoadDiskCache(string titleIdText, string displayVersion, bool enabled)
        {
            return new DiskCacheLoadState();
        }

        /// <inheritdoc/>
        public void PrepareCodeRange(ulong address, ulong size)
        {
        }

        public void Dispose()
        {
        }
    }
}
