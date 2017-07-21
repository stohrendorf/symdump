using JetBrains.Annotations;

namespace core.expression
{
    public class DataCopyNode : IExpressionNode
    {
        [NotNull] public readonly IExpressionNode to;
        [NotNull] public readonly IExpressionNode from;

        public DataCopyNode([NotNull] IExpressionNode to, [NotNull] IExpressionNode from)
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