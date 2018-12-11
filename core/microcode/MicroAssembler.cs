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
        SSetL,
        SSetLE,
        USetL,
        USetLE,
        SetEq,
        SetNEq,
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
            ulong mask = (1ul << bits) - 1;
            Value = value & mask;
            Bits = bits;
        }

        public override string ToString()
        {
            return $"0x{Value:X}<<{Bits}>>";
        }

        public bool Signed => (Value & (1ul << (Bits-1))) != 0;

        public ConstValue SignedResized(byte toBits)
        {
            if(toBits <= Bits)
                return new ConstValue(Value, toBits);

            var ext = Signed ? ulong.MaxValue << (toBits-1) : 0;
            return new ConstValue(Value | ext, toBits);
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
        public readonly byte ToBits;

        public UnsignedCastInsn(IMicroArg to, IMicroArg from) : base(MicroOpcode.UResize, to, from)
        {
            _fromBits = from.Bits;
            ToBits = to.Bits;
        }

        public override string ToString()
        {
            return base.ToString() + $" <<{_fromBits} -> {ToBits}>>";
        }
    }

    public class SignedCastInsn : MicroInsn
    {
        private readonly byte _fromBits;
        private readonly byte _toBits;

        public SignedCastInsn(IMicroArg to, IMicroArg from) : base(MicroOpcode.SResize, to, from)
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
        public readonly List<MicroInsn> Insns = new List<MicroInsn>();
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
