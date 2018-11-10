using System;
using System.Collections.Generic;
using System.Linq;

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
        /// <summary>
        /// Size of this argument.
        /// </summary>
        byte Bits { get; }

        /// <summary>
        /// Base register used (if any).
        /// </summary>
        uint? Register { get; }
    }

    public class ConstValue : IMicroArg
    {
        public byte Bits { get; }
        public uint? Register => null;

        public readonly ulong Value;

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

        public AddressValue(ulong value, string name, byte bits) : base(value, bits)
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
        public byte Bits { get; }
        public uint? Register { get; }

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
        public uint? Register { get; }

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
        public MicroOpcode Opcode;
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

        public CopyInsn(IMicroArg dest, IMicroArg src) : base(MicroOpcode.Copy, dest, src)
        {
            if (dest.Bits != src.Bits)
                throw new ArgumentException($"Parameter bit size mismatch (dest={dest.Bits} vs. src={src.Bits})");

            Bits = dest.Bits;
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

        public UnsignedCastInsn(RegisterArg from, RegisterArg to) : base(MicroOpcode.UResize, to, from)
        {
            FromBits = from.Bits;
            ToBits = to.Bits;
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

        public SignedCastInsn(RegisterArg from, RegisterArg to) : base(MicroOpcode.SResize, to, from)
        {
            FromBits = from.Bits;
            ToBits = to.Bits;
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

    public class MicroAssemblyBlock
    {
        public readonly uint Address;
        public uint Size = 0;
        public IList<MicroInsn> Insns = new List<MicroInsn>();
        public IList<uint> Outs = new List<uint>();

        private static uint _regId = 1000;

        public MicroAssemblyBlock(uint address)
        {
            Address = address;
        }

        public uint GetTmpRegId()
        {
            return _regId++;
        }

        public RegisterArg GetTmpReg(byte bits)
        {
            return new RegisterArg(GetTmpRegId(), bits);
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

        private static bool IsSequence(IReadOnlyList<MicroInsn> insns, int ofs, params MicroOpcode[] types)
        {
            if (ofs + types.Length >= insns.Count)
                return false;

            for (int i = 0; i < types.Length; ++i)
            {
                if (insns[ofs + i].Opcode != types[i])
                    return false;
            }

            return true;
        }

        public void Optimize(IDebugSource debugSource)
        {
            var tmp = Insns.Where(i => i.Opcode != MicroOpcode.Nop).ToList();
            Insns.Clear();

            for (int i = 0; i < tmp.Count; ++i)
            {
                var insn = tmp[i];
                if (i < tmp.Count - 1)
                {
                    if (IsSequence(tmp, i, MicroOpcode.Copy, MicroOpcode.Copy))
                    {
                        var next = tmp[i + 1];
                        if (insn.Args[0] is RegisterArg r0 && insn.Args[1] is ConstValue c0
                                                           && next.Args[0] is RegisterArg r1 &&
                                                           r1.Register == r0.Register
                                                           && next.Args[1] is RegisterMemArg m1 &&
                                                           m1.Register == r0.Register)
                        {
                            // copy r0, const
                            // copy r0, r0(m1)
                            // -> copy r0, const+m1
                            var addr = c0.Value + (ulong) m1.Offset;
                            Add(new CopyInsn(insn.Args[0],
                                new AddressValue(addr, debugSource.GetSymbolName((uint) addr), insn.Args[0].Bits)
                            ));
                            ++i;
                            continue;
                        }
                        else if (insn.Args[0] is RegisterArg r0_1 && insn.Args[1] is ConstValue c0_1
                                                                  && next.Args[0] is RegisterArg r1_1
                                                                  && next.Args[1] is RegisterMemArg m1_1
                                                                  && m1_1.Register == r0_1.Register)
                        {
                            // copy r0, const
                            // copy r1, r0(m1)
                            // -> copy r0, const
                            // -> copy r1, const+m1
                            var addr = c0_1.Value + (ulong) m1_1.Offset;
                            Add(insn);
                            Add(new CopyInsn(next.Args[0],
                                new AddressValue(addr, debugSource.GetSymbolName((uint) addr), next.Args[0].Bits)
                            ));
                            ++i;
                            continue;
                        }
                    }
                    else if (IsSequence(tmp, i, MicroOpcode.Copy, MicroOpcode.Add))
                    {
                        var next = tmp[i + 1];
                        if (insn.Args[0] is RegisterArg r0 && insn.Args[1] is ConstValue c0
                                                           && next.Args[1] is RegisterArg r1 &&
                                                           r1.Register == r0.Register
                                                           && next.Args[2] is ConstValue c1)
                        {
                            // copy r0, const
                            // add x, r0, const
                            // -> copy x, const+const
                            var val = c0.Value + c1.Value;
                            Add(new CopyInsn(insn.Args[0], new ConstValue(val, 32)));
                            ++i;
                            continue;
                        }
                    }
                }

                Add(insn);
            }
        }
    }
}
