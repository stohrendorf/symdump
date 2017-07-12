namespace symdump.exefile.expression
{
    public class DerefNode : IExpressionNode
    {
        public readonly IExpressionNode inner;

        public DerefNode(IExpressionNode inner)
        {
            this.inner = inner;
        }

        public string toCode()
        {
            return $"*({inner.toCode()})";
        }
    }
}
