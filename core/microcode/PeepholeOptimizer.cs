using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace core.microcode
{
    internal static class PeepholeOptimizer
    {
        private delegate bool Peephole1Delegate(IDebugSource debugSource, IList<MicroInsn> insns, MicroInsn insn);

        private delegate bool Peephole2Delegate(IDebugSource debugSource, IList<MicroInsn> insns, MicroInsn insn1, MicroInsn insn2);
        
        private static readonly Peephole1Delegate[] peephole1 =
        {
            (debugSource, insns, insn) =>
            {
                if (!insn.Is(MicroOpcode.Add).AnyArg().Arg<ConstValue>(out var c0).Arg<ConstValue>(out var c1))
                    return false;

                insns.Add(new CopyInsn(insn.Args[0], new ConstValue(c0.Value + c1.Value, insn.Args[0].Bits)));
                return true;
            },
            (debugSource, insns, insn) =>
            {
                if (!insn.Is(MicroOpcode.Sub).AnyArg().Arg<ConstValue>(out var c0).Arg<ConstValue>(out var c1))
                    return false;

                insns.Add(new CopyInsn(insn.Args[0], new ConstValue(c0.Value - c1.Value, insn.Args[0].Bits)));
                return true;
            },
            (debugSource, insns, insn) =>
            {
                if (!insn.Is(MicroOpcode.Or).AnyArg().Arg<ConstValue>(out var c0).Arg<ConstValue>(out var c1))
                    return false;

                insns.Add(new CopyInsn(insn.Args[0], new ConstValue(c0.Value | c1.Value, insn.Args[0].Bits)));
                return true;
            },
            (debugSource, insns, insn) =>
            {
                if (!insn.Is(MicroOpcode.XOr).AnyArg().Arg<ConstValue>(out var c0).Arg<ConstValue>(out var c1))
                    return false;

                insns.Add(new CopyInsn(insn.Args[0], new ConstValue(c0.Value ^ c1.Value, insn.Args[0].Bits)));
                return true;
            },
            (debugSource, insns, insn) =>
            {
                if (!insn.Is(MicroOpcode.And).AnyArg().Arg<ConstValue>(out var c0).Arg<ConstValue>(out var c1))
                    return false;

                insns.Add(new CopyInsn(insn.Args[0], new ConstValue(c0.Value & c1.Value, insn.Args[0].Bits)));
                return true;
            },
            (debugSource, insns, insn) =>
            {
                if (!insn.Is(MicroOpcode.SHL).AnyArg().Arg<ConstValue>(out var c0).Arg<ConstValue>(out var c1))
                    return false;

                insns.Add(new CopyInsn(insn.Args[0], new ConstValue(c0.Value << (int) c1.Value, insn.Args[0].Bits)));
                return true;
            },
            (debugSource, insns, insn) =>
            {
                if (!insn.Is(MicroOpcode.SRA).AnyArg().Arg<ConstValue>(out var c0).Arg<ConstValue>(out var c1))
                    return false;

                insns.Add(new CopyInsn(insn.Args[0],
                    new ConstValue((ulong) ((long) c0.Value / (1 << (int) c1.Value)), insn.Args[0].Bits)));
                return true;
            },
            (debugSource, insns, insn) =>
            {
                if (!insn.Is(MicroOpcode.SRL).AnyArg().Arg<ConstValue>(out var c0).Arg<ConstValue>(out var c1))
                    return false;

                insns.Add(new CopyInsn(insn.Args[0], new ConstValue(c0.Value >> (int) c1.Value, insn.Args[0].Bits)));
                return true;
            },
            (debugSource, insns, insn) =>
            {
                if (!insn.Is(MicroOpcode.Add, MicroOpcode.Sub).AnyArg().AnyArg().Arg<ConstValue>(out var c0) || c0.Value != 0)
                    return false;

                insns.Add(new CopyInsn(insn.Args[0], insn.Args[1]));
                return true;
            },
            (debugSource, insns, insn) =>
            {
                if (!insn.Is(MicroOpcode.Add).AnyArg().Arg<ConstValue>(out var c0) || c0.Value != 0)
                    return false;

                insns.Add(new CopyInsn(insn.Args[0], insn.Args[2]));
                return true;
            },
            (debugSource, insns, insn) =>
            {
                if (!insn.Is(MicroOpcode.UResize).Arg<RegisterMemArg>(out var m0).Arg<ConstValue>(out var c0))
                    return false;

                insns.Add(new CopyInsn(m0, new ConstValue(c0.Value, m0.Bits)));
                return true;
            },
            (debugSource, insns, insn) =>
            {
                if (!insn.Is(MicroOpcode.UResize).Arg<RegisterArg>(out var r0).Arg<ConstValue>(out var c0))
                    return false;

                insns.Add(new CopyInsn(r0, new ConstValue(c0.Value, r0.Bits)));
                return true;
            },
            (debugSource, insns, insn) =>
            {
                if (!insn.Is(MicroOpcode.UResize).Arg<AddressValue>(out var a0).Arg<ConstValue>(out var c0))
                    return false;

                insns.Add(new CopyInsn(a0, new ConstValue(c0.Value, ((UnsignedCastInsn)insn).ToBits)));
                return true;
            },
            (debugSource, insns, insn) =>
            {
                if (!insn.Is(MicroOpcode.SResize).Arg<RegisterMemArg>(out var m0).Arg<ConstValue>(out var c0))
                    return false;

                insns.Add(new CopyInsn(m0, c0.SignedResized(m0.Bits)));
                return true;
            },
            (debugSource, insns, insn) =>
            {
                if (!insn.Is(MicroOpcode.SResize).Arg<RegisterArg>(out var r0).Arg<ConstValue>(out var c0))
                    return false;

                insns.Add(new CopyInsn(r0, c0.SignedResized(r0.Bits)));
                return true;
            },
            (debugSource, insns, insn) =>
            {
                if (!insn.Is(MicroOpcode.SResize).Arg<AddressValue>(out var a0).Arg<ConstValue>(out var c0))
                    return false;

                insns.Add(new CopyInsn(a0, c0.SignedResized(((UnsignedCastInsn)insn).ToBits)));
                return true;
            },
        };

        private static readonly Peephole2Delegate[] peephole2 =
        {
            (debugSource, insns, insn1, insn2) =>
            {
                if (!insn1.Is(MicroOpcode.Copy).Arg<RegisterArg>(out var r0).Arg<ConstValue>(out var c0) ||
                    !insn2.Is(MicroOpcode.Copy).Arg<RegisterArg>().ArgMemRegIs(r0, out var m1))
                    return false;

                // copy r0, c0
                // copy r1, r0(m1)
                // -> copy r0, c0
                // -> copy r1, [[c0+m1]]
                var addr = c0.Value + (ulong) m1.Offset;
                insns.Add(insn1);
                insns.Add(new CopyInsn(insn2.Args[0],
                    new AddressValue(addr, debugSource.GetSymbolName((uint) addr), insn2.Args[0].Bits)
                ));
                return true;
            },
            (debugSource, insns, insn1, insn2) =>
            {
                if (!insn1.Is(MicroOpcode.Copy).Arg<RegisterArg>(out var r0).Arg<ConstValue>(out var c0) ||
                    !insn2.Is(MicroOpcode.Copy).ArgRegIs(r0).ArgMemRegIs(r0, out var m1))
                    return false;

                // copy r0, const
                // copy r0, r0(m1)
                // -> copy r0, const+m1
                var addr = c0.Value + (ulong) m1.Offset;
                insns.Add(new CopyInsn(insn1.Args[0],
                    new AddressValue(addr, debugSource.GetSymbolName((uint) addr), insn1.Args[0].Bits)
                ));
                return true;
            },
            (debugSource, insns, insn1, insn2) =>
            {
                if (!insn1.Is(MicroOpcode.Copy).Arg<RegisterArg>(out var r0).Arg<ConstValue>(out var c0) ||
                    !insn2.Is(MicroOpcode.Copy).ArgMemRegIs(r0, out var m0))
                    return false;

                // copy r0, c0
                // copy r0(c0), x
                // -> copy r0, c0
                // -> copy addr, x
                insns.Add(insn1);
                var addr = c0.Value + (ulong) m0.Offset;
                insns.Add(new CopyInsn(insn1.Args[0],
                    new AddressValue(addr, debugSource.GetSymbolName((uint) addr), insn1.Args[0].Bits)
                ));
                return true;
            },
            (debugSource, insns, insn1, insn2) =>
            {
                if (!insn1.Is(MicroOpcode.Copy).Arg<RegisterArg>(out var r0).Arg<ConstValue>(out var c0) ||
                    !insn2.Is(MicroOpcode.Add).AnyArg().ArgRegIs(r0).Arg<ConstValue>(out var c1))
                    return false;

                // copy r0, const
                // add x, r0, const
                // -> copy x, const+const
                var val = c0.Value + c1.Value;
                insns.Add(new CopyInsn(insn1.Args[0], new ConstValue(val, 32)));
                return true;
            },
            (debugSource, insns, insn1, insn2) =>
            {
                if (!insn1.Is(MicroOpcode.Copy).Arg<RegisterArg>(out var r0).AnyArg() ||
                    !insn2.Is(MicroOpcode.Copy).ArgRegIs(r0).Arg<ConstValue>())
                    return false;

                // copy r0, x
                // copy r0, c0
                // -> copy r0, c0
                insns.Add(insn2);
                return true;
            },
            (debugSource, insns, insn1, insn2) =>
            {
                if (!insn1.Is(MicroOpcode.Copy).Arg<RegisterArg>(out var r0).AnyArg() ||
                    !insn2.Is(MicroOpcode.Copy).ArgRegIs(r0).Arg<AddressValue>())
                    return false;

                // copy r0, x
                // copy r0, c0
                // -> copy r0, c0
                insns.Add(insn2);
                return true;
            },
            (debugSource, insns, insn1, insn2) =>
            {
                if (!insn1.Is(MicroOpcode.Copy).Arg<RegisterArg>(out var r0).AnyArg() ||
                    !insn2.Is(MicroOpcode.Copy).ArgRegIs(r0).Arg<RegisterMemArg>(out var m0) ||
                    m0.Register == r0.Register)
                    return false;

                // copy r0, x
                // copy r0, m0
                // -> copy r0, m0
                insns.Add(insn2);
                return true;
            },
            (debugSource, insns, insn1, insn2) =>
            {
                if (!insn1.Is(MicroOpcode.Copy).Arg<RegisterArg>(out var r0).Arg<ConstValue>(out var c0) ||
                    !insn2.Is(MicroOpcode.Not).ArgRegIs(r0))
                    return false;

                // copy r0, c0
                // not r0
                // -> copy r0, ~c0
                insns.Add(new CopyInsn(r0, new ConstValue(~c0.Value, r0.Bits)));
                return true;
            },
            (debugSource, insns, insn1, insn2) =>
            {
                if (!insn1.Is(MicroOpcode.Copy).Arg<RegisterArg>(out var r0) ||
                    !insn2.Is(MicroOpcode.Sub, MicroOpcode.Add, MicroOpcode.And, MicroOpcode.Or, MicroOpcode.XOr, MicroOpcode.SHL, MicroOpcode.SRL, MicroOpcode.SRA).AnyArg().ArgRegIs(r0).ArgRegIsNot(r0))
                    return false;

                // copy r0, a
                // <op> r0, r0, b
                // -> <op> r0, a, b
                insns.Add(new MicroInsn(insn2.Opcode, r0, insn1.Args[1], insn2.Args[2]));
                return true;
            },
        };

        private static bool Substitute(IDebugSource debugSource, [NotNull] MicroInsn insn,
            IDictionary<uint, IMicroArg> registerValues)
        {
            bool substituted = false;

            for (int i = 1; i < insn.Args.Count; ++i)
            {
                if (insn.Args[i] is RegisterArg regArg &&
                    registerValues.TryGetValue(regArg.Register, out var knownArg))
                {
                    insn.Args[i] = knownArg;
                    substituted = true;
                }
                else if (insn.Args[i] is RegisterMemArg regMemArg &&
                         registerValues.TryGetValue(regMemArg.Register, out var knownArg2) &&
                         knownArg2 is ConstValue knownArg2C)
                {
                    var addr = knownArg2C.Value + (ulong) regMemArg.Offset;
                    insn.Args[i] = new AddressValue(addr, debugSource.GetSymbolName((uint) addr), regMemArg.Bits);
                    substituted = true;
                }
            }

            if (insn.Args[0] is RegisterArg r)
            {
                if (insn.Opcode == MicroOpcode.Copy)
                {
                    if (!(insn.Args[1] is RegisterArg cr) || cr.Register != r.Register)
                        registerValues[r.Register] = insn.Args[1];
                    else
                        registerValues.Remove(r.Register);
                }
                else if (insn.Opcode == MicroOpcode.UResize && insn.Args[1] is ConstValue v2)
                {
                    registerValues[r.Register] = new ConstValue(v2.Value, ((UnsignedCastInsn) insn).ToBits);
                }
                else if (insn.Opcode == MicroOpcode.SResize && insn.Args[1] is ConstValue v3)
                {
                    registerValues[r.Register] = v3.SignedResized(((SignedCastInsn) insn).ToBits);
                }
                else
                {
                    registerValues.Remove(r.Register);
                }
            }
            else if (insn.Args[0] is AddressValue || insn.Args[0] is RegisterMemArg)
            {
                // drop all non-constants, because we're modifying memory
                Dictionary<uint, IMicroArg> tmp = new Dictionary<uint, IMicroArg>(registerValues);
                registerValues.Clear();
                foreach (var rv in tmp)
                {
                    if (rv.Value is ConstValue)
                        registerValues[rv.Key] = rv.Value;
                }
            }

            return substituted;
        }

        public static void Optimize(List<MicroInsn> insns, IDebugSource debugSource)
        {
            while (OptimizePass(insns, debugSource))
            {
                DeadWriteRemoval(insns);
            }
        }

        private static bool OptimizePass(IList<MicroInsn> insns, IDebugSource debugSource)
        {
            var tmp = insns.Where(i => i.Opcode != MicroOpcode.Nop).ToList();
            insns.Clear();

            IDictionary<uint, IMicroArg> registerValues = new Dictionary<uint, IMicroArg>();
            bool optimizedAny = false;

            for (int i = 0; i < tmp.Count; ++i)
            {
                var insn1 = tmp[i];
                optimizedAny |= Substitute(debugSource, insn1, registerValues);
                bool replaced = false;

                foreach (var po in peephole1)
                    if (po(debugSource, insns, insn1))
                    {
                        replaced = true;
                        break;
                    }


                if (!replaced && i < tmp.Count - 1)
                {
                    var insn2 = tmp[i + 1];
                    optimizedAny |= Substitute(debugSource, insn2, registerValues);
                    foreach (var po in peephole2)
                        if (po(debugSource, insns, insn1, insn2))
                        {
                            replaced = true;
                            i += 1;
                            break;
                        }
                }

                if (!replaced)
                    insns.Add(insn1);

                optimizedAny |= replaced;
            }

            return optimizedAny;
        }

        private static void DeadWriteRemoval(List<MicroInsn> insns)
        {
            var reversedInsns = insns.Where(i => i.Opcode != MicroOpcode.Nop).ToList();
            reversedInsns.Reverse();
            insns.Clear();

            var registerUsed = new Dictionary<uint, bool>(); // true if read, false if written only

            foreach (var insn in reversedInsns)
            {
                switch (insn.Opcode)
                {
                    case MicroOpcode.Jmp:
                    case MicroOpcode.JmpIf:
                    {
                        // read-only instructions
                        foreach (var arg in insn.Args)
                        {
                            if (arg is RegisterArg r)
                                registerUsed[r.Register] = true;
                            else if (arg is RegisterMemArg rm)
                                registerUsed[rm.Register] = true;
                        }
                        
                        insns.Add(insn);

                        break;
                    }
                    default:
                    {
                        var locallyRead = new HashSet<uint>();

                        // process read registers first
                        for (var i = 1; i < insn.Args.Count; i++)
                        {
                            var arg = insn.Args[i];
                            if (arg is RegisterArg r)
                                locallyRead.Add(r.Register);
                            else if (arg is RegisterMemArg rm)
                                locallyRead.Add(rm.Register);
                        }

                        {
                            // check if we can discard this instruction because it's doing a redundant write
                            bool keep = true;
                            uint? written = null;
                            if (insn.Args[0] is RegisterArg r)
                            {
                                written = r.Register;
                                if (!locallyRead.Contains(r.Register) && registerUsed.TryGetValue(r.Register, out var isRead) && !isRead)
                                    keep = false;
                            }
                            else if (insn.Args[0] is RegisterMemArg rm)
                            {
                                written = rm.Register;
                                if (!locallyRead.Contains(rm.Register) && registerUsed.TryGetValue(rm.Register, out var isRead) && !isRead)
                                    keep = false;
                            }

                            if (keep)
                            {
                                insns.Add(insn);
                                if (written.HasValue)
                                {
                                    registerUsed[written.Value] = false;
                                }
                                foreach (var lr in locallyRead)
                                {
                                    registerUsed[lr] = true;
                                }
                            }
                        }
                        
                        break;
                    }
                }
            }

            insns.Reverse();
        }
    }
}
