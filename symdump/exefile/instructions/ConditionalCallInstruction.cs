using symdump.exefile.operands;

namespace symdump.exefile.instructions
{
    public class ConditionalCallInstruction : ConditionalBranchInstruction
    {
        public ConditionalCallInstruction(Operation operation, IOperand lhs, IOperand rhs, IOperand target) : base(operation, lhs, rhs, target)
        {
        }
    }
}