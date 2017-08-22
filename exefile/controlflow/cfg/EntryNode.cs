using System.Collections.Generic;
using core;
using core.util;

namespace exefile.controlflow.cfg
{
    public sealed class EntryNode : Node
    {
        public EntryNode(IGraph graph) : base(graph)
        {
        }

        public override bool ContainsAddress(uint address) => false;

        public override SortedDictionary<uint, Instruction> Instructions { get; } =
            new SortedDictionary<uint, Instruction>();

        public override void Dump(IndentedTextWriter writer)
        {
            writer.WriteLine("EntryNode");
        }

        public override uint Start => uint.MinValue;
        
        public override string Id => "entry";
    }
}
