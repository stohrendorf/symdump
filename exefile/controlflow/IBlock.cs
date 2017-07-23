using System.Collections.Generic;
using core;
using JetBrains.Annotations;

namespace exefile.controlflow
{
    public interface IBlock
    {
        [CanBeNull] Block trueExit { get; set; }
        [CanBeNull] Block falseExit { get; set; }
        uint start { get; }
        [NotNull] SortedDictionary<uint, Instruction> instructions { get; }
        [CanBeNull] ExitType? exitType { get; set; }
        bool containsAddress(uint address);
    }
}
