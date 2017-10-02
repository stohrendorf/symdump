using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace core
{
    public interface IOperand : IEquatable<IOperand>
    {
        [NotNull]
        IExpressionNode ToExpressionNode([NotNull] IDataFlowState dataFlowState);

        [NotNull]
        IEnumerable<int> TouchedRegisters { get; }
    }
}
