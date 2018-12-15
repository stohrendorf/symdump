using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace core.microcode
{
    public delegate bool Peephole1Delegate(IDebugSource debugSource, IList<MicroInsn> insns, MicroInsn insn);

    public delegate bool Peephole2Delegate(IDebugSource debugSource, IList<MicroInsn> insns, MicroInsn insn1,
        MicroInsn insn2);

    internal static class PeepholeOptimizer
    {
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
                if (!insn.Is(MicroOpcode.Add, MicroOpcode.Sub).AnyArg().AnyArg().Arg<ConstValue>(out var c0) ||
                    c0.Value != 0)
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
                if (!insn.Is(MicroOpcode.UResize).Arg<Deref>(out var a0).Arg<ConstValue>(out var c0))
                    return false;

                insns.Add(new CopyInsn(a0, new ConstValue(c0.Value, ((UnsignedCastInsn) insn).ToBits)));
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
                if (!insn.Is(MicroOpcode.SResize).Arg<Deref>(out var a0).Arg<ConstValue>(out var c0))
                    return false;

                insns.Add(new CopyInsn(a0, c0.SignedResized(((UnsignedCastInsn) insn).ToBits)));
                return true;
            }
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
                // -> copy r1, *[[c0+m1]]
                var addr = c0.Value + (ulong) m1.Offset;
                insns.Add(insn1);
                insns.Add(new CopyInsn(insn2.Args[0],
                    new AddressValue(addr, debugSource.GetSymbolName((uint) addr)).Deref(m1.Bits)
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
                // -> copy r0, *[[const+m1]]
                var addr = c0.Value + (ulong) m1.Offset;
                insns.Add(new CopyInsn(insn1.Args[0],
                    new AddressValue(addr, debugSource.GetSymbolName((uint) addr)).Deref(m1.Bits)
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
                // -> copy *[[r0 + c0]], x
                insns.Add(insn1);
                var addr = c0.Value + (ulong) m0.Offset;
                insns.Add(new CopyInsn(
                    new AddressValue(addr, debugSource.GetSymbolName((uint) addr)).Deref(m0.Bits),
                    insn2.Args[1]
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
                    !insn2.Is(MicroOpcode.Sub, MicroOpcode.Add, MicroOpcode.And, MicroOpcode.Or, MicroOpcode.XOr,
                        MicroOpcode.SHL, MicroOpcode.SRL, MicroOpcode.SRA).AnyArg().ArgRegIs(r0).ArgRegIsNot(r0))
                    return false;

                // copy r0, a
                // <op> r0, r0, b
                // -> <op> r0, a, b
                insns.Add(new MicroInsn(insn2.Opcode, r0, insn1.Args[1], insn2.Args[2]));
                return true;
            }
        };

        private static bool PropagateConstants(IDebugSource debugSource, [NotNull] MicroInsn insn,
            IDictionary<uint, ConstValue> registerValues)
        {
            var substituted = false;

            for (var i = 0; i < insn.Args.Count; ++i)
            {
                ConstValue substC;

                switch (insn.Args[i])
                {
                    case RegisterArg regArg when (i > 0 || insn.Opcode == MicroOpcode.DynamicJmp ||
                                                  insn.Opcode == MicroOpcode.JmpIf || insn.Opcode == MicroOpcode.Jmp)
                                                 && registerValues.TryGetValue(regArg.Register, out substC):
                        insn.Args[i] = substC;
                        substituted = true;
                        break;
                    case RegisterMemArg regMemArg when registerValues.TryGetValue(regMemArg.Register, out substC):
                    {
                        var addr = substC.Value + (ulong) regMemArg.Offset;
                        var av = new AddressValue(addr, debugSource.GetSymbolName((uint) addr));
                        if (insn.Opcode != MicroOpcode.Copy && insn.Opcode != MicroOpcode.UResize &&
                            insn.Opcode != MicroOpcode.SResize)
                            insn.Args[i] = av;
                        else
                            insn.Args[i] = av.Deref(regMemArg.Bits);
                        substituted = true;
                        break;
                    }
                }
            }

            if (insn.Opcode == MicroOpcode.Call || insn.Opcode == MicroOpcode.Syscall)
            {
                registerValues.Clear();
                return substituted;
            }

            if (!(insn.Args[0] is RegisterArg r))
                return substituted;

            if (insn.Args.Count < 2 || !(insn.Args[1] is ConstValue c))
            {
                registerValues.Remove(r.Register);
                return substituted;
            }

            if (insn.Opcode == MicroOpcode.Copy)
                registerValues[r.Register] = c;
            else if (insn.Opcode == MicroOpcode.UResize)
                registerValues[r.Register] = new ConstValue(c.Value, ((UnsignedCastInsn) insn).ToBits);
            else if (insn.Opcode == MicroOpcode.SResize)
                registerValues[r.Register] = c.SignedResized(((SignedCastInsn) insn).ToBits);
            else
                registerValues.Remove(r.Register);

            return substituted;
        }

        public static void Optimize(List<MicroInsn> insns, IDebugSource debugSource,
            IEnumerable<Peephole1Delegate> customPeephole1, IEnumerable<Peephole2Delegate> customPeephole2)
        {
            while (OptimizePass(insns, debugSource, customPeephole1, customPeephole2)) DeadWriteRemoval(insns);
        }

        private static void PropagateConstants(List<MicroInsn> insns, IDebugSource debugSource)
        {
            var tmp = insns.Where(i => i.Opcode != MicroOpcode.Nop).ToList();
            insns.Clear();

            IDictionary<uint, ConstValue> registerValues = new Dictionary<uint, ConstValue>();

            foreach (var insn in tmp) PropagateConstants(debugSource, insn, registerValues);

            DeadWriteRemoval(insns);
        }

        private static bool OptimizePass(IList<MicroInsn> insns, IDebugSource debugSource,
            IEnumerable<Peephole1Delegate> customPeephole1, IEnumerable<Peephole2Delegate> customPeephole2)
        {
            var tmp = insns.Where(i => i.Opcode != MicroOpcode.Nop).ToList();
            insns.Clear();

            IDictionary<uint, ConstValue> registerValues = new Dictionary<uint, ConstValue>();
            var optimizedAny = false;

            for (var i = 0; i < tmp.Count; ++i)
            {
                var insn1 = tmp[i];
                optimizedAny |= PropagateConstants(debugSource, insn1, registerValues);

                var replaced = peephole1.Concat(customPeephole1 ?? Enumerable.Empty<Peephole1Delegate>())
                    .Any(po => po(debugSource, insns, insn1));

                if (!replaced && i < tmp.Count - 1)
                {
                    var insn2 = tmp[i + 1];
                    var tmpRegVal = new Dictionary<uint, ConstValue>(registerValues);
                    optimizedAny |= PropagateConstants(debugSource, insn2, tmpRegVal);
                    if (peephole2.Concat(customPeephole2 ?? Enumerable.Empty<Peephole2Delegate>())
                        .Any(po => po(debugSource, insns, insn1, insn2)))
                    {
                        replaced = true;
                        i += 1;
                        registerValues = tmpRegVal;
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

            var seenUndefinedCall = false;

            foreach (var insn in reversedInsns)
            {
                if (seenUndefinedCall)
                {
                    insns.Add(insn);
                    continue;
                }

                switch (insn.Opcode)
                {
                    case MicroOpcode.Jmp:
                    case MicroOpcode.JmpIf:
                    case MicroOpcode.DynamicJmp:
                    {
                        // read-only instructions
                        foreach (var arg in insn.Args)
                            switch (arg)
                            {
                                case RegisterArg r:
                                    registerUsed[r.Register] = true;
                                    break;
                                case RegisterMemArg rm:
                                    registerUsed[rm.Register] = true;
                                    break;
                            }

                        insns.Add(insn);

                        break;
                    }
                    case MicroOpcode.Call:
                    case MicroOpcode.Syscall:
                        seenUndefinedCall = true; // assume anything is used
                        insns.Add(insn);
                        break;
                    default:
                    {
                        var locallyRead = new HashSet<uint>();

                        // process read registers first
                        for (var i = 1; i < insn.Args.Count; i++)
                        {
                            var arg = insn.Args[i];
                            switch (arg)
                            {
                                case RegisterArg r:
                                    locallyRead.Add(r.Register);
                                    break;
                                case RegisterMemArg rm:
                                    locallyRead.Add(rm.Register);
                                    break;
                            }
                        }

                        {
                            // check if we can discard this instruction because it's doing a redundant write
                            var keep = true;
                            uint? written = null;
                            if (insn.Args[0] is RegisterArg r)
                            {
                                written = r.Register;
                                if (!locallyRead.Contains(r.Register) &&
                                    registerUsed.TryGetValue(r.Register, out var isRead) && !isRead)
                                    keep = false;
                            }

                            if (keep)
                            {
                                insns.Add(insn);
                                if (written.HasValue) registerUsed[written.Value] = false;

                                foreach (var lr in locallyRead) registerUsed[lr] = true;
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