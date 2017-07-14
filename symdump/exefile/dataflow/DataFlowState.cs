using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using symdump.exefile.expression;
using symdump.exefile.instructions;
using symdump.exefile.operands;
using symdump.symfile;

namespace symdump.exefile.dataflow
{
    public class DataFlowState
    {
        private readonly SortedDictionary<Register, IExpressionNode> m_registers =
            new SortedDictionary<Register, IExpressionNode>();

        private readonly List<IExpressionNode> m_stack = new List<IExpressionNode>();

        private readonly SymFile m_symFile;

        public DataFlowState(SymFile symFile, Function func)
        {
            m_symFile = symFile;

            if (func == null)
                return;
            
            foreach (var param in func.registerParameters)
            {
                if (param.Value.Count > 1)
                    continue;
                
                m_registers[param.Key] = new LabelNode(param.Value.First().name);
            }
            
            dumpState();
        }

        public bool process(Instruction insn, Instruction nextInsn)
        {
            if (insn is NopInstruction)
                return true;

            //Console.WriteLine("[eval] " + insn.asReadable());

            if (nextInsn != null && nextInsn.isBranchDelaySlot)
                process(nextInsn, null);

            if (insn is CallPtrInstruction)
            {
                return pocess((CallPtrInstruction) insn, nextInsn);
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
                process(nextInsn, null);
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
            var copyTo = insn.to;
            if (copyTo is RegisterOperand)
            {
                m_registers[((RegisterOperand) copyTo).register] = insn.from.toExpressionNode(this);
            }
            else if (copyTo is RegisterOffsetOperand && ((RegisterOffsetOperand) copyTo).register == Register.sp)
            {
                var ofs = ((RegisterOffsetOperand) copyTo).offset;
                Debug.Assert(ofs % 4 == 0);
                // FIXME: If ofs exceeds the stack frame size, assume it's a parameter...
                Debug.Assert(ofs >= 0 && ofs / 4 < m_stack.Count);
                m_stack[ofs / 4] = insn.from.toExpressionNode(this);
            }
            else
            {
                dumpState();
                Console.WriteLine("[ram access] " + insn.toExpressionNode(this).toCode());
                m_registers.Clear();
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
                if (arith.operation == Operation.Sub)
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
                Console.WriteLine("[ram access] " + arith.toExpressionNode(this).toCode());
            }
            return true;
        }

        private bool pocess(CallPtrInstruction insn, Instruction nextInsn)
        {
            if (insn.returnAddressTarget != null)
            {
                dumpState();

                if (insn.target is LabelOperand)
                {
                    var fn = m_symFile.findFunction(((LabelOperand) insn.target).label);
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

            Debug.Assert(nextInsn != null);
            process(nextInsn, null);
            if (insn.target is RegisterOperand && ((RegisterOperand) insn.target).register == Register.ra)
                Console.WriteLine("return");
            else
                Console.WriteLine("[jmp] " + insn.asReadable());
            return false;
        }

        private void dumpState()
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

        public IExpressionNode getRegisterExpression(Register register)
        {
            IExpressionNode expression;
            m_registers.TryGetValue(register, out expression);
            return expression;
        }
    }
}