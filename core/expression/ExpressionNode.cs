using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using core.util;
using JetBrains.Annotations;

namespace core.expression
{
    public class ExpressionNode : IExpressionNode
    {
        public Operator @operator { get; }
        [NotNull] public readonly IExpressionNode lhs;
        [NotNull] public readonly IExpressionNode rhs;

        public IEnumerable<int> usedRegisters => lhs.usedRegisters.Concat(rhs.usedRegisters);
        public IEnumerable<int> usedStack => lhs.usedStack.Concat(rhs.usedStack);
        public IEnumerable<uint> usedMemory => lhs.usedMemory.Concat(rhs.usedMemory);

        public ExpressionNode(Operator @operator, [NotNull] IExpressionNode lhs, [NotNull] IExpressionNode rhs)
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

        [CanBeNull]
        public string tryDeref()
        {
            if (@operator != Operator.Add || !(lhs is NamedMemoryLayout) || !(rhs is ValueNode))
                return null;

            var memoryLayout = ((NamedMemoryLayout) lhs).memoryLayout;
            Debug.Assert(memoryLayout.pointee != null);
            var member = memoryLayout.pointee.getAccessPathTo(
                (uint) ((ValueNode) rhs).value
            );
            return ((NamedMemoryLayout) lhs).label + "->" + member;
        }

        public override string ToString()
        {
            return $"{lhs} {@operator} {rhs}";
        }
    }
}
