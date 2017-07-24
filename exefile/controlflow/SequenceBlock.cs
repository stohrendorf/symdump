using System.Collections.Generic;
using System.Linq;
using core;
using core.util;

namespace exefile.controlflow
{
    public class SequenceBlock : IBlock
    {
        public readonly SortedDictionary<uint, IBlock> sequence = new SortedDictionary<uint, IBlock>();

        public IBlock trueExit => sequence.Values.Last().trueExit;
        public IBlock falseExit => null;
        public uint start => sequence.Keys.First();

        public SortedDictionary<uint, Instruction> instructions
        {
            get
            {
                var tmp = new SortedDictionary<uint, Instruction>();
                foreach (var block in sequence.Values)
                {
                    foreach (var insn in block.instructions)
                    {
                        tmp.Add(insn.Key, insn.Value);
                    }
                }
                return tmp;
            }
        }

        public ExitType? exitType => sequence.Values.Last().exitType;

        public bool containsAddress(uint address) => sequence.Values.Any(b => b.containsAddress(address));

        public void dump(IndentedTextWriter writer)
        {
            foreach (var block in sequence.Values)
            {
                block.dump(writer);
            }
        }
    }
}
