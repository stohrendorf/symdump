using System.Collections.Generic;
using JetBrains.Annotations;

namespace core
{
    public interface IExpressionNode
    {
        [NotNull] IEnumerable<int> UsedRegisters { get; }

        [NotNull] IEnumerable<int> UsedStack { get; }

        [NotNull] IEnumerable<uint> UsedMemory { get; }

        [NotNull]
        string ToCode();
    }
}