using System.Collections.Generic;
using core;
using core.util;

namespace exefile.controlflow.cfg
{
    public class ExitNode : Node
    {
        public ExitNode(IGraph graph) : base(graph)
        {
        }

        public override bool ContainsAddress(uint address) => false;

        public override SortedDictionary<uint, Instruction> Instructions { get; } =
            new SortedDictionary<uint, Instruction>();

        public override void Dump(IndentedTextWriter writer)
        {
            writer.WriteLine("ExitNode");
        }

        public override uint Start => uint.MaxValue;
        
        public override string Id => "exit";
    }
}
