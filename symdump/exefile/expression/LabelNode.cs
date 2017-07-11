namespace symdump.exefile.expression
{
    public class LabelNode : IExpressionNode
    {
        public readonly string label;

        public LabelNode(string label)
        {
            this.label = label;
        }

        public string toCode()
        {
            return label;
        }
    }
}