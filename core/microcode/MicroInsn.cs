using System;
using System.Collections.Generic;

namespace core.microcode
{
    public enum MicroOpcode
    {
        ERROR,
        Nop,
        Data,
        Call,
        Syscall,
        Return,
        Jmp,
        DynamicJmp,
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
        SMul,
        UMul,
        SDiv,
        UDiv,
        SMod,
        UMod,
        Copy,
        UResize,
        SResize
    }

    public class MicroInsn
    {
        public readonly IList<IMicroArg> Args;
        public readonly MicroOpcode Opcode;

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

    public sealed class CopyInsn : MicroInsn
    {
        private readonly byte _bits;

        public CopyInsn(IMicroArg dest, IMicroArg src) : base(MicroOpcode.Copy, dest, src)
        {
            if (dest.Bits != src.Bits)
                throw new ArgumentException($"Parameter bit size mismatch (dest={dest} vs. src={src})");

            _bits = dest.Bits;
        }

        public override string ToString()
        {
            return base.ToString() + $" <<{_bits}>>";
        }
    }

    public sealed class UnsupportedInsn : MicroInsn
    {
        private readonly string _mnemonic;

        public UnsupportedInsn(string mnemonic, params IMicroArg[] args) : base(MicroOpcode.ERROR, args)
        {
            _mnemonic = mnemonic;
        }

        public override string ToString()
        {
            return $"{{{_mnemonic}}} " + base.ToString();
        }
    }

    public sealed class UnsignedCastInsn : MicroInsn
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

    public sealed class SignedCastInsn : MicroInsn
    {
        private readonly byte _fromBits;
        public readonly byte ToBits;

        public SignedCastInsn(IMicroArg to, IMicroArg from) : base(MicroOpcode.SResize, to, from)
        {
            _fromBits = from.Bits;
            ToBits = to.Bits;
        }

        public override string ToString()
        {
            return base.ToString() + $" <<{_fromBits} -> {ToBits}>>";
        }
    }
}