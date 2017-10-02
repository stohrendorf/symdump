using System.Collections.Generic;
using System.Linq;
using core.util;

namespace core.cfg
{
    public class ExitNode : Node
    {
        public ExitNode(IGraph graph) : base(graph)
        {
        }

        public override bool ContainsAddress(uint address) => false;

        public override IEnumerable<Instruction> Instructions => Enumerable.Empty<Instruction>();

        public override void Dump(IndentedTextWriter writer, IDataFlowState dataFlowState)
        {
            writer.WriteLine("ExitNode");
        }

        public override string Id => "exit";
        
        public override IEnumerable<int> InputRegisters => Enumerable.Empty<int>();

        public override IEnumerable<int> OutputRegisters => Enumerable.Empty<int>();
    }
}
