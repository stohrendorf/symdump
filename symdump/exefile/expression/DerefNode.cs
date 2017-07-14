using symdump.symfile.type;

namespace symdump.exefile.expression
{
    public class DerefNode : IExpressionNode
    {
        public readonly IExpressionNode inner;

        public ITypeDefinition typeDefinition => null;

        public DerefNode(IExpressionNode inner)
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
    }
}
