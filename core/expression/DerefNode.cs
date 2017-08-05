using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace core.expression
{
    public class DerefNode : IExpressionNode
    {
        [NotNull] public readonly IExpressionNode inner;

        public IEnumerable<int> usedRegisters => inner.usedRegisters;
        public IEnumerable<int> usedStack => inner.usedStack;
        public IEnumerable<uint> usedMemory => inner.usedMemory;

        public DerefNode([NotNull] IExpressionNode inner)
        {
            this.inner = inner;
        }

        public string toCode()
        {
            if (inner is RegisterOffsetNode)
            {
                var c = ((RegisterOffsetNode) inner).tryDeref();
                if (c != null)
                    return c;
            }
            else
            {
                var c = (inner as ExpressionNode)?.tryDeref();
                if (c != null)
                    return c;
            }
            return $"*({inner.toCode()})";
        }

        public override string ToString()
        {
            return $"pointer to {inner}";
        }
    }
}
