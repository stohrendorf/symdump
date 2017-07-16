namespace core.expression
{
    public class DataCopyNode : IExpressionNode
    {
        public readonly IExpressionNode to;
        public readonly IExpressionNode from;

        public ICompoundType compoundType => null;

        public DataCopyNode(IExpressionNode to, IExpressionNode @from)
        {
            this.to = to;
            this.from = from;
        }

        public string toCode()
        {
            return $"{to.toCode()} = {from.toCode()}";
        }
    }
}
