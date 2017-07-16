using core;
using core.expression;

namespace mips.instructions
{
    public class ConditionalBranchInstruction : Instruction
    {
        public readonly Operator @operator;

        public ConditionalBranchInstruction(Operator @operator, IOperand lhs, IOperand rhs, IOperand target)
        {
            this.@operator = @operator;
            operands = new[] {lhs, rhs, target};
        }

        public IOperand lhs => operands[0];
        public IOperand rhs => operands[1];
        public IOperand target => operands[2];

        public override IOperand[] operands { get; }

        public override string asReadable()
        {
            var op = @operator.toCode();

            return $"if({lhs} {op} {rhs}) goto {target}";
        }

        public override IExpressionNode toExpressionNode(IDataFlowState dataFlowState)
        {
            return new ConditionalBranchNode(@operator, lhs.toExpressionNode(dataFlowState), rhs.toExpressionNode(dataFlowState), target.toExpressionNode(dataFlowState) as LabelNode);
        }
    }
}
