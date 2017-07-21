using core.util;

namespace core.expression
{
    public class ExpressionNode : IExpressionNode
    {
        public Operator @operator { get; }
        public readonly IExpressionNode lhs;
        public readonly IExpressionNode rhs;

        public ExpressionNode(Operator @operator, IExpressionNode lhs, IExpressionNode rhs)
        {
            this.@operator = @operator;
            this.lhs = lhs;
            this.rhs = rhs;
        }

        public string toCode()
        {
            var lhsCode = lhs.toCode();
            var rhsCode = rhs.toCode();

            var selfPrecedence = @operator.getPrecedence(false);
            if (selfPrecedence > (lhs as ExpressionNode)?.@operator.getPrecedence(false))
                lhsCode = $"({lhsCode})";
            
            if (selfPrecedence > (rhs as ExpressionNode)?.@operator.getPrecedence(false))
                rhsCode = $"({rhsCode})";

            return $"{lhsCode} {@operator.asCode()} {rhsCode}";
        }

        public string tryDeref()
        {
            if (@operator != Operator.Add || !(lhs is NamedMemoryLayout) || !(rhs is ValueNode))
                return null;

            var memoryLayout = ((NamedMemoryLayout) lhs).memoryLayout;
            if (memoryLayout == null)
                return null;
            
            var member = memoryLayout.getAccessPathTo(
                (uint) ((ValueNode)rhs).value
            );
            return ((NamedMemoryLayout) lhs).label + "->" + member;
        }
    }
}
