namespace core.expression
{
    public class NamedMemoryLayout : IExpressionNode
    {
        public readonly string label;

        public IMemoryLayout memoryLayout { get; }

        public NamedMemoryLayout(string label, IMemoryLayout memoryLayout)
        {
            this.label = label;
            this.memoryLayout = memoryLayout;
        }

        public string toCode()
        {
            return label;
        }
    }
}
