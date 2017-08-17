using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using core;
using core.util;

namespace exefile.controlflow
{
    public class SequenceBlock : IBlock
    {
        public SortedDictionary<uint, IBlock> Sequence { get; private set; } = new SortedDictionary<uint, IBlock>();

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

        public override string ToString()
        {
            var sb = new StringBuilder();
            var writer = new IndentedTextWriter(new StringWriter(sb));
            Dump(writer);
            return sb.ToString();
        }

        public void Dump(IndentedTextWriter writer)
        {
            foreach (var block in Sequence.Values)
            {
                block.Dump(writer);
            }
        }

        public void UpdateReferences(IReadOnlyDictionary<uint, IBlock> blocks, ISet<uint> processed)
        {
            if (!processed.Add(Start))
                return;

            var lastAddress = Sequence.Keys.Last();
            if (blocks.ContainsKey(lastAddress))
                Sequence[lastAddress] = blocks[lastAddress];
            Sequence[lastAddress].UpdateReferences(blocks, processed);
        }
    }
}
