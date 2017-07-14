namespace symdump.exefile.expression
{
    public class LabelNode : IExpressionNode
    {
        // TODO provide type and address information here
        public readonly string label;

        public LabelNode(string label)
        {
            this.label = label;
        }

        public string toCode()
        {
            // FIXME: This is simply wrong.
            return "(char*)&" + label;
        }
    }
}
