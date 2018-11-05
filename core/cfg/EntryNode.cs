using System.Collections.Generic;
using System.Linq;
using core.microcode;
using core.util;

namespace core.cfg
{
    public sealed class EntryNode : Node
    {
        public EntryNode(IGraph graph) : base(graph)
        {
        }

        public override bool ContainsAddress(uint address) => false;

        public override IEnumerable<MicroInsn> Instructions => Enumerable.Empty<MicroInsn>();

        public override string Id => "entry";
    }
}
