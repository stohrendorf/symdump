using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using core;
using core.util;

namespace exefile.controlflow.cfg
{
    public abstract class Node : INode
    {
        protected Node(IGraph graph)
        {
            Graph = graph;
        }

        public abstract bool ContainsAddress(uint address);

        public abstract SortedDictionary<uint, Instruction> Instructions { get; }

        public virtual uint Start => Instructions.Keys.FirstOrDefault();
        
        public abstract string Id { get; }

        public abstract void Dump(IndentedTextWriter writer);

        public IGraph Graph { get; }

        public IEnumerable<IEdge> Ins => Graph.GetIns(this);
        
        public IEnumerable<IEdge> Outs => Graph.GetOuts(this);

        public sealed override string ToString()
        {
            var sb = new StringBuilder();
            Dump(new IndentedTextWriter(new StringWriter(sb)));
            return sb.ToString();
        }
    }
}
