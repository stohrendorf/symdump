using System;
using System.Collections.Generic;
using System.Linq;
using core;
using core.util;

namespace exefile.controlflow.cfg
{
    public abstract class Node : INode, IEquatable<Node>
    {
        protected Node(IGraph graph)
        {
            Graph = graph;
        }

        public abstract bool ContainsAddress(uint address);

        public abstract SortedDictionary<uint, Instruction> Instructions { get; }

        public virtual uint Start => Instructions.Keys.First();

        public abstract void Dump(IndentedTextWriter writer);

        public IGraph Graph { get; }

        public IEnumerable<IEdge> Outs => Graph.GetOuts(this);

        public bool Equals(Node other)
        {
            return Start == other.Start;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Node) obj);
        }

        public override int GetHashCode()
        {
            return (int) Start;
        }

        public virtual string Id => $"n_{Start:x8}";
    }
}
