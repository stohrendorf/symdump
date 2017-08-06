using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace core.expression
{
    public class ConditionalBranchNode : IExpressionNode
    {
        [NotNull] public readonly IExpressionNode Condition;
        [NotNull] public readonly NamedMemoryLayout Target;

        public IEnumerable<int> UsedRegisters => Condition.UsedRegisters;
        public IEnumerable<int> UsedStack => Condition.UsedStack;
        public IEnumerable<uint> UsedMemory => Enumerable.Repeat(Target.Address, 1);

        public ConditionalBranchNode(Operator @operator, [NotNull] IExpressionNode lhs, [NotNull] IExpressionNode rhs,
            NamedMemoryLayout target)
        {
            Condition = new ExpressionNode(@operator, lhs, rhs);
            Target = target;
        }

        public string ToCode()
        {
            return $"if({Condition.ToCode()}) goto {Target.ToCode()}";
        }

        public override string ToString()
        {
            return ToCode();
        }
    }
}
