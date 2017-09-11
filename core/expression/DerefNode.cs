using System.Collections.Generic;
using JetBrains.Annotations;

namespace core.expression
{
    public class DerefNode : IExpressionNode
    {
        [NotNull] public readonly IExpressionNode Inner;

        public IEnumerable<int> UsedRegisters => Inner.UsedRegisters;
        public IEnumerable<int> UsedStack => Inner.UsedStack;
        public IEnumerable<uint> UsedMemory => Inner.UsedMemory;

        public DerefNode([NotNull] IExpressionNode inner)
        {
            Inner = inner;
        }

        public string ToCode()
        {
            if (Inner is RegisterOffsetNode node)
            {
                var c = node.TryDeref();
                if (c != null)
                    return c;
            }
            else
            {
                var c = (Inner as ExpressionNode)?.TryDeref();
                if (c != null)
                    return c;
            }
            return $"*({Inner.ToCode()})";
        }

        public override string ToString()
        {
            return $"pointer to {Inner}";
        }
    }
}
