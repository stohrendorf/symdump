using System;
using JetBrains.Annotations;

namespace core
{
    public interface IOperand : IEquatable<IOperand>
    {
        [NotNull]
        IExpressionNode toExpressionNode([NotNull] IDataFlowState dataFlowState);
    }
}
