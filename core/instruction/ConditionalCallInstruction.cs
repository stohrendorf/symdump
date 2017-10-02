namespace core.instruction
{
    public class ConditionalCallInstruction : ConditionalBranchInstruction
    {
        public ConditionalCallInstruction(Operator @operator, IOperand lhs, IOperand rhs, IOperand target)
            : base(@operator, lhs, rhs, target)
        {
        }
    }
}
