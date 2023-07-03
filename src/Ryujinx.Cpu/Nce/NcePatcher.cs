using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.Memory;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Cpu.Nce
{
    static class NcePatcher
    {
        private struct Context
        {
            public readonly ICpuMemoryManager MemoryManager;
            public ulong PatchRegionAddress;
            public ulong PatchRegionSize;

            public Context(ICpuMemoryManager memoryManager, ulong patchRegionAddress, ulong patchRegionSize)
            {
                MemoryManager = memoryManager;
                PatchRegionAddress = patchRegionAddress;
                PatchRegionSize = patchRegionSize;
            }

            public ulong GetPatchWriteAddress(int length)
            {
                ulong byteLength = (ulong)length * 4;

                if (PatchRegionSize < byteLength)
                {
                    throw new Exception("No enough space for patch.");
                }

                ulong address = PatchRegionAddress;
                PatchRegionAddress += byteLength;
                PatchRegionSize -= byteLength;

                return address;
            }
        }

        public static void Patch(
            ICpuMemoryManager memoryManager,
            ulong textAddress,
            ulong textSize,
            ulong patchRegionAddress,
            ulong patchRegionSize)
        {
            Context context = new Context(memoryManager, patchRegionAddress, patchRegionSize);

            ulong address = textAddress;
            while (address < textAddress + textSize)
            {
                uint inst = memoryManager.Read<uint>(address);

                if ((inst & ~(0xffffu << 5)) == 0xd4000001u) // svc #0
                {
                    uint svcId = (ushort)(inst >> 5);
                    PatchInstruction(memoryManager, address, WriteSvcPatch(ref context, address, svcId));
                    Logger.Debug?.Print(LogClass.Cpu, $"Patched SVC #{svcId} at 0x{address:X}.");
                }
                else if ((inst & ~0x1f) == 0xd53bd060) // mrs x0, tpidrro_el0
                {
                    uint rd = inst & 0x1f;
                    PatchInstruction(memoryManager, address, WriteMrsTpidrroEl0Patch(ref context, address, rd));
                    Logger.Debug?.Print(LogClass.Cpu, $"Patched MRS x{rd}, tpidrro_el0 at 0x{address:X}.");
                }
                else if ((inst & ~0x1f) == 0xd53bd040) // mrs x0, tpidr_el0
                {
                    uint rd = inst & 0x1f;
                    PatchInstruction(memoryManager, address, WriteMrsTpidrEl0Patch(ref context, address, rd));
                    Logger.Debug?.Print(LogClass.Cpu, $"Patched MRS x{rd}, tpidr_el0 at 0x{address:X}.");
                }
                else if ((inst & ~0x1f) == 0xd53b0020 && OperatingSystem.IsMacOS()) // mrs x0, ctr_el0
                {
                    uint rd = inst & 0x1f;
                    PatchInstruction(memoryManager, address, WriteMrsCtrEl0Patch(ref context, address, rd));
                    Logger.Debug?.Print(LogClass.Cpu, $"Patched MRS x{rd}, ctr_el0 at 0x{address:X}.");
                }
                else if ((inst & ~0x1f) == 0xd51bd040) // msr tpidr_el0, x0
                {
                    uint rd = inst & 0x1f;
                    PatchInstruction(memoryManager, address, WriteMsrTpidrEl0Patch(ref context, address, rd));
                    Logger.Debug?.Print(LogClass.Cpu, $"Patched MSR tpidr_el0, x{rd} at 0x{address:X}.");
                }
                else if ((inst & ~0x1f) == 0xd53be020) // mrs x0, cntpct_el0
                {
                    uint rd = inst & 0x1f;
                    PatchInstruction(memoryManager, address, WriteMrsCntpctEl0Patch(ref context, address, rd));
                    Logger.Debug?.Print(LogClass.Cpu, $"Patched MRS x{rd}, cntpct_el0 at 0x{address:X}.");
                }

                address += 4;
            }

            ulong patchRegionConsumed = BitUtils.AlignUp(context.PatchRegionAddress - patchRegionAddress, 0x1000UL);
            if (patchRegionConsumed != 0)
            {
                memoryManager.Reprotect(patchRegionAddress, patchRegionConsumed, MemoryPermission.ReadAndExecute);
            }
        }

        private static void PatchInstruction(ICpuMemoryManager memoryManager, ulong instructionAddress, ulong targetAddress)
        {
            memoryManager.Write(instructionAddress, 0x14000000u | GetImm26(instructionAddress, targetAddress));
        }

        private static ulong WriteSvcPatch(ref Context context, ulong svcAddress, uint svcId)
        {
            uint[] code = GetCopy(NceAsmTable.SvcPatchCode);

            int movIndex = Array.IndexOf(code, 0xd2800013u);

            WritePointer(code, movIndex, (ulong)NceThreadTable.EntriesPointer);

            int mov2Index = Array.IndexOf(code, 0x52800000u, movIndex + 1);

            ulong targetAddress = context.GetPatchWriteAddress(code.Length);

            code[mov2Index] |= svcId << 5;
            code[code.Length - 1] |= GetImm26(targetAddress + (ulong)(code.Length - 1) * 4, svcAddress + 4);

            WriteCode(context.MemoryManager, targetAddress, code);

            return targetAddress;
        }

        private static ulong WriteMrsTpidrroEl0Patch(ref Context context, ulong mrsAddress, uint rd)
        {
            uint[] code = GetCopy(NceAsmTable.MrsTpidrroEl0PatchCode);

            int movIndex = Array.IndexOf(code, 0xd2800013u);

            WritePointer(code, movIndex, (ulong)NceThreadTable.EntriesPointer);

            ulong targetAddress = context.GetPatchWriteAddress(code.Length);

            code[code.Length - 2] |= rd;
            code[code.Length - 1] |= GetImm26(targetAddress + (ulong)(code.Length - 1) * 4, mrsAddress + 4);

            WriteCode(context.MemoryManager, targetAddress, code);

            return targetAddress;
        }

        private static ulong WriteMrsTpidrEl0Patch(ref Context context, ulong mrsAddress, uint rd)
        {
            uint[] code = GetCopy(NceAsmTable.MrsTpidrEl0PatchCode);

            int movIndex = Array.IndexOf(code, 0xd2800013u);

            WritePointer(code, movIndex, (ulong)NceThreadTable.EntriesPointer);

            ulong targetAddress = context.GetPatchWriteAddress(code.Length);

            code[code.Length - 2] |= rd;
            code[code.Length - 1] |= GetImm26(targetAddress + (ulong)(code.Length - 1) * 4, mrsAddress + 4);

            WriteCode(context.MemoryManager, targetAddress, code);

            return targetAddress;
        }

        private static ulong WriteMrsCtrEl0Patch(ref Context context, ulong mrsAddress, uint rd)
        {
            uint[] code = GetCopy(NceAsmTable.MrsCtrEl0PatchCode);

            int movIndex = Array.IndexOf(code, 0xd2800013u);

            WritePointer(code, movIndex, (ulong)NceThreadTable.EntriesPointer);

            ulong targetAddress = context.GetPatchWriteAddress(code.Length);

            code[code.Length - 2] |= rd;
            code[code.Length - 1] |= GetImm26(targetAddress + (ulong)(code.Length - 1) * 4, mrsAddress + 4);

            WriteCode(context.MemoryManager, targetAddress, code);

            return targetAddress;
        }

        private static ulong WriteMsrTpidrEl0Patch(ref Context context, ulong msrAddress, uint rd)
        {
            uint r2 = rd == 0 ? 1u : 0u;

            uint[] code = GetCopy(NceAsmTable.MsrTpidrEl0PatchCode);

            code[0] |= rd << 10;

            int movIndex = Array.IndexOf(code, 0xd2800013u);

            WritePointer(code, movIndex, (ulong)NceThreadTable.EntriesPointer);

            ulong targetAddress = context.GetPatchWriteAddress(code.Length);

            code[code.Length - 1] |= GetImm26(targetAddress + (ulong)(code.Length - 1) * 4, msrAddress + 4);

            WriteCode(context.MemoryManager, targetAddress, code);

            return targetAddress;
        }

        private static ulong WriteMrsCntpctEl0Patch(ref Context context, ulong mrsAddress, uint rd)
        {
            uint[] code = GetCopy(NceAsmTable.MrsCntpctEl0PatchCode);

            int movIndex = Array.IndexOf(code, 0xd2800013u);

            WritePointer(code, movIndex, (ulong)NceThreadTable.EntriesPointer);

            int mov2Index = Array.IndexOf(code, 0xD2800000u, movIndex + 1);

            WriteTickCounterAccessFunctionPointer(code, mov2Index);

            ulong targetAddress = context.GetPatchWriteAddress(code.Length);

            code[code.Length - 2] |= rd;
            code[code.Length - 1] |= GetImm26(targetAddress + (ulong)(code.Length - 1) * 4, mrsAddress + 4);

            WriteCode(context.MemoryManager, targetAddress, code);

            return targetAddress;
        }

        public static uint[] GenerateExceptionHandlerEntry(IntPtr oldSignalHandlerSegfaultPtr, IntPtr signalHandlerPtr)
        {
            uint[] code = GetCopy(NceAsmTable.ExceptionHandlerEntryCode);

            int movIndex = Array.IndexOf(code, 0xd2800018u);

            WritePointer(code, movIndex, (ulong)NceThreadTable.EntriesPointer);

            int mov2Index = Array.IndexOf(code, 0xd2800008u, movIndex + 1);

            WritePointer(code, mov2Index, (ulong)signalHandlerPtr);

            int mov3Index = Array.IndexOf(code, 0xd2800000u, mov2Index + 1);

            WritePointer(code, mov3Index, (ulong)NceNativeInterface.GetSuspendThreadHandlerFunctionPointer());

            int mov4Index = Array.IndexOf(code, 0xd2800003u, mov3Index + 1);

            WritePointer(code, mov4Index, (ulong)oldSignalHandlerSegfaultPtr);

            int cmpIndex = Array.IndexOf(code, 0x7100027fu);

            code[cmpIndex] |= (uint)NceThreadPal.UnixSuspendSignal << 10;

            return code;
        }

        private static void WriteTickCounterAccessFunctionPointer(uint[] code, int movIndex)
        {
            WritePointer(code, movIndex, (ulong)NceNativeInterface.GetTickCounterAccessFunctionPointer());
        }

        private static void WritePointer(uint[] code, int movIndex, ulong ptr)
        {
            code[movIndex] |= (uint)(ushort)ptr << 5;
            code[movIndex + 1] |= (uint)(ushort)(ptr >> 16) << 5;
            code[movIndex + 2] |= (uint)(ushort)(ptr >> 32) << 5;
            code[movIndex + 3] |= (uint)(ushort)(ptr >> 48) << 5;
        }

        private static uint GetImm26(ulong sourceAddress, ulong targetAddress)
        {
            long offset = (long)(targetAddress - sourceAddress);
            long offsetTrunc = (offset >> 2) & 0x3FFFFFF;

            if ((offsetTrunc << 38) >> 36 != offset)
            {
                throw new Exception($"Offset out of range: 0x{sourceAddress:X} -> 0x{targetAddress:X} (0x{offset:X})");
            }

            return (uint)offsetTrunc;
        }

        private static uint[] GetCopy(uint[] code)
        {
            uint[] codeCopy = new uint[code.Length];
            code.CopyTo(codeCopy, 0);

            return codeCopy;
        }

        private static void WriteCode(ICpuMemoryManager memoryManager, ulong address, uint[] code)
        {
            for (int i = 0; i < code.Length; i++)
            {
                memoryManager.Write(address + (ulong)i * sizeof(uint), code[i]);
            }
        }
    }
}