using System;

namespace core
{
    public interface IOperand : IEquatable<IOperand>
    {
        IExpressionNode toExpressionNode(IDataFlowState dataFlowState);
    }
}
