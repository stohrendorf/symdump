using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using core.microcode;
using core.util;
using JetBrains.Annotations;

namespace core.cfg
{
    public class InstructionSequence : Node
    {
        public override string Id => $"insnseq_{InstructionList.Keys.First():x8}";

        public SortedDictionary<uint, MicroInsn> InstructionList { get; } = new SortedDictionary<uint, MicroInsn>();

        public InstructionSequence([NotNull] IGraph graph)
            : base(graph)
        {
        }

        public override IEnumerable<MicroInsn> Instructions => InstructionList.Values;

        public override bool ContainsAddress(uint address)
        {
            if (InstructionList.Count == 0)
                return false;

            return address >= InstructionList.Keys.First() && address <= InstructionList.Keys.Last();
        }

        public InstructionSequence Chop(uint from)
        {
            Debug.Assert(ContainsAddress(from));

            var result = new InstructionSequence(Graph);
            foreach (var split in InstructionList.Where(i => i.Key >= from))
            {
                result.InstructionList.Add(split.Key, split.Value);
            }

            foreach (var rm in result.InstructionList.Keys)
            {
                InstructionList.Remove(rm);
            }

            Debug.Assert(!ContainsAddress(from));
            Debug.Assert(result.ContainsAddress(from));

            return result;
        }
    }

    internal enum RegisterUsage
    {
        Input,
        Output,
        Modified
    }
}
