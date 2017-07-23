using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using core;
using core.expression;
using mips.disasm;
using mips.instructions;
using mips.operands;

namespace exefile.dataflow
{
    public class DataFlowState : IDataFlowState
    {
        private readonly SortedDictionary<Register, IExpressionNode> m_registers =
            new SortedDictionary<Register, IExpressionNode>();

        private readonly List<IExpressionNode> m_stack = new List<IExpressionNode>();

        public IDebugSource debugSource { get; }

        public DataFlowState(IDebugSource debugSource, IFunction func)
        {
            this.debugSource = debugSource;

            if (func == null)
                return;

            foreach (var param in func.registerParameters)
            {
                var p = param.Value;
                m_registers[(Register) param.Key] = new NamedMemoryLayout(p.name, 0, p.memoryLayout);
            }

#if TRACE_DATAFLOW_EVAL
            dumpState();
#endif
        }

        public bool process(Instruction insn, Instruction nextInsn)
        {
            if (insn is NopInstruction)
            {
#if TRACE_DATAFLOW_EVAL
                Console.WriteLine("[eval] " + insn.asReadable());
#endif
                return true;
            }

            IExpressionNode condition = null;
            if (insn is ConditionalBranchInstruction)
            {
                var cbi = (ConditionalBranchInstruction) insn;
                condition = (ConditionalBranchNode) cbi.toExpressionNode(this);
            }

            if (nextInsn != null && nextInsn.isBranchDelaySlot)
                process(nextInsn, null);

#if TRACE_DATAFLOW_EVAL
            Console.WriteLine("[eval] " + insn.asReadable());
#endif

            if (insn is CallPtrInstruction)
            {
                return pocess((CallPtrInstruction) insn);
            }
            else if (insn is ArithmeticInstruction)
            {
                return process((ArithmeticInstruction) insn);
            }
            else if (insn is DataCopyInstruction)
            {
                var copy = (DataCopyInstruction) insn;
                process(copy);
                return true;
            }
            else if (insn is ConditionalBranchInstruction)
            {
                Debug.Assert(condition != null);
                Console.WriteLine(condition.toCode());
            }
            else
            {
#if TRACE_DATAFLOW_EVAL
                dumpState();
#endif
                Console.WriteLine("[raw] " + insn.asReadable());
                m_registers.Clear();
            }

            return true;
        }

        private void process(DataCopyInstruction insn)
        {
            var copyTo = insn.dst;
            if (copyTo is RegisterOperand)
            {
                m_registers[((RegisterOperand) copyTo).register] = insn.src.toExpressionNode(this);
            }
            else if (copyTo is RegisterOffsetOperand && ((RegisterOffsetOperand) copyTo).register == Register.sp)
            {
                var ofs = ((RegisterOffsetOperand) copyTo).offset;
                // FIXME: handle non-dword data
                Debug.Assert(ofs % 4 == 0);
                // FIXME: If ofs is <0, assume it's a parameter...
                Debug.Assert(ofs >= 0 && ofs / 4 < m_stack.Count);
                m_stack[ofs / 4] = insn.src.toExpressionNode(this);
            }
            else
            {
#if TRACE_DATAFLOW_EVAL
                dumpState();
#endif
                Console.WriteLine(insn.toExpressionNode(this).toCode());
            }
        }

        private bool process(ArithmeticInstruction arith)
        {
            var dst = arith.destination;
            if (dst is RegisterOperand)
            {
                var reg = (RegisterOperand) dst;
                m_registers[reg.register] = arith.toExpressionNode(this);
                if (reg.register != Register.sp || !arith.isInplace || !(arith.rhs is ImmediateOperand))
                    return true;

                // stack frame size change
                var delta = (int) ((ImmediateOperand) arith.rhs).value;
                Debug.Assert(delta % 4 == 0);
                delta /= 4;
                if (arith.@operator == Operator.Sub)
                    delta = -delta;

                if (delta > 0)
                {
                    m_stack.RemoveRange(0, delta);
                }
                else if (delta < 0)
                {
                    for (; delta < 0; ++delta)
                        m_stack.Insert(0, null);
                }
            }
            else if (dst is RegisterOffsetOperand && ((RegisterOffsetOperand) dst).register == Register.sp)
            {
                var ofs = ((RegisterOffsetOperand) dst).offset;
                Debug.Assert(ofs % 4 == 0);
                m_stack[ofs / 4] = arith.toExpressionNode(this);
            }
            else
            {
#if TRACE_DATAFLOW_EVAL
                dumpState();
#endif
                Console.WriteLine(arith.toExpressionNode(this).toCode());
            }
            return true;
        }

        private bool pocess(CallPtrInstruction insn)
        {
            if (insn.returnAddressTarget != null)
            {
#if TRACE_DATAFLOW_EVAL
                dumpState();
#endif

                if (insn.target is LabelOperand)
                {
                    var fn = debugSource.findFunction(((LabelOperand) insn.target).label);
                    if (fn != null)
                    {
                        Console.WriteLine("// " + fn.getSignature());
                        // piece together the parameters
                        var parameters = new List<string>();
                        foreach (var p in fn.registerParameters)
                        {
                            IExpressionNode tmp;
                            if (m_registers.TryGetValue((Register) p.Key, out tmp))
                                parameters.Add(tmp.toCode());
                            else
                                parameters.Add("__UNKNOWN__");
                        }
                        foreach (var p in fn.stackParameters)
                        {
                            // TODO check
                            parameters.Add(m_stack[p.Key / 4].toCode());
                        }

                        if (!fn.getSignature().StartsWith("void ")) // TODO this is ugly
                        {
                            Console.WriteLine($"ret = {fn.name}({string.Join(", ", parameters)})");
                            m_registers[Register.v0] = new NamedMemoryLayout("ret", 0, fn.returnType);
                        }
                        else
                        {
                            Console.WriteLine($"{fn.name}({string.Join(", ", parameters)})");
                        }
                        return true;
                    }
                }

                Console.WriteLine(insn.asReadable());
                m_registers.Remove(Register.v0);
                return true;
            }

            if (insn.target is RegisterOperand && ((RegisterOperand) insn.target).register == Register.ra)
            {
                Console.WriteLine("return");
            }
            else
            {
#if TRACE_DATAFLOW_EVAL
                dumpState();
#endif
                Console.WriteLine("[jmp] " + insn.asReadable());
            }
            return false;
        }

        public void dumpState()
        {
            var regDump = string.Join("; ", m_registers.Select(reg => reg.Key + " = " + reg.Value.toCode()));
            var sel = Enumerable
                .Range(0, m_stack.Count)
                .Where(i => m_stack[i] != null)
                .Select(i => "sp[" + (i * 4) + "] = " + m_stack[i].toCode());
            var stackDump = string.Join("; ", sel);
            Console.WriteLine("    # Registers: " + regDump);
            Console.WriteLine("    # Stack: " + stackDump);
        }

        public IExpressionNode getRegisterExpression(int registerId)
        {
            IExpressionNode expression;
            m_registers.TryGetValue((Register) registerId, out expression);
            return expression;
        }
    }
}
