using System;

namespace exefile.controlflow.cfg
{
    public abstract class Edge : IEdge, IEquatable<Edge>
    {
        protected Edge(INode from, INode to)
        {
            From = from;
            To = to;
        }

        public INode From { get; }
        public INode To { get; }
        public abstract IEdge CloneTyped(INode from, INode to);

        public override string ToString()
        {
            return $"-- 0x{From.Start:x8} -- {GetType().Name} -- 0x{To.Start:x8} -->";
        }

        public bool Equals(Edge other)
        {
            return From.Equals(other.From) && To.Equals(other.To);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Edge) obj);
        }

        public override int GetHashCode()
        {
            return From.GetHashCode() ^ To.GetHashCode();
        }
    }
}
