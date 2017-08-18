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
        public List<IBlock> Sequence { get; private set; } = new List<IBlock>();

        public IBlock TrueExit => Sequence.Last().TrueExit;
        public IBlock FalseExit => null;
        public uint Start => Sequence.First().Start;

        public SortedDictionary<uint, Instruction> Instructions
        {
            get
            {
                var tmp = new SortedDictionary<uint, Instruction>();
                foreach (var block in Sequence)
                {
                    foreach (var insn in block.Instructions)
                    {
                        tmp.Add(insn.Key, insn.Value);
                    }
                }
                return tmp;
            }
        }

        public ExitType? ExitType => Sequence.Last().ExitType;

        public bool ContainsAddress(uint address) => Sequence.Any(b => b.ContainsAddress(address));

        public override string ToString()
        {
            var sb = new StringBuilder();
            var writer = new IndentedTextWriter(new StringWriter(sb));
            Dump(writer);
            return sb.ToString();
        }

        public void Dump(IndentedTextWriter writer)
        {
            foreach (var block in Sequence)
            {
                block.Dump(writer);
            }
        }

        public void UpdateReferences(IReadOnlyDictionary<uint, IBlock> blocks, ISet<uint> processed)
        {
            if (!processed.Add(Start))
                return;

            var lastAddress = Sequence.Last().Start;
            if (blocks.ContainsKey(lastAddress))
                Sequence.Add(blocks[lastAddress]);
            Sequence.Last().UpdateReferences(blocks, processed);
        }
    }
}
