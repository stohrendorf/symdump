using System.Collections.Generic;
using System.Diagnostics;

namespace core.microcode
{
    public enum MicroOpcode
    {
        Nop,
        Data,
        Call,
        Jmp,
        JmpIf,
        Cmp,
        SSetL,
        SSetLE,
        USetL,
        USetLE,
        SetEq,
        LogicalNot,
        SHL,
        SRL,
        SRA,
        XOr,
        And,
        Or,
        Not,
        Sub,
        Add,
        Copy,
        UResize,
        SResize
    }

    public interface IMicroArg
    {
    }

    public class ConstValue : IMicroArg
    {
        public readonly ulong Value;
        public readonly byte Bits;

        public ConstValue(ulong value, byte bits)
        {
            Value = value;
            Bits = bits;
        }

        public override string ToString()
        {
            return $"0x{Value:X}[{Bits}]";
        }
    }

    public class AddressValue : ConstValue
    {
        public readonly string Name;

        public AddressValue(ulong value, string name) : base(value, 32)
        {
            Name = name;
        }

        public override string ToString()
        {
            if (Name != null)
                return $"&{Name}[@0x{Value:X}]";
            else
                return $"&<unnamed>[@0x{Value:X}]";
        }
    }


    public class RegisterArg : IMicroArg
    {
        public readonly uint Register;

        public RegisterArg(uint register)
        {
            Register = register;
        }

        public override string ToString()
        {
            return $"$r{Register}";
        }
    }

    public class RegisterMemArg : IMicroArg
    {
        public readonly uint Register;
        public readonly int Offset;

        public RegisterMemArg(uint register, int offset = 0)
        {
            Register = register;
            Offset = offset;
        }

        public override string ToString()
        {
            return $"$r{Register}(0x{Offset:X})";
        }
    }

    public class MicroInsn
    {
        public MicroOpcode Opcode = MicroOpcode.Nop;
        public IList<IMicroArg> Args;

        public MicroInsn(MicroOpcode opcode, params IMicroArg[] args)
        {
            Opcode = opcode;
            Args = new List<IMicroArg>(args);
        }

        public override string ToString()
        {
            return Opcode + " " + string.Join(", ", Args);
        }
    }

    public class CopyInsn : MicroInsn
    {
        public readonly byte Bits;

        public CopyInsn(IMicroArg dest, IMicroArg src, byte bits) : base(MicroOpcode.Copy, dest, src)
        {
            Debug.Assert(bits > 0);
            Bits = bits;
        }

        public override string ToString()
        {
            return base.ToString() + $" [{Bits}]";
        }
    }

    public class UnsignedCastInsn : MicroInsn
    {
        public readonly byte FromBits;
        public readonly byte ToBits;

        public UnsignedCastInsn(RegisterArg arg, byte fromBits, byte toBits) : base(MicroOpcode.UResize, arg)
        {
            Debug.Assert(fromBits > 0);
            Debug.Assert(toBits > 0);

            FromBits = fromBits;
            ToBits = toBits;
        }

        public override string ToString()
        {
            return base.ToString() + $" [{FromBits} -> {ToBits}]";
        }
    }

    public class SignedCastInsn : MicroInsn
    {
        public readonly byte FromBits;
        public readonly byte ToBits;

        public SignedCastInsn(RegisterArg arg, byte fromBits, byte toBits) : base(MicroOpcode.SResize, arg)
        {
            Debug.Assert(fromBits > 0);
            Debug.Assert(toBits > 0);

            FromBits = fromBits;
            ToBits = toBits;
        }

        public override string ToString()
        {
            return base.ToString() + $" [{FromBits} -> {ToBits}]";
        }
    }

    public class UnsupportedInsn : MicroInsn
    {
        public readonly string Mnemonic;

        public UnsupportedInsn(string mnemonic, params IMicroArg[] args) : base(MicroOpcode.Nop, args)
        {
            Mnemonic = mnemonic;
        }

        public override string ToString()
        {
            return base.ToString() + $" [{Mnemonic}]";
        }
    }

    public class MicroAssembly
    {
        public IList<MicroInsn> Insns = new List<MicroInsn>();

        private static uint _regId = 1000;

        public uint GetTmpRegId()
        {
            return _regId++;
        }

        public RegisterArg GetTmpReg()
        {
            return new RegisterArg(GetTmpRegId());
        }

        public void Add(MicroInsn insn)
        {
            Insns.Add(insn);
        }

        public void Add(MicroOpcode opcode, params IMicroArg[] args)
        {
            Add(new MicroInsn(opcode, args));
        }

        public override string ToString()
        {
            return string.Join("\n", Insns);
        }
    }
}
