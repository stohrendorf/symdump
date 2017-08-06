using System;
using JetBrains.Annotations;

namespace core
{
    public interface IOperand : IEquatable<IOperand>
    {
        [NotNull]
        IExpressionNode ToExpressionNode([NotNull] IDataFlowState dataFlowState);
    }
}
