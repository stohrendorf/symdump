using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using core.util;
using JetBrains.Annotations;

namespace core.expression
{
    public class ExpressionNode : IExpressionNode
    {
        public Operator Operator { get; }
        [NotNull] public readonly IExpressionNode Lhs;
        [NotNull] public readonly IExpressionNode Rhs;

        public IEnumerable<int> UsedRegisters => Lhs.UsedRegisters.Concat(Rhs.UsedRegisters);
        public IEnumerable<int> UsedStack => Lhs.UsedStack.Concat(Rhs.UsedStack);
        public IEnumerable<uint> UsedMemory => Lhs.UsedMemory.Concat(Rhs.UsedMemory);

        public ExpressionNode(Operator @operator, [NotNull] IExpressionNode lhs, [NotNull] IExpressionNode rhs)
        {
            Operator = @operator;
            Lhs = lhs;
            Rhs = rhs;
        }

        public string ToCode()
        {
            var lhsCode = Lhs.ToCode();
            var rhsCode = Rhs.ToCode();

            var selfPrecedence = Operator.GetPrecedence(false);
            if (selfPrecedence > (Lhs as ExpressionNode)?.Operator.GetPrecedence(false))
                lhsCode = $"({lhsCode})";

            if (selfPrecedence > (Rhs as ExpressionNode)?.Operator.GetPrecedence(false))
                rhsCode = $"({rhsCode})";

            return $"{lhsCode} {Operator.AsCode()} {rhsCode}";
        }

        [CanBeNull]
        public string TryDeref()
        {
            if (Operator != Operator.Add
                || !(Lhs is NamedMemoryLayout namedMemoryLayout)
                || !(Rhs is ValueNode valueNode))
                return null;

            var memoryLayout = namedMemoryLayout.MemoryLayout;

            if (memoryLayout.Pointee == null)
            {
                return $"((char*){namedMemoryLayout.Label})[{valueNode.Value}]";
            }
            
            var member = memoryLayout.Pointee.GetAccessPathTo((uint) valueNode.Value);
            return namedMemoryLayout.Label + "->" + member;
        }

        public override string ToString()
        {
            return $"{Lhs} {Operator} {Rhs}";
        }
    }
}
