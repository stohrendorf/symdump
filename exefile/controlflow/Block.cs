using System.Collections.Generic;
using System.IO;
using System.Linq;
using core;
using core.util;

namespace exefile.controlflow
{
    public class Block : IBlock
    {
        public IBlock trueExit { get; set; }

        public IBlock falseExit { get; set; }

        public uint start => instructions.Keys.First();

        public SortedDictionary<uint, Instruction> instructions { get; } = new SortedDictionary<uint, Instruction>();

        public ExitType? exitType { get; set; }

        public bool containsAddress(uint address)
        {
            if (instructions.Count == 0)
                return false;

            return address >= instructions.Keys.First() && address <= instructions.Keys.Last();
        }
    }
}
