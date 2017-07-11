using System.Collections.Generic;
using symdump.exefile.expression;
using symdump.exefile.operands;
using symdump.symfile;

namespace symdump.exefile.instructions
{
    public abstract class Instruction
    {
        public bool isBranchDelaySlot;

        protected Instruction(bool isBranchDelaySlot = false)
        {
            this.isBranchDelaySlot = isBranchDelaySlot;
        }

        public abstract IOperand[] operands { get; }

        public abstract string asReadable();

        public abstract IExpressionNode toExpressionNode(IReadOnlyDictionary<Register, IExpressionNode> registers);
    }
}