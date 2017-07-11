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
            var lhsCode = lhs.toCode();
            var rhsCode = rhs.toCode();

            var selfPrecedence = operation.getPrecedence();
            if (lhs is ExpressionNode)
            {
                if (selfPrecedence > ((ExpressionNode) lhs).operation.getPrecedence())
                    lhsCode = $"({lhsCode})";
            }
            
            if (rhs is ExpressionNode)
            {
                if (selfPrecedence > ((ExpressionNode) rhs).operation.getPrecedence())
                    rhsCode = $"({rhsCode})";
            }

            return $"{lhsCode} {operation.toCode()} {rhsCode}";
        }
    }
}