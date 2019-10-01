using JetBrains.Annotations;
using symdump.exefile.operands;

namespace symdump.exefile.instructions
{
    public class ConditionalCallInstruction : ConditionalBranchInstruction
    {
        public ConditionalCallInstruction(BoolOperation boolOperation, [NotNull] IOperand lhs, [NotNull] IOperand rhs,
            [NotNull] IOperand target) :
            base(boolOperation, lhs, rhs, target)
        {
        }
    }
}