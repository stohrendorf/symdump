using symdump.exefile.instructions;

namespace symdump.exefile.expression
{
    public class ExpressionNode : IExpressionNode
    {
        public Operation operation { get; }
        public readonly IExpressionNode lhs;
        public readonly IExpressionNode rhs;

        public ExpressionNode(Operation operation, IExpressionNode lhs, IExpressionNode rhs)
        {
            this.operation = operation;
            this.lhs = lhs;
            this.rhs = rhs;
        }

        public string toCode()
        {
            return $"({lhs.toCode()}) {operation.toCode()} ({rhs.toCode()})";
        }
    }
}