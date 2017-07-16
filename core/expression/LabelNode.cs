namespace core.expression
{
    public class LabelNode : IExpressionNode
    {
        public readonly string label;

        public ICompoundType compoundType { get; }

        public LabelNode(string label, ICompoundType compoundType)
        {
            this.label = label;
            this.compoundType = compoundType;
        }

        public string toCode()
        {
            return compoundType == null ? label : compoundType.ToString();
        }
    }
}
