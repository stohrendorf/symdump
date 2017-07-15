using symdump.symfile.type;

namespace symdump.exefile.expression
{
    public class ValueNode : IExpressionNode
    {
        public readonly long value;

        public ICompoundType compoundType => null;

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
