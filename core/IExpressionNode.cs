using System.Collections.Generic;
using JetBrains.Annotations;

namespace core
{
    public interface IExpressionNode
    {
        [NotNull]
        string toCode();

        [NotNull] IEnumerable<int> usedRegisters { get; }
        [NotNull] IEnumerable<int> usedStack { get; }
        [NotNull] IEnumerable<uint> usedMemory { get; }
    }
}
