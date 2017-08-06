using JetBrains.Annotations;

namespace core
{
    public interface IDataFlowState
    {
        [NotNull] IDebugSource DebugSource { get; }
        
        [CanBeNull] IExpressionNode GetRegisterExpression(int registerId);
    }
}
