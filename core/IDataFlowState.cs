using JetBrains.Annotations;

namespace core
{
    public interface IDataFlowState
    {
        [NotNull] IDebugSource debugSource { get; }
        
        [CanBeNull] IExpressionNode getRegisterExpression(int registerId);
    }
}
