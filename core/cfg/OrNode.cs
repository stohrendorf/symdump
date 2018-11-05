using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using core.microcode;
using core.util;
using JetBrains.Annotations;

namespace core.cfg
{
    public class OrNode : Node
    {
        private readonly IList<INode> _nodes;

        public override string Id => "or_" + _nodes[0].Id;

        public OrNode([NotNull] INode c0) : base(c0.Graph)
        {
            Debug.Assert(c0.Outs.Count() == 2);
            Debug.Assert(c0.Outs.Count(e => e is TrueEdge) == 1);
            Debug.Assert(c0.Outs.Count(e => e is FalseEdge) == 1);

            var c1 = c0.Outs.First(e => e is FalseEdge).To;
            Debug.Assert(!c0.Equals(c1));
            Debug.Assert(c1.Outs.Count() == 2);
            Debug.Assert(c1.Outs.Count(e => e is TrueEdge) == 1);
            Debug.Assert(c1.Outs.Count(e => e is FalseEdge) == 1);

            var sTrue = c0.Outs.First(e => e is TrueEdge).To;
            Debug.Assert(c1.Outs.First(e => e is TrueEdge).To.Equals(sTrue));

            var sFalse = c1.Outs.First(e => e is FalseEdge).To;
            Debug.Assert(!sFalse.Equals(sTrue));
            Debug.Assert(!sFalse.Equals(c0));
            Debug.Assert(!sFalse.Equals(c1));

            if (c0 is OrNode c0Or)
            {
                _nodes = c0Or._nodes;
                _nodes.Add(c1);
            }
            else
            {
                _nodes = new List<INode> {c0, c1};
            }

            Graph.ReplaceNode(c0, this);
            Graph.RemoveNode(c1);
            Graph.AddEdge(new FalseEdge(this, sFalse));
        }

        public static bool IsCandidate([NotNull] INode c0)
        {
            if (c0.Outs.Count() != 2)
                return false;

            if (c0.Outs.Count(e => e is TrueEdge) != 1)
                return false;

            if (c0.Outs.Count(e => e is FalseEdge) != 1)
                return false;

            var c1 = c0.Outs.First(e => e is FalseEdge).To;
            if (c1.Outs.Count() != 2)
                return false;
            if (c1.Outs.Count(e => e is TrueEdge) != 1)
                return false;
            if (c1.Outs.Count(e => e is FalseEdge) != 1)
                return false;

            var sTrue = c0.Outs.First(e => e is TrueEdge).To;
            if (!c1.Outs.First(e => e is TrueEdge).To.Equals(sTrue))
                return false;
            
            var sFalse = c1.Outs.First(e => e is FalseEdge).To;
            if (sFalse.Equals(sTrue))
                return false;

            if (sFalse.Equals(c0))
                return false;
            
            return !sFalse.Equals(c1);
        }

        public override bool ContainsAddress(uint address)
            => _nodes.Any(n => n.ContainsAddress(address));

        public override IEnumerable<MicroInsn> Instructions => _nodes.SelectMany(n => n.Instructions);
    }
}
