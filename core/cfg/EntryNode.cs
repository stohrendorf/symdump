using System.Collections.Generic;
using System.Linq;
using core.util;

namespace core.cfg
{
    public sealed class EntryNode : Node
    {
        public EntryNode(IGraph graph) : base(graph)
        {
        }

        public override bool ContainsAddress(uint address) => false;

        public override IEnumerable<Instruction> Instructions => Enumerable.Empty<Instruction>();

        public override void Dump(IndentedTextWriter writer, IDataFlowState dataFlowState)
        {
            writer.WriteLine("EntryNode");
        }

        public override string Id => "entry";
        
        public override IEnumerable<int> InputRegisters => Enumerable.Empty<int>();

        public override IEnumerable<int> OutputRegisters => Enumerable.Empty<int>();
    }
}
