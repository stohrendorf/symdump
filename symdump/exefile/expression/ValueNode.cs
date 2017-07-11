namespace symdump.exefile.expression
{
    public class ValueNode : IExpressionNode
    {
        public readonly long value;

        public ValueNode(long value)
        {
            this.value = value;
        }

        public string toCode()
        {
            return value.ToString();
        }
    }
}