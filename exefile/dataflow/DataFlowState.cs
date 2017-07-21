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
                m_registers[(Register) param.Key] = new NamedMemoryLayout(p.name, p.memoryLayout);
            }
            
            dumpState();
        }

        public bool process(Instruction insn, Instruction nextInsn)
        {
            if (insn is NopInstruction)
                return true;

            if (nextInsn != null && nextInsn.isBranchDelaySlot)
                process(nextInsn, null);

            //Console.WriteLine("[eval] " + insn.asReadable());

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
                Console.WriteLine(((ConditionalBranchInstruction) insn).toExpressionNode(this).toCode());
            }
            else
            {
                dumpState();
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
                Debug.Assert(ofs % 4 == 0);
                // FIXME: If ofs exceeds the stack frame size, assume it's a parameter...
                Debug.Assert(ofs >= 0 && ofs / 4 < m_stack.Count);
                m_stack[ofs / 4] = insn.src.toExpressionNode(this);
            }
            else
            {
                dumpState();
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
                dumpState();
                Console.WriteLine(arith.toExpressionNode(this).toCode());
            }
            return true;
        }

        private bool pocess(CallPtrInstruction insn)
        {
            if (insn.returnAddressTarget != null)
            {
                dumpState();

                if (insn.target is LabelOperand)
                {
                    var fn = debugSource.findFunction(((LabelOperand) insn.target).label);
                    if (fn != null)
                    {
                        Console.WriteLine("// " + fn.getSignature());
                        Console.WriteLine(insn.asReadable());
                        return true;
                    }
                }
                
                Console.WriteLine(insn.asReadable());
                m_registers.Remove(Register.a0);
                return true;
            }

            if (insn.target is RegisterOperand && ((RegisterOperand) insn.target).register == Register.ra)
                Console.WriteLine("return");
            else
                Console.WriteLine("[jmp] " + insn.asReadable());
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
            m_registers.TryGetValue((Register)registerId, out expression);
            return expression;
        }
    }
}
