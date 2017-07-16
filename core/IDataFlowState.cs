namespace core
{
    public interface IDataFlowState
    {
        IDebugSource debugSource { get; }
        
        IExpressionNode getRegisterExpression(int registerId);
    }
}
