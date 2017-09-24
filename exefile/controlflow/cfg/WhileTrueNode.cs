using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using core;
using core.util;
using JetBrains.Annotations;

namespace exefile.controlflow.cfg
{
    public class WhileTrueNode : Node
    {
        public override string Id => "whiletrue_" + _body.Id;

        public WhileTrueNode([NotNull] INode body) : base(body.Graph)
        {
            Debug.Assert(IsCandidate(body));
            
            Debug.Assert(body.Outs.Count() == 1);
            Debug.Assert(body.Outs.All(e => e is AlwaysEdge));
            Debug.Assert(body.Outs.First().To.Equals(body));

            _body = body;
            
            var loop = _body.Outs.First();
            Graph.RemoveEdge(loop);
            Graph.ReplaceNode(_body, this);
        }

        [NotNull] private readonly INode _body;

        public override SortedDictionary<uint, Instruction> Instructions => _body.Instructions;

        public override bool ContainsAddress(uint address) => _body.ContainsAddress(address);

        public override void Dump(IndentedTextWriter writer)
        {
            writer.WriteLine("while(true) {");
            ++writer.Indent;
            _body.Dump(writer);
            --writer.Indent;
            writer.WriteLine("}");
        }

        public static bool IsCandidate([NotNull] INode body)
        {
            if (body is EntryNode || body is ExitNode)
                return false;

            if (body.Outs.Count() != 1)
                return false;

            var next = body.Outs.FirstOrDefault(e => e is AlwaysEdge)?.To;
            return next != null && next.Equals(body);
        }
    }
}
