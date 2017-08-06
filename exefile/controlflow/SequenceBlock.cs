using System.Collections.Generic;
using System.Linq;
using core;
using core.util;

namespace exefile.controlflow
{
    public class SequenceBlock : IBlock
    {
        public readonly SortedDictionary<uint, IBlock> Sequence = new SortedDictionary<uint, IBlock>();

        public IBlock TrueExit => Sequence.Values.Last().TrueExit;
        public IBlock FalseExit => null;
        public uint Start => Sequence.Keys.First();

        public SortedDictionary<uint, Instruction> Instructions
        {
            get
            {
                var tmp = new SortedDictionary<uint, Instruction>();
                foreach (var block in Sequence.Values)
                {
                    foreach (var insn in block.Instructions)
                    {
                        tmp.Add(insn.Key, insn.Value);
                    }
                }
                return tmp;
            }
        }

        public ExitType? ExitType => Sequence.Values.Last().ExitType;

        public bool ContainsAddress(uint address) => Sequence.Values.Any(b => b.ContainsAddress(address));

        public void Dump(IndentedTextWriter writer)
        {
            foreach (var block in Sequence.Values)
            {
                block.Dump(writer);
            }
        }
    }
}
