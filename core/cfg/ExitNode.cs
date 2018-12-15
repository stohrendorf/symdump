using System.Collections.Generic;
using System.Linq;
using core.microcode;

namespace core.cfg
{
    public class ExitNode : Node
    {
        public ExitNode(IGraph graph) : base(graph)
        {
        }

        public override IEnumerable<MicroInsn> Instructions => Enumerable.Empty<MicroInsn>();

        public override string Id => "exit";

        public override bool ContainsAddress(uint address)
        {
            return false;
        }
    }
}