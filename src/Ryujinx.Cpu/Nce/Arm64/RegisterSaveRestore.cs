using System.Numerics;

namespace Ryujinx.Cpu.Nce.Arm64
{
    readonly struct RegisterSaveRestore
    {
        private const int FpRegister = 29;
        private const int LrRegister = 30;

        private const int Encodable9BitsOffsetLimit = 0x200;

        private readonly int _intMask;
        private readonly int _vecMask;
        private readonly OperandType _vecType;
        private readonly bool _hasCall;

        public RegisterSaveRestore(int intMask, int vecMask = 0, OperandType vecType = OperandType.FP64, bool hasCall = false)
        {
            _intMask = intMask;
            _vecMask = vecMask;
            _vecType = vecType;
            _hasCall = hasCall;
        }

        public void WritePrologue(Assembler asm)
        {
            int intMask = _intMask;
            int vecMask = _vecMask;

            int intCalleeSavedRegsCount = BitOperations.PopCount((uint)intMask);
            int vecCalleeSavedRegsCount = BitOperations.PopCount((uint)vecMask);

            int calleeSaveRegionSize = Align16(intCalleeSavedRegsCount * 8 + vecCalleeSavedRegsCount * _vecType.GetSizeInBytes());

            int offset = 0;

            WritePrologueCalleeSavesPreIndexed(asm, ref intMask, ref offset, calleeSaveRegionSize, OperandType.I64);

            if (_vecType == OperandType.V128 && (intCalleeSavedRegsCount & 1) != 0)
            {
                offset += 8;
            }

            WritePrologueCalleeSavesPreIndexed(asm, ref vecMask, ref offset, calleeSaveRegionSize, _vecType);

            if (_hasCall)
            {
                Operand rsp = Register(Assembler.SpRegister);

                asm.StpRiPre(Register(FpRegister), Register(LrRegister), rsp, -16);
                asm.MovSp(Register(FpRegister), rsp);
            }
        }

        private static void WritePrologueCalleeSavesPreIndexed(
            Assembler asm,
            ref int mask,
            ref int offset,
            int calleeSaveRegionSize,
            OperandType type)
        {
            if ((BitOperations.PopCount((uint)mask) & 1) != 0)
            {
                int reg = BitOperations.TrailingZeroCount(mask);

                mask &= ~(1 << reg);

                if (offset != 0)
                {
                    asm.StrRiUn(Register(reg, type), Register(Assembler.SpRegister), offset);
                }
                else if (calleeSaveRegionSize < Encodable9BitsOffsetLimit)
                {
                    asm.StrRiPre(Register(reg, type), Register(Assembler.SpRegister), -calleeSaveRegionSize);
                }
                else
                {
                    asm.Sub(Register(Assembler.SpRegister), Register(Assembler.SpRegister), new Operand(OperandType.I64, (ulong)calleeSaveRegionSize));
                    asm.StrRiUn(Register(reg, type), Register(Assembler.SpRegister), 0);
                }

                offset += type.GetSizeInBytes();
            }

            while (mask != 0)
            {
                int reg = BitOperations.TrailingZeroCount(mask);

                mask &= ~(1 << reg);

                int reg2 = BitOperations.TrailingZeroCount(mask);

                mask &= ~(1 << reg2);

                if (offset != 0)
                {
                    asm.StpRiUn(Register(reg, type), Register(reg2, type), Register(Assembler.SpRegister), offset);
                }
                else if (calleeSaveRegionSize < Encodable9BitsOffsetLimit)
                {
                    asm.StpRiPre(Register(reg, type), Register(reg2, type), Register(Assembler.SpRegister), -calleeSaveRegionSize);
                }
                else
                {
                    asm.Sub(Register(Assembler.SpRegister), Register(Assembler.SpRegister), new Operand(OperandType.I64, (ulong)calleeSaveRegionSize));
                    asm.StpRiUn(Register(reg, type), Register(reg2, type), Register(Assembler.SpRegister), 0);
                }

                offset += type.GetSizeInBytes() * 2;
            }
        }

        public void WriteEpilogue(Assembler asm)
        {
            int intMask = _intMask;
            int vecMask = _vecMask;

            int intCalleeSavedRegsCount = BitOperations.PopCount((uint)intMask);
            int vecCalleeSavedRegsCount = BitOperations.PopCount((uint)vecMask);

            bool misalignedVector = _vecType == OperandType.V128 && (intCalleeSavedRegsCount & 1) != 0;

            int offset = intCalleeSavedRegsCount * 8 + vecCalleeSavedRegsCount * _vecType.GetSizeInBytes();

            if (misalignedVector)
            {
                offset += 8;
            }

            int calleeSaveRegionSize = Align16(offset);

            if (_hasCall)
            {
                Operand rsp = Register(Assembler.SpRegister);

                asm.LdpRiPost(Register(FpRegister), Register(LrRegister), rsp, 16);
            }

            WriteEpilogueCalleeSavesPostIndexed(asm, ref vecMask, ref offset, calleeSaveRegionSize, _vecType);

            if (misalignedVector)
            {
                offset -= 8;
            }

            WriteEpilogueCalleeSavesPostIndexed(asm, ref intMask, ref offset, calleeSaveRegionSize, OperandType.I64);
        }

        private static void WriteEpilogueCalleeSavesPostIndexed(
            Assembler asm,
            ref int mask,
            ref int offset,
            int calleeSaveRegionSize,
            OperandType type)
        {
            while (mask != 0)
            {
                int reg = HighestBitSet(mask);

                mask &= ~(1 << reg);

                if (mask != 0)
                {
                    int reg2 = HighestBitSet(mask);

                    mask &= ~(1 << reg2);

                    offset -= type.GetSizeInBytes() * 2;

                    if (offset != 0)
                    {
                        asm.LdpRiUn(Register(reg2, type), Register(reg, type), Register(Assembler.SpRegister), offset);
                    }
                    else if (calleeSaveRegionSize < Encodable9BitsOffsetLimit)
                    {
                        asm.LdpRiPost(Register(reg2, type), Register(reg, type), Register(Assembler.SpRegister), calleeSaveRegionSize);
                    }
                    else
                    {
                        asm.LdpRiUn(Register(reg2, type), Register(reg, type), Register(Assembler.SpRegister), 0);
                        asm.Add(Register(Assembler.SpRegister), Register(Assembler.SpRegister), new Operand(OperandType.I64, (ulong)calleeSaveRegionSize));
                    }
                }
                else
                {
                    offset -= type.GetSizeInBytes();

                    if (offset != 0)
                    {
                        asm.LdrRiUn(Register(reg, type), Register(Assembler.SpRegister), offset);
                    }
                    else  if (calleeSaveRegionSize < Encodable9BitsOffsetLimit)
                    {
                        asm.LdrRiPost(Register(reg, type), Register(Assembler.SpRegister), calleeSaveRegionSize);
                    }
                    else
                    {
                        asm.LdrRiUn(Register(reg, type), Register(Assembler.SpRegister), 0);
                        asm.Add(Register(Assembler.SpRegister), Register(Assembler.SpRegister), new Operand(OperandType.I64, (ulong)calleeSaveRegionSize));
                    }
                }
            }
        }

        private static int HighestBitSet(int value)
        {
            return 31 - BitOperations.LeadingZeroCount((uint)value);
        }

        private static Operand Register(int register, OperandType type = OperandType.I64)
        {
            return new Operand(register, RegisterType.Integer, type);
        }

        private static int Align16(int value)
        {
            return (value + 0xf) & ~0xf;
        }
    }
}