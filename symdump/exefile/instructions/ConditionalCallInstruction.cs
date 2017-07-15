using symdump.exefile.operands;

namespace symdump.exefile.instructions
{
    public class ConditionalCallInstruction : ConditionalBranchInstruction
    {
        public ConditionalCallInstruction(Operator @operator, IOperand lhs, IOperand rhs, IOperand target) : base(@operator, lhs, rhs, target)
        {
        }
    }
}