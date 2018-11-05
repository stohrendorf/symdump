using System.Collections.Generic;
using System.Linq;
using core.microcode;
using JetBrains.Annotations;

namespace core.cfg
{
    public class InstructionCollection : Node
    {
        public override string Id => $"insncoll_{InstructionList.First().Key:x8}";

        [NotNull] public IList<KeyValuePair<uint, MicroInsn>> InstructionList { get; }

        public InstructionCollection([NotNull] IGraph graph)
            : base(graph)
        {
            InstructionList = new List<KeyValuePair<uint, MicroInsn>>();
        }

        public InstructionCollection([NotNull] InstructionSequence sequence)
            : base(sequence.Graph)
        {
            InstructionList = new List<KeyValuePair<uint, MicroInsn>>(sequence.InstructionList);
        }

        public override IEnumerable<MicroInsn> Instructions => InstructionList.Select(i => i.Value);

        public override bool ContainsAddress(uint address)
        {
            return InstructionList.Any(i => i.Key == address);
        }
    }
}
