using System.Collections.Generic;
using core;
using core.util;
using JetBrains.Annotations;

namespace exefile.controlflow
{
    public interface IBlock
    {
        [CanBeNull]
        IBlock trueExit { get; }

        [CanBeNull]
        IBlock falseExit { get; }

        uint start { get; }

        [NotNull]
        SortedDictionary<uint, Instruction> instructions { get; }

        [CanBeNull]
        ExitType? exitType { get; }

        bool containsAddress(uint address);
        void dump([NotNull] IndentedTextWriter writer);
    }
}
