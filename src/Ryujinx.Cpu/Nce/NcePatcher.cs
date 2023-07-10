using Ryujinx.Cpu.Nce.Arm64;
using Ryujinx.Common.Logging;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Cpu.Nce
{
    public static class NcePatcher
    {
        private const int ScratchBaseReg = 19;

        private const uint IntCalleeSavedRegsMask = 0x1ff80000; // X19 to X28
        private const uint FpCalleeSavedRegsMask = 0xff00; // D8 to D15

        public static NceCpuCodePatch CreatePatch(ReadOnlySpan<byte> textSection)
        {
            NceCpuCodePatch codePatch = new();

            var textUint = MemoryMarshal.Cast<byte, uint>(textSection);

            for (int i = 0; i < textUint.Length; i++)
            {
                uint inst = textUint[i];
                ulong address = (ulong)i * sizeof(uint);

                if ((inst & ~(0xffffu << 5)) == 0xd4000001u) // svc #0
                {
                    uint svcId = (ushort)(inst >> 5);
                    codePatch.AddCode(i, WriteSvcPatch(svcId));
                    Logger.Debug?.Print(LogClass.Cpu, $"Patched SVC #{svcId} at 0x{address:X}.");
                }
                else if ((inst & ~0x1f) == 0xd53bd060) // mrs x0, tpidrro_el0
                {
                    uint rd = inst & 0x1f;
                    codePatch.AddCode(i, WriteMrsTpidrroEl0Patch(rd));
                    Logger.Debug?.Print(LogClass.Cpu, $"Patched MRS x{rd}, tpidrro_el0 at 0x{address:X}.");
                }
                else if ((inst & ~0x1f) == 0xd53bd040) // mrs x0, tpidr_el0
                {
                    uint rd = inst & 0x1f;
                    codePatch.AddCode(i, WriteMrsTpidrEl0Patch(rd));
                    Logger.Debug?.Print(LogClass.Cpu, $"Patched MRS x{rd}, tpidr_el0 at 0x{address:X}.");
                }
                else if ((inst & ~0x1f) == 0xd53b0020 && OperatingSystem.IsMacOS()) // mrs x0, ctr_el0
                {
                    uint rd = inst & 0x1f;
                    codePatch.AddCode(i, WriteMrsCtrEl0Patch(rd));
                    Logger.Debug?.Print(LogClass.Cpu, $"Patched MRS x{rd}, ctr_el0 at 0x{address:X}.");
                }
                else if ((inst & ~0x1f) == 0xd53be020) // mrs x0, cntpct_el0
                {
                    uint rd = inst & 0x1f;
                    codePatch.AddCode(i, WriteMrsCntpctEl0Patch(rd));
                    Logger.Debug?.Print(LogClass.Cpu, $"Patched MRS x{rd}, cntpct_el0 at 0x{address:X}.");
                }
                else if ((inst & ~0x1f) == 0xd51bd040) // msr tpidr_el0, x0
                {
                    uint rd = inst & 0x1f;
                    codePatch.AddCode(i, WriteMsrTpidrEl0Patch(rd));
                    Logger.Debug?.Print(LogClass.Cpu, $"Patched MSR tpidr_el0, x{rd} at 0x{address:X}.");
                }
            }

            return codePatch;
        }

        private static uint[] WriteSvcPatch(uint svcId)
        {
            Assembler asm = new();

            WriteManagedCall(asm, (asm, ctx, tmp, tmp2) =>
            {
                for (int i = 0; i < 8; i++)
                {
                    asm.StrRiUn(Gpr(i), ctx, NceNativeContext.GetXOffset(i));
                }

                WriteInManagedLockAcquire(asm, ctx, tmp, tmp2);

                asm.Mov(Gpr(0, OperandType.I32), svcId);
                asm.LdrRiUn(tmp, ctx, NceNativeContext.GetSvcCallHandlerOffset());
                asm.Blr(tmp);

                Operand lblContinue = asm.CreateLabel();
                Operand lblQuit = asm.CreateLabel();

                asm.Cbnz(Gpr(0, OperandType.I32), lblContinue);

                asm.MarkLabel(lblQuit);

                CreateRegisterSaveRestoreForManaged().WriteEpilogue(asm);

                asm.Ret(Gpr(30));

                asm.MarkLabel(lblContinue);

                WriteInManagedLockRelease(asm, ctx, tmp, tmp2, ThreadExitMethod.Label, lblQuit);

                for (int i = 0; i < 8; i++)
                {
                    asm.LdrRiUn(Gpr(i), ctx, NceNativeContext.GetXOffset(i));
                }
            }, 0xff);

            asm.B(0);

            return asm.GetCode();
        }

        private static uint[] WriteMrsTpidrroEl0Patch(uint rd)
        {
            return WriteMrsContextRead(rd, NceNativeContext.GetTpidrroEl0Offset());
        }

        private static uint[] WriteMrsTpidrEl0Patch(uint rd)
        {
            return WriteMrsContextRead(rd, NceNativeContext.GetTpidrEl0Offset());
        }

        private static uint[] WriteMrsCtrEl0Patch(uint rd)
        {
            return WriteMrsContextRead(rd, NceNativeContext.GetCtrEl0Offset());
        }

        private static uint[] WriteMrsCntpctEl0Patch(uint rd)
        {
            Assembler asm = new();

            WriteManagedCall(asm, (asm, ctx, tmp, tmp2) =>
            {
                WriteInManagedLockAcquire(asm, ctx, tmp, tmp2);

                asm.Mov(tmp, (ulong)NceNativeInterface.GetTickCounterAccessFunctionPointer());
                asm.Blr(tmp);
                asm.StrRiUn(Gpr(0), ctx, NceNativeContext.GetTempStorageOffset());

                WriteInManagedLockRelease(asm, ctx, tmp, tmp2, ThreadExitMethod.GenerateReturn);

                asm.LdrRiUn(Gpr((int)rd), ctx, NceNativeContext.GetTempStorageOffset());
            }, 1u << (int)rd);

            asm.B(0);

            return asm.GetCode();
        }

        private static uint[] WriteMsrTpidrEl0Patch(uint rd)
        {
            Assembler asm = new();

            Span<int> scratchRegs = stackalloc int[3];
            PickScratchRegs(scratchRegs, 1u << (int)rd);

            RegisterSaveRestore rsr = new((1 << scratchRegs[0]) | (1 << scratchRegs[1]) | (1 << scratchRegs[2]));

            rsr.WritePrologue(asm);

            WriteLoadContext(asm, Gpr(scratchRegs[0]), Gpr(scratchRegs[1]), Gpr(scratchRegs[2]));
            asm.StrRiUn(Gpr((int)rd), Gpr(scratchRegs[0]),NceNativeContext.GetTpidrEl0Offset());

            rsr.WriteEpilogue(asm);

            asm.B(0);

            return asm.GetCode();
        }

        private static uint[] WriteMrsContextRead(uint rd, int contextOffset)
        {
            Assembler asm = new();

            Span<int> scratchRegs = stackalloc int[3];
            PickScratchRegs(scratchRegs, 1u << (int)rd);

            RegisterSaveRestore rsr = new((1 << scratchRegs[0]) | (1 << scratchRegs[1]) | (1 << scratchRegs[2]));

            rsr.WritePrologue(asm);

            WriteLoadContext(asm, Gpr(scratchRegs[0]), Gpr(scratchRegs[1]), Gpr(scratchRegs[2]));
            asm.Add(Gpr((int)rd), Gpr(scratchRegs[0]), Const((ulong)contextOffset));

            rsr.WriteEpilogue(asm);

            asm.LdrRiUn(Gpr((int)rd), Gpr((int)rd), 0);

            asm.B(0);

            return asm.GetCode();
        }

        private static void WriteLoadContext(Assembler asm, Operand tmp0, Operand tmp1, Operand tmp2)
        {
            asm.Mov(tmp0, (ulong)NceThreadTable.EntriesPointer);

            if (OperatingSystem.IsMacOS())
            {
                asm.MrsTpidrroEl0(tmp1);
            }
            else
            {
                asm.MrsTpidrEl0(tmp1);
            }

            Operand lblFound = asm.CreateLabel();
            Operand lblLoop = asm.CreateLabel();

            asm.MarkLabel(lblLoop);

            asm.LdrRiPost(tmp2, tmp0, 16);
            asm.Cmp(tmp1, tmp2);
            asm.B(lblFound, ArmCondition.Eq);
            asm.B(lblLoop);

            asm.MarkLabel(lblFound);

            asm.Ldur(tmp0, tmp0, -8);
        }

        private static void WriteLoadContextSafe(Assembler asm, Operand lblFail, Operand tmp0, Operand tmp1, Operand tmp2, Operand tmp3)
        {
            asm.Mov(tmp0, (ulong)NceThreadTable.EntriesPointer);
            asm.Ldur(tmp3, tmp0, -8);
            asm.Add(tmp3, tmp0, tmp3, ArmShiftType.Lsl, 4);

            if (OperatingSystem.IsMacOS())
            {
                asm.MrsTpidrroEl0(tmp1);
            }
            else
            {
                asm.MrsTpidrEl0(tmp1);
            }

            Operand lblFound = asm.CreateLabel();
            Operand lblLoop = asm.CreateLabel();

            asm.MarkLabel(lblLoop);

            asm.Cmp(tmp0, tmp3);
            asm.B(lblFail, ArmCondition.GeUn);
            asm.LdrRiPost(tmp2, tmp0, 16);
            asm.Cmp(tmp1, tmp2);
            asm.B(lblFound, ArmCondition.Eq);
            asm.B(lblLoop);

            asm.MarkLabel(lblFound);

            asm.Ldur(tmp0, tmp0, -8);
        }

        private static void PickScratchRegs(Span<int> scratchRegs, uint blacklistedRegMask)
        {
            int scratchReg = ScratchBaseReg;

            for (int i = 0; i < scratchRegs.Length; i++)
            {
                while ((blacklistedRegMask & (1u << scratchReg)) != 0)
                {
                    scratchReg++;
                }

                if (scratchReg >= 29)
                {
                    throw new ArgumentException($"No enough register for {scratchRegs.Length} scratch register, started from {ScratchBaseReg}");
                }

                scratchRegs[i] = scratchReg++;
            }
        }

        private static Operand Gpr(int register, OperandType type = OperandType.I64)
        {
            return new Operand(register, RegisterType.Integer, type);
        }

        private static Operand Vec(int register, OperandType type = OperandType.V128)
        {
            return new Operand(register, RegisterType.Vector, type);
        }

        private static Operand Const(ulong value)
        {
            return new Operand(OperandType.I64, value);
        }

        private static Operand Const(OperandType type, ulong value)
        {
            return new Operand(type, value);
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

        private static int GetOffset(ulong sourceAddress, ulong targetAddress)
        {
            long offset = (long)(targetAddress - sourceAddress);

            return checked((int)offset);
        }

        private static uint[] GetCopy(uint[] code)
        {
            uint[] codeCopy = new uint[code.Length];
            code.CopyTo(codeCopy, 0);

            return codeCopy;
        }

        private static void WriteManagedCall(Assembler asm, Action<Assembler, Operand, Operand, Operand> writeCall, uint blacklistedRegMask)
        {
            int intMask = 0x7fffffff & (int)~blacklistedRegMask;
            int vecMask = unchecked((int)0xffffffff);

            Span<int> scratchRegs = stackalloc int[3];
            PickScratchRegs(scratchRegs, blacklistedRegMask);

            RegisterSaveRestore rsr = new(intMask, vecMask, OperandType.V128);

            rsr.WritePrologue(asm);

            WriteLoadContext(asm, Gpr(scratchRegs[0]), Gpr(scratchRegs[1]), Gpr(scratchRegs[2]));

            asm.MovSp(Gpr(scratchRegs[1]), Gpr(Assembler.SpRegister));
            asm.StrRiUn(Gpr(scratchRegs[1]), Gpr(scratchRegs[0]), NceNativeContext.GetGuestSPOffset());
            asm.LdrRiUn(Gpr(scratchRegs[1]), Gpr(scratchRegs[0]), NceNativeContext.GetHostSPOffset());
            asm.MovSp(Gpr(Assembler.SpRegister), Gpr(scratchRegs[1]));

            writeCall(asm, Gpr(scratchRegs[0]), Gpr(scratchRegs[1]), Gpr(scratchRegs[2]));

            asm.LdrRiUn(Gpr(scratchRegs[1]), Gpr(scratchRegs[0]), NceNativeContext.GetGuestSPOffset());
            asm.MovSp(Gpr(Assembler.SpRegister), Gpr(scratchRegs[1]));

            rsr.WriteEpilogue(asm);
        }

        internal static uint[] GenerateThreadStartCode()
        {
            Assembler asm = new();

            CreateRegisterSaveRestoreForManaged().WritePrologue(asm);

            asm.MovSp(Gpr(1), Gpr(Assembler.SpRegister));
            asm.StrRiUn(Gpr(1), Gpr(0), NceNativeContext.GetHostSPOffset());

            for (int i = 2; i < 30; i += 2)
            {
                asm.LdpRiUn(Gpr(i), Gpr(i + 1), Gpr(0), NceNativeContext.GetXOffset(i));
            }

            for (int i = 0; i < 32; i += 2)
            {
                asm.LdpRiUn(Vec(i), Vec(i + 1), Gpr(0), NceNativeContext.GetVOffset(i));
            }

            asm.LdpRiUn(Gpr(30), Gpr(1), Gpr(0), NceNativeContext.GetXOffset(30));
            asm.MovSp(Gpr(Assembler.SpRegister), Gpr(1));

            asm.StrRiUn(Gpr(Assembler.ZrRegister, OperandType.I32), Gpr(0), NceNativeContext.GetInManagedOffset());

            asm.LdpRiUn(Gpr(0), Gpr(1), Gpr(0), NceNativeContext.GetXOffset(0));
            asm.Br(Gpr(30));

            return asm.GetCode();
        }

        internal static uint[] GenerateSuspendExceptionHandler()
        {
            Assembler asm = new();

            Span<int> scratchRegs = stackalloc int[4];
            PickScratchRegs(scratchRegs, 0u);

            RegisterSaveRestore rsr = new((1 << scratchRegs[0]) | (1 << scratchRegs[1]) | (1 << scratchRegs[2]) | (1 << scratchRegs[3]), hasCall: true);

            rsr.WritePrologue(asm);

            Operand lblAgain = asm.CreateLabel();
            Operand lblFail = asm.CreateLabel();

            WriteLoadContextSafe(asm, lblFail, Gpr(scratchRegs[0]), Gpr(scratchRegs[1]), Gpr(scratchRegs[2]), Gpr(scratchRegs[3]));

            asm.LdrRiUn(Gpr(scratchRegs[1]), Gpr(scratchRegs[0]), NceNativeContext.GetHostSPOffset());
            asm.MovSp(Gpr(scratchRegs[2]), Gpr(Assembler.SpRegister));
            asm.MovSp(Gpr(Assembler.SpRegister), Gpr(scratchRegs[1]));

            asm.Cmp(Gpr(0, OperandType.I32), Const((ulong)NceThreadPal.UnixSuspendSignal));
            asm.B(lblFail, ArmCondition.Ne);

            // SigUsr2

            asm.Mov(Gpr(scratchRegs[1], OperandType.I32), Const(OperandType.I32, 1));
            asm.StrRiUn(Gpr(scratchRegs[1], OperandType.I32), Gpr(scratchRegs[0]), NceNativeContext.GetInManagedOffset());

            asm.MarkLabel(lblAgain);

            asm.Mov(Gpr(scratchRegs[3]), (ulong)NceNativeInterface.GetSuspendThreadHandlerFunctionPointer());
            asm.Blr(Gpr(scratchRegs[3]));

            // TODO: Check return value, exit if we must.

            WriteInManagedLockReleaseForSuspendHandler(asm, Gpr(scratchRegs[0]), Gpr(scratchRegs[1]), Gpr(scratchRegs[3]), lblAgain);

            asm.MovSp(Gpr(Assembler.SpRegister), Gpr(scratchRegs[2]));

            rsr.WriteEpilogue(asm);

            asm.Ret(Gpr(30));

            asm.MarkLabel(lblFail);

            rsr.WriteEpilogue(asm);

            asm.Ret(Gpr(30));

            return asm.GetCode();
        }

        internal static uint[] GenerateWrapperExceptionHandler(IntPtr oldSignalHandlerSegfaultPtr, IntPtr signalHandlerPtr)
        {
            Assembler asm = new();

            Span<int> scratchRegs = stackalloc int[4];
            PickScratchRegs(scratchRegs, 0u);

            RegisterSaveRestore rsr = new((1 << scratchRegs[0]) | (1 << scratchRegs[1]) | (1 << scratchRegs[2]) | (1 << scratchRegs[3]), hasCall: true);

            rsr.WritePrologue(asm);

            Operand lblFail = asm.CreateLabel();

            WriteLoadContextSafe(asm, lblFail, Gpr(scratchRegs[0]), Gpr(scratchRegs[1]), Gpr(scratchRegs[2]), Gpr(scratchRegs[3]));

            asm.LdrRiUn(Gpr(scratchRegs[1]), Gpr(scratchRegs[0]), NceNativeContext.GetHostSPOffset());
            asm.MovSp(Gpr(scratchRegs[2]), Gpr(Assembler.SpRegister));
            asm.MovSp(Gpr(Assembler.SpRegister), Gpr(scratchRegs[1]));

            // SigSegv

            WriteInManagedLockAcquire(asm, Gpr(scratchRegs[0]), Gpr(scratchRegs[1]), Gpr(scratchRegs[3]));

            asm.Mov(Gpr(scratchRegs[3]), (ulong)signalHandlerPtr);
            asm.Blr(Gpr(scratchRegs[3]));

            WriteInManagedLockRelease(asm, Gpr(scratchRegs[0]), Gpr(scratchRegs[1]), Gpr(scratchRegs[3]), ThreadExitMethod.None);

            asm.MovSp(Gpr(Assembler.SpRegister), Gpr(scratchRegs[2]));

            rsr.WriteEpilogue(asm);

            asm.Ret(Gpr(30));

            asm.MarkLabel(lblFail);

            rsr.WriteEpilogue(asm);

            asm.Mov(Gpr(3), (ulong)oldSignalHandlerSegfaultPtr);
            asm.Br(Gpr(3));

            return asm.GetCode();
        }

        private static void WriteInManagedLockAcquire(Assembler asm, Operand ctx, Operand tmp, Operand tmp2)
        {
            Operand tmpUint = new Operand(tmp.GetRegister().Index, RegisterType.Integer, OperandType.I32);
            Operand tmp2Uint = new Operand(tmp2.GetRegister().Index, RegisterType.Integer, OperandType.I32);

            Operand lblLoop = asm.CreateLabel();

            // Bit 0 set means that the thread is currently executing managed code (that case should be impossible here).
            // Bit 1 being set means there is a signal pending, we should wait for the signal, otherwise it could trigger
            // while running managed code.

            asm.MarkLabel(lblLoop);

            asm.Add(tmp, ctx, Const((ulong)NceNativeContext.GetInManagedOffset()));
            asm.Ldaxr(tmp2Uint, tmp);
            asm.Cbnz(tmp2Uint, lblLoop);
            asm.Mov(tmp2Uint, Const(OperandType.I32, 1));
            asm.Stlxr(tmp2Uint, tmp, tmpUint);
            asm.Cbnz(tmpUint, lblLoop); // Retry if store failed.
        }

        private enum ThreadExitMethod
        {
            None,
            GenerateReturn,
            Label
        }

        private static void WriteInManagedLockRelease(Assembler asm, Operand ctx, Operand tmp, Operand tmp2, ThreadExitMethod exitMethod, Operand lblQuit = default)
        {
            Operand tmpUint = new Operand(tmp.GetRegister().Index, RegisterType.Integer, OperandType.I32);
            Operand tmp2Uint = new Operand(tmp2.GetRegister().Index, RegisterType.Integer, OperandType.I32);

            Operand lblLoop = asm.CreateLabel();
            Operand lblInterrupt = asm.CreateLabel();
            Operand lblDone = asm.CreateLabel();

            // Bit 0 set means that the thread is currently executing managed code (it should be always set here, as we just returned from managed code).
            // Bit 1 being set means a interrupt was requested while it was in managed, we should service it.

            asm.MarkLabel(lblLoop);

            asm.Add(tmp, ctx, Const((ulong)NceNativeContext.GetInManagedOffset()));
            asm.Ldaxr(tmp2Uint, tmp);
            asm.Cmp(tmp2Uint, Const(OperandType.I32, 3));
            asm.B(lblInterrupt, ArmCondition.Eq);
            asm.Stlxr(Gpr(Assembler.ZrRegister, OperandType.I32), tmp, tmpUint);
            asm.Cbnz(tmpUint, lblLoop); // Retry if store failed.
            asm.B(lblDone);

            asm.MarkLabel(lblInterrupt);

            // If we got here, a interrupt was requested while it was in managed code.
            // Let's service the interrupt and check what we should do next.

            asm.Mov(tmp2Uint, Const(OperandType.I32, 1));
            asm.Stlxr(tmp2Uint, tmp, tmpUint);
            asm.Cbnz(tmpUint, lblLoop); // Retry if store failed.
            asm.Mov(tmp, (ulong)NceNativeInterface.GetSuspendThreadHandlerFunctionPointer());
            asm.Blr(tmp);

            // The return value from the interrupt handler indicates if we should continue running.
            // From here, we either try to release the lock again. We might have received another interrupt
            // request in the meantime, in which case we should service it again.
            // If we were requested to exit, then we exit if we can.
            // TODO: We should also exit while on a signal handler. To do that we need to modify the PC value on the
            // context. It's a bit more tricky to do, so for now we ignore that case with "ThreadExitMethod.None".

            if (exitMethod == ThreadExitMethod.None)
            {
                asm.B(lblLoop);
            }
            else
            {
                asm.Cbnz(Gpr(0, OperandType.I32), lblLoop);

                if (exitMethod == ThreadExitMethod.Label)
                {
                    asm.B(lblQuit);
                }
                else if (exitMethod == ThreadExitMethod.GenerateReturn)
                {
                    CreateRegisterSaveRestoreForManaged().WriteEpilogue(asm);

                    asm.Ret(Gpr(30));
                }
            }

            asm.MarkLabel(lblDone);
        }

        private static void WriteInManagedLockReleaseForSuspendHandler(Assembler asm, Operand ctx, Operand tmp, Operand tmp2, Operand lblAgain)
        {
            Operand tmpUint = new Operand(tmp.GetRegister().Index, RegisterType.Integer, OperandType.I32);
            Operand tmp2Uint = new Operand(tmp2.GetRegister().Index, RegisterType.Integer, OperandType.I32);

            Operand lblLoop = asm.CreateLabel();
            Operand lblInterrupt = asm.CreateLabel();
            Operand lblDone = asm.CreateLabel();

            // Bit 0 set means that the thread is currently executing managed code (it should be always set here, as we just returned from managed code).
            // Bit 1 being set means a interrupt was requested while it was in managed, we should service it.

            asm.MarkLabel(lblLoop);

            asm.Add(tmp, ctx, Const((ulong)NceNativeContext.GetInManagedOffset()));
            asm.Ldaxr(tmp2Uint, tmp);
            asm.Cmp(tmp2Uint, Const(OperandType.I32, 3));
            asm.B(lblInterrupt, ArmCondition.Eq);
            asm.Stlxr(Gpr(Assembler.ZrRegister, OperandType.I32), tmp, tmpUint);
            asm.Cbnz(tmpUint, lblLoop); // Retry if store failed.
            asm.B(lblDone);

            asm.MarkLabel(lblInterrupt);

            // If we got here, a interrupt was requested while it was in managed code.
            // Let's service the interrupt and check what we should do next.

            asm.Mov(tmp2Uint, Const(OperandType.I32, 1));
            asm.Stlxr(tmp2Uint, tmp, tmpUint);
            asm.Cbnz(tmpUint, lblLoop); // Retry if store failed.
            asm.B(lblAgain);

            asm.MarkLabel(lblDone);
        }

        private static RegisterSaveRestore CreateRegisterSaveRestoreForManaged()
        {
            return new RegisterSaveRestore((int)IntCalleeSavedRegsMask, unchecked((int)FpCalleeSavedRegsMask), OperandType.FP64, hasCall: true);
        }
    }
}