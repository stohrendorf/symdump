using symdump.exefile.operands;

namespace symdump.exefile.instructions
{
    public class ConditionalCallInstruction : ConditionalBranchInstruction
    {
        public ConditionalCallInstruction(BoolOperation boolOperation, IOperand lhs, IOperand? rhs,
            IOperand? target) :
            base(boolOperation, lhs, rhs, target)
        {
        }
    }
}
