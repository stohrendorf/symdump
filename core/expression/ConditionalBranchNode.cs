using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace core.expression
{
    public class ConditionalBranchNode : IExpressionNode
    {
        [NotNull] public readonly IExpressionNode condition;
        [NotNull] public readonly NamedMemoryLayout target;

        public IEnumerable<int> usedRegisters => condition.usedRegisters;
        public IEnumerable<int> usedStack => condition.usedStack;
        public IEnumerable<uint> usedMemory => Enumerable.Repeat(target.address, 1);

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
