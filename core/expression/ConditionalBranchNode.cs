using JetBrains.Annotations;

namespace core.expression
{
    public class ConditionalBranchNode : IExpressionNode
    {
        public readonly IExpressionNode condition; 
        [NotNull] public readonly NamedMemoryLayout target;

        public ConditionalBranchNode(Operator @operator, [NotNull] IExpressionNode lhs, [NotNull] IExpressionNode rhs,
            NamedMemoryLayout target)
        {
            condition = new ExpressionNode(@operator, lhs, rhs);
            this.target = target;
        }

        public string toCode()
        {
            return $"if({condition.toCode()}) goto {target.toCode()}";
        }

        public override string ToString()
        {
            return toCode();
        }
    }
}
