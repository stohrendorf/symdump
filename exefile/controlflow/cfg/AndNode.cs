using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using core;
using core.util;
using JetBrains.Annotations;

namespace exefile.controlflow.cfg
{
    public class AndNode : Node
    {
        private readonly IList<INode> _nodes;

        public override string Id => "and_" + _nodes[0].Id;

        public AndNode([NotNull] INode c0) : base(c0.Graph)
        {
            Debug.Assert(c0.Outs.Count() == 2);
            Debug.Assert(c0.Outs.Count(e => e is TrueEdge) == 1);
            Debug.Assert(c0.Outs.Count(e => e is FalseEdge) == 1);

            var c1 = c0.Outs.First(e => e is TrueEdge).To;
            Debug.Assert(!c0.Equals(c1));
            Debug.Assert(c1.Outs.Count() == 2);
            Debug.Assert(c1.Outs.Count(e => e is TrueEdge) == 1);
            Debug.Assert(c1.Outs.Count(e => e is FalseEdge) == 1);

            var sFalse = c0.Outs.First(e => e is FalseEdge).To;
            Debug.Assert(c1.Outs.First(e => e is FalseEdge).To.Equals(sFalse));

            var sTrue = c1.Outs.First(e => e is TrueEdge).To;
            Debug.Assert(!sTrue.Equals(sFalse));
            Debug.Assert(!sTrue.Equals(c0));
            Debug.Assert(!sTrue.Equals(c1));

            if (c0 is AndNode c0And)
            {
                _nodes = c0And._nodes;
                _nodes.Add(c1);
            }
            else
            {
                _nodes = new List<INode> {c0, c1};
            }

            Graph.ReplaceNode(c0, this);
            Graph.RemoveNode(c1);
            Graph.AddEdge(new TrueEdge(this, sTrue));
        }

        public static bool IsCandidate([NotNull] INode c0)
        {
            if (c0.Outs.Count() != 2)
                return false;

            if (c0.Outs.Count(e => e is TrueEdge) != 1)
                return false;

            if (c0.Outs.Count(e => e is FalseEdge) != 1)
                return false;

            var c1 = c0.Outs.First(e => e is TrueEdge).To;
            if (c1.Outs.Count() != 2)
                return false;
            if (c1.Outs.Count(e => e is TrueEdge) != 1)
                return false;
            if (c1.Outs.Count(e => e is FalseEdge) != 1)
                return false;

            var sFalse = c0.Outs.First(e => e is FalseEdge).To;
            if (!c1.Outs.First(e => e is FalseEdge).To.Equals(sFalse))
                return false;
            
            var sTrue = c1.Outs.First(e => e is TrueEdge).To;
            if (sTrue.Equals(sFalse))
                return false;

            if (sTrue.Equals(c0))
                return false;
            
            return !sTrue.Equals(c1);
        }

        public override bool ContainsAddress(uint address)
            => _nodes.Any(n => n.ContainsAddress(address));

        public override IEnumerable<Instruction> Instructions => _nodes.SelectMany(n => n.Instructions);

        public override void Dump(IndentedTextWriter writer)
        {
            writer.WriteLine("{");
            ++writer.Indent;
            bool first = true;
            foreach (var n in _nodes)
            {
                if (!first)
                {
                    writer.WriteLine("&&");
                }
                first = false;

                ++writer.Indent;
                n.Dump(writer);
                --writer.Indent;
            }
            --writer.Indent;
            writer.WriteLine("}");
        }
    }
}
