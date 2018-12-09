using System;
using System.Collections.Generic;

namespace core.microcode
{
    public enum MicroOpcode
    {
        Nop,
        Data,
        Call,
        Return,
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
        /// <summary>
        /// Size of this argument.
        /// </summary>
        byte Bits { get; }
    }

    public class ConstValue : IMicroArg
    {
        public byte Bits { get; }

        public readonly ulong Value;

        public ConstValue(ulong value, byte bits)
        {
            Value = value;
            Bits = bits;
        }

        public override string ToString()
        {
            return $"0x{Value:X}<<{Bits}>>";
        }
    }

    public class AddressValue : IMicroArg
    {
        private readonly string _name;
        public readonly ulong Address;
        public byte Bits { get; }

        public AddressValue(ulong address, string name, byte bits)
        {
            _name = name;
            Address = address;
            Bits = bits;
        }

        public override string ToString()
        {
            return $"0x{Address:X}[[{_name ?? "?"}]]";
        }
    }


    public class RegisterArg : IMicroArg
    {
        public byte Bits { get; }
        public uint Register { get; }

        public RegisterArg(uint register, byte bits)
        {
            Register = register;
            Bits = bits;
        }

        public override string ToString()
        {
            return $"$r{Register}";
        }

        public UnsignedCastInsn UCastTo(RegisterArg dst)
        {
            return new UnsignedCastInsn(this, dst);
        }

        public SignedCastInsn SCastTo(RegisterArg dst)
        {
            return new SignedCastInsn(this, dst);
        }
    }

    public class RegisterMemArg : IMicroArg
    {
        public byte Bits { get; }
        public uint Register { get; }

        public readonly int Offset;

        public RegisterMemArg(uint register, int offset, byte bits)
        {
            Register = register;
            Offset = offset;
            Bits = bits;
        }

        public override string ToString()
        {
            return $"$r{Register}(0x{Offset:X})";
        }
    }

    public class MicroInsn
    {
        public readonly MicroOpcode Opcode;
        public readonly IList<IMicroArg> Args;

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
        private readonly byte _bits;

        public CopyInsn(IMicroArg dest, IMicroArg src) : base(MicroOpcode.Copy, dest, src)
        {
            if (dest.Bits != src.Bits)
                throw new ArgumentException($"Parameter bit size mismatch (dest={dest.Bits} vs. src={src.Bits})");

            _bits = dest.Bits;
        }

        public override string ToString()
        {
            return base.ToString() + $" <<{_bits}>>";
        }
    }

    public class UnsignedCastInsn : MicroInsn
    {
        private readonly byte _fromBits;
        private readonly byte _toBits;

        public UnsignedCastInsn(RegisterArg from, RegisterArg to) : base(MicroOpcode.UResize, to, from)
        {
            _fromBits = from.Bits;
            _toBits = to.Bits;
        }

        public override string ToString()
        {
            return base.ToString() + $" <<{_fromBits} -> {_toBits}>>";
        }
    }

    public class SignedCastInsn : MicroInsn
    {
        private readonly byte _fromBits;
        private readonly byte _toBits;

        public SignedCastInsn(RegisterArg from, RegisterArg to) : base(MicroOpcode.SResize, to, from)
        {
            _fromBits = from.Bits;
            _toBits = to.Bits;
        }

        public override string ToString()
        {
            return base.ToString() + $" <<{_fromBits} -> {_toBits}>>";
        }
    }

    public class UnsupportedInsn : MicroInsn
    {
        private readonly string _mnemonic;

        public UnsupportedInsn(string mnemonic, params IMicroArg[] args) : base(MicroOpcode.Nop, args)
        {
            _mnemonic = mnemonic;
        }

        public override string ToString()
        {
            return base.ToString() + $" [{_mnemonic}]";
        }
    }

    public enum JumpType
    {
        Call,
        CallConditional,
        Jump,
        JumpConditional,
        Control
    }

    public class MicroAssemblyBlock
    {
        public readonly uint Address;
        public readonly IList<MicroInsn> Insns = new List<MicroInsn>();
        public readonly IDictionary<uint, JumpType> Ins = new Dictionary<uint, JumpType>();
        public IDictionary<uint, JumpType> Outs = new Dictionary<uint, JumpType>();

        public MicroAssemblyBlock(uint address)
        {
            Address = address;
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

        public void Optimize(IDebugSource debugSource)
        {
            PeepholeOptimizer.Optimize(Insns, debugSource);
        }
    }
}
