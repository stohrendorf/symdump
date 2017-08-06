using System.Collections.Generic;
using core;
using core.util;
using JetBrains.Annotations;

namespace exefile.controlflow
{
    public interface IBlock
    {
        [CanBeNull]
        IBlock TrueExit { get; }

        [CanBeNull]
        IBlock FalseExit { get; }

        uint Start { get; }

        [NotNull]
        SortedDictionary<uint, Instruction> Instructions { get; }

        [CanBeNull]
        ExitType? ExitType { get; }

        bool ContainsAddress(uint address);
        void Dump([NotNull] IndentedTextWriter writer);
    }
}
