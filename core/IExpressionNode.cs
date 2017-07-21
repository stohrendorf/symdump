using JetBrains.Annotations;

namespace core
{
    public interface IExpressionNode
    {
        [NotNull]
        string toCode();
    }
}