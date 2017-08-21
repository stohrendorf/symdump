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
        public WhileTrueNode([NotNull] INode body) : base(body.Graph)
        {
            Debug.Assert(body.Outs.Count() == 1);
            Debug.Assert(body.Outs.All(e => e is AlwaysEdge));

            _body = body;
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
    }
}
