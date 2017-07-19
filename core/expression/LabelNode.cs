namespace core.expression
{
    public class LabelNode : IExpressionNode
    {
        public readonly string label;

        public IMemoryLayout memoryLayout { get; }

        public LabelNode(string label, IMemoryLayout memoryLayout)
        {
            this.label = label;
            this.memoryLayout = memoryLayout;
        }

        public string toCode()
        {
            return memoryLayout == null ? label : memoryLayout.ToString();
        }
    }
}
