using core.util;
using JetBrains.Annotations;

namespace core.expression
{
    public class ConditionalBranchNode : IExpressionNode
    {
        public Operator @operator { get; }
        [NotNull] public readonly IExpressionNode lhs;
        [NotNull] public readonly IExpressionNode rhs;
        [NotNull] public readonly NamedMemoryLayout target;

        public ConditionalBranchNode(Operator @operator, [NotNull] IExpressionNode lhs, [NotNull] IExpressionNode rhs,
            NamedMemoryLayout target)
        {
            this.@operator = @operator;
            this.lhs = lhs;
            this.rhs = rhs;
            this.target = target;
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

            return $"if({lhsCode} {@operator.asCode()} {rhsCode}) goto {target.toCode()}";
        }
    }
}