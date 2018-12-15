using System.Collections.Generic;
using core.microcode;

namespace core.cfg
{
    public abstract class Node : INode
    {
        protected Node(IGraph graph)
        {
            Graph = graph;
        }

        public abstract bool ContainsAddress(uint address);

        public abstract IEnumerable<MicroInsn> Instructions { get; }

        public abstract string Id { get; }

        public IGraph Graph { get; }

        public IEnumerable<IEdge> Ins => Graph.GetIns(this);

        public IEnumerable<IEdge> Outs => Graph.GetOuts(this);
    }
}