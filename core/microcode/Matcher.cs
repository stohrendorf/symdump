using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;

namespace core.microcode
{
    public class ArgMatcher
    {
        private readonly MicroInsn _insn;
        private readonly int _index;

        public static readonly ArgMatcher NotMatched = new ArgMatcher(null, -1);

        public ArgMatcher(MicroInsn insn) : this(insn, 0)
        {
        }

        private ArgMatcher(MicroInsn insn, int index)
        {
            _insn = insn;
            _index = index;
        }

        public ArgMatcher AnyArg()
        {
            return Next();
        }

        public ArgMatcher Arg<T>(out T result) where T : IMicroArg
        {
            result = default(T);

            if (_insn == null || _index >= _insn.Args.Count || !(_insn.Args[_index] is T casted))
                return NotMatched;

            result = casted;
            return Next();
        }

        public ArgMatcher Arg<T>() where T : IMicroArg
        {
            return Arg<T>(out _);
        }

        public ArgMatcher ArgRegIs(RegisterArg r)
        {
            if (!Arg<RegisterArg>(out var tmp).Matches() || r.Register != tmp.Register)
                return NotMatched;
            return Next();
        }

        public ArgMatcher ArgMemRegIs(RegisterArg r)
        {
            return ArgMemRegIs(r, out _);
        }

        public ArgMatcher ArgMemRegIs(RegisterArg r, out RegisterMemArg result)
        {
            result = null;

            if (!Arg(out result) || r.Register != result.Register)
                return NotMatched;
            return Next();
        }

        private bool Matches()
        {
            return _insn != null;
        }

        public static implicit operator bool(ArgMatcher m)
        {
            return m.Matches();
        }

        private ArgMatcher Next()
        {
            return new ArgMatcher(_insn, _index + 1);
        }
    }

    public static class MatcherMixin
    {
        public static ArgMatcher Is(this MicroInsn insn, params MicroOpcode[] opcode)
        {
            Debug.Assert(opcode.Length > 0);
            if (!opcode.Contains(insn.Opcode))
                return ArgMatcher.NotMatched;

            return new ArgMatcher(insn);
        }
    }

    public static class PeepholeOptimizer
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
                if (!insn.Is(MicroOpcode.Add, MicroOpcode.Sub).AnyArg().AnyArg().Arg<ConstValue>(out var c0) ||
                    c0.Value != 0)
                    return false;

                insns.Add(new CopyInsn(insn.Args[0], insn.Args[1]));
                return true;
            },
            (debugSource, insns, insn) =>
            {
                if (!insn.Is(MicroOpcode.Sub).AnyArg().Arg<ConstValue>(out var c0) || c0.Value != 0)
                    return false;

                insns.Add(new CopyInsn(insn.Args[0], insn.Args[2]));
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
                if (!insn1.Is(MicroOpcode.Copy).Arg<RegisterArg>(out var r0).Arg<ConstValue>(out var c0) ||
                    !insn2.Is(MicroOpcode.Cmp).AnyArg().ArgRegIs(r0))
                    return false;

                // copy r0, const
                // cmp x, r0
                // -> copy r0, const
                // -> cmp x, const
                insns.Add(insn1);
                insns.Add(new MicroInsn(MicroOpcode.Cmp, insn2.Args[0], c0));
                return true;
            },
        };

        private static bool Substitute([NotNull] MicroInsn insn, IDictionary<uint, ConstValue> registerValues)
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
            }

            if (insn.Args[0] is RegisterArg r)
            {
                if (insn.Opcode == MicroOpcode.Copy && insn.Args[1] is ConstValue v)
                    registerValues[r.Register] = v;
                else
                    registerValues.Remove(r.Register);
            }

            return substituted;
        }

        public static void Optimize(IList<MicroInsn> insns, IDebugSource debugSource)
        {
            while (OptimizePass(insns, debugSource))
            {
                /* continue */
            }
        }

        private static bool OptimizePass(IList<MicroInsn> insns, IDebugSource debugSource)
        {
            var tmp = insns.Where(i => i.Opcode != MicroOpcode.Nop).ToList();
            insns.Clear();

            IDictionary<uint, ConstValue> registerValues = new Dictionary<uint, ConstValue>();
            bool optimizedAny = false;

            for (int i = 0; i < tmp.Count; ++i)
            {
                var insn1 = tmp[i];
                optimizedAny |= Substitute(insn1, registerValues);
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
                    optimizedAny |= Substitute(insn2, registerValues);
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
    }
}