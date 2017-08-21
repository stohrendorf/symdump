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
            throw new System.NotImplementedException();
        }

        public override string ToString()
        {
            return "ExitNode";
        }
        
        public override uint Start => 0;
        
        public override string Id => "exit";
    }
}
