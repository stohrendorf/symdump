using System.Collections.Generic;
using System.Linq;
using core.microcode;

namespace core.cfg
{
    public sealed class EntryNode : Node
    {
        public EntryNode(IGraph graph) : base(graph)
        {
        }

        public override IEnumerable<MicroInsn> Instructions => Enumerable.Empty<MicroInsn>();

        public override string Id => "entry";

        public override bool ContainsAddress(uint address)
        {
            return false;
        }
    }
}