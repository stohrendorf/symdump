using System;
using System.Collections.Generic;
using System.Diagnostics;
using symdump.exefile.expression;
using symdump.exefile.instructions;
using symdump.exefile.operands;
using symdump.symfile;

namespace symdump.exefile.dataflow
{
    public class DataFlowState
    {
        private readonly SortedDictionary<Register, IExpressionNode> registers = new SortedDictionary<Register, IExpressionNode>();

        public bool process(Instruction insn, Instruction nextInsn)
        {
            if (insn is NopInstruction)
                return true;
            
            if(nextInsn != null && nextInsn.isBranchDelaySlot)
                process(nextInsn, null);
            
            if (insn is CallPtrInstruction)
            {
                var i = (CallPtrInstruction) insn;
                if (i.returnAddressTarget != null)
                {
                    Console.WriteLine("[raw] " + insn.asReadable());
                    return true;
                }

                Debug.Assert(nextInsn != null);
                process(nextInsn, null);
                Console.WriteLine("return");
                return false;
            }
            else if (insn is ArithmeticInstruction)
            {
                var dst = ((ArithmeticInstruction) insn).destination;
                if (dst is RegisterOperand)
                {
                    registers[((RegisterOperand) dst).register] = insn.toExpressionNode(registers);
                }
                else
                {
                    var regs = dumpRegisters();
                    Console.WriteLine("[ram access] " + insn.toExpressionNode(regs).toCode());
                }
            }
            else if (insn is DataCopyInstruction)
            {
                var dst = ((DataCopyInstruction) insn).to;
                if (dst is RegisterOperand)
                {
                    registers[((RegisterOperand) dst).register] = ((DataCopyInstruction) insn).from.toExpressionNode(registers);
                }
                else
                {
                    var regs = dumpRegisters();
                    Console.WriteLine("[ram access] " + insn.toExpressionNode(regs).toCode());
                }
            }
            else
            {
                dumpRegisters();
                
                Console.WriteLine("[raw] " + insn.asReadable());
            }

            return true;
        }

        private SortedDictionary<Register, IExpressionNode> dumpRegisters()
        {
            foreach (var regExpr in registers)
            {
                Console.WriteLine("    # " + regExpr.Key + " = " + regExpr.Value.toCode());
            }

            var regs = registers;
            registers.Clear();
            return regs;
        }
    }
}