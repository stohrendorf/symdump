using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

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

    public class AddressValue : ConstValue
    {
        private readonly string _name;

        public AddressValue(ulong value, string name, byte bits) : base(value, bits)
        {
            _name = name;
        }

        public override string ToString()
        {
            if (_name != null)
                return $"&{_name}[[0x{Value:X}]]";
            else
                return $"&<!unnamed!>[[0x{Value:X}]]";
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

        private static bool IsSequence(IReadOnlyList<MicroInsn> insns, int ofs, params MicroOpcode[] types)
        {
            if (ofs + types.Length > insns.Count)
                return false;

            for (int i = 0; i < types.Length; ++i)
            {
                if (insns[ofs + i].Opcode != types[i])
                    return false;
            }

            return true;
        }

        [NotNull]
        private static MicroInsn Optimize([NotNull] MicroInsn insn, IDictionary<uint, ConstValue> registerValues)
        {
            {
                for (int i = 1; i < insn.Args.Count; ++i)
                {
                    if (insn.Args[i] is RegisterArg regArg && registerValues.TryGetValue(regArg.Register, out var knownArg))
                    {
                        insn.Args[i] = knownArg;
                    }
                }
                if (insn.Args[0] is RegisterArg r)
                {
                    if (insn.Opcode == MicroOpcode.Copy && insn.Args[1] is ConstValue v)
                        registerValues[r.Register] = v;
                    else
                        registerValues.Remove(r.Register);
                }
            }
            
            {
                if (insn.Opcode == MicroOpcode.Add && insn.Args[1] is ConstValue c0 && insn.Args[2] is ConstValue c1)
                {
                    return new CopyInsn(insn.Args[0], new ConstValue(c0.Value + c1.Value, insn.Args[0].Bits));
                }
            }
            {
                if (insn.Opcode == MicroOpcode.Sub && insn.Args[1] is ConstValue c0 && insn.Args[2] is ConstValue c1)
                {
                    return new CopyInsn(insn.Args[0], new ConstValue(c0.Value - c1.Value, insn.Args[0].Bits));
                }
            }
            {
                if (insn.Opcode == MicroOpcode.Or && insn.Args[1] is ConstValue c0 && insn.Args[2] is ConstValue c1)
                {
                    return new CopyInsn(insn.Args[0], new ConstValue(c0.Value | c1.Value, insn.Args[0].Bits));
                }
            }
            {
                if (insn.Opcode == MicroOpcode.XOr && insn.Args[1] is ConstValue c0 && insn.Args[2] is ConstValue c1)
                {
                    return new CopyInsn(insn.Args[0], new ConstValue(c0.Value ^ c1.Value, insn.Args[0].Bits));
                }
            }
            {
                if (insn.Opcode == MicroOpcode.And && insn.Args[1] is ConstValue c0 && insn.Args[2] is ConstValue c1)
                {
                    return new CopyInsn(insn.Args[0], new ConstValue(c0.Value & c1.Value, insn.Args[0].Bits));
                }
            }
            {
                if (insn.Opcode == MicroOpcode.SHL && insn.Args[1] is ConstValue c0 && insn.Args[2] is ConstValue c1)
                {
                    return new CopyInsn(insn.Args[0], new ConstValue(c0.Value << (int) c1.Value, insn.Args[0].Bits));
                }
            }
            {
                if (insn.Opcode == MicroOpcode.SRA && insn.Args[1] is ConstValue c0 && insn.Args[2] is ConstValue c1)
                {
                    return new CopyInsn(insn.Args[0],
                        new ConstValue((ulong) ((long) c0.Value / (1 << (int) c1.Value)), insn.Args[0].Bits));
                }
            }
            {
                if (insn.Opcode == MicroOpcode.SRL && insn.Args[1] is ConstValue c0 && insn.Args[2] is ConstValue c1)
                {
                    return new CopyInsn(insn.Args[0], new ConstValue(c0.Value >> (int) c1.Value, insn.Args[0].Bits));
                }
            }

            {
                if (insn.Opcode == MicroOpcode.Add && insn.Args[2] is ConstValue c1 && c1.Value == 0)
                {
                    return new CopyInsn(insn.Args[0], insn.Args[1]);
                }
            }
            {
                if (insn.Opcode == MicroOpcode.Sub && insn.Args[2] is ConstValue c1 && c1.Value == 0)
                {
                    return new CopyInsn(insn.Args[0], insn.Args[1]);
                }
            }
            {
                if (insn.Opcode == MicroOpcode.Add && insn.Args[1] is ConstValue c1 && c1.Value == 0)
                {
                    return new CopyInsn(insn.Args[0], insn.Args[2]);
                }
            }

            return insn;
        }

        public void Optimize(IDebugSource debugSource)
        {
            while (OptimizeImpl(debugSource))
            {
                /* continue */
            }
        }

        private bool OptimizeImpl(IDebugSource debugSource)
        {
            var tmp = Insns.Where(i => i.Opcode != MicroOpcode.Nop).ToList();
            Insns.Clear();
            
            IDictionary<uint, ConstValue> registerValues = new Dictionary<uint, ConstValue>();
            bool changed = false;

            for (int i = 0; i < tmp.Count; ++i)
            {
                var insn = Optimize(tmp[i], registerValues);
                changed |= !ReferenceEquals(insn, tmp[i]);

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
                            changed = true;
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
                            changed = true;
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
                            changed = true;
                            continue;
                        }
                    }
                    else if (IsSequence(tmp, i, MicroOpcode.Copy, MicroOpcode.Cmp))
                    {
                        var next = tmp[i + 1];
                        if (insn.Args[0] is RegisterArg r0 && insn.Args[1] is ConstValue c0
                                                           && next.Args[1] is RegisterArg r1 &&
                                                           r1.Register == r0.Register)
                        {
                            // copy r0, const
                            // cmp x, r0
                            // -> copy r0, const
                            // -> cmp x, const
                            Add(insn);
                            Add(new MicroInsn(MicroOpcode.Cmp, r0, c0));
                            ++i;
                            changed = true;
                            continue;
                        }
                    }
                }

                Add(insn);
            }

            return changed;
        }
    }
}
