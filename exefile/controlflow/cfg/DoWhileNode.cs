using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using core;
using core.util;
using JetBrains.Annotations;

namespace exefile.controlflow.cfg
{
    public class DoWhileNode : Node
    {
        [NotNull] private readonly INode _condition;

        [NotNull] private readonly INode _body;

        private readonly bool _invertedCondition;

        public DoWhileNode([NotNull] INode body, IGraph graph)
            : base(graph)
        {
            Debug.Assert(body.Outs.Count() == 1);
            Debug.Assert(body.Outs.All(e => e is AlwaysEdge));

            var condition = body.Outs.First().To;
            Debug.Assert(condition.Outs.Count() == 2);
            Debug.Assert(condition.Outs.Count(e => e is TrueEdge) == 1);
            Debug.Assert(condition.Outs.Count(e => e is FalseEdge) == 1);

            var trueEdge = condition.Outs.First(e => e is TrueEdge);
            var falseEdge = condition.Outs.First(e => e is FalseEdge);

            var trueNode = trueEdge.To;
            var falseNode = falseEdge.To;
            _invertedCondition = !body.Equals(trueNode);
            Debug.Assert(_invertedCondition || body.Equals(falseNode));

            _condition = condition;
            _body = body;
        }

        public override SortedDictionary<uint, Instruction> Instructions
        {
            get
            {
                var tmp = new SortedDictionary<uint, Instruction>();
                foreach (var insn in _condition.Instructions) tmp.Add(insn.Key, insn.Value);
                foreach (var insn in _body.Instructions) tmp.Add(insn.Key, insn.Value);
                return tmp;
            }
        }

        public override bool ContainsAddress(uint address) =>
            _condition.ContainsAddress(address) || _body.ContainsAddress(address);

        public override void Dump(IndentedTextWriter writer)
        {
            writer.WriteLine("do {");
            ++writer.Indent;
            _body.Dump(writer);
            --writer.Indent;
            writer.WriteLine(_invertedCondition ? "} while_not {" : "} while {");
            ++writer.Indent;
            _condition.Dump(writer);
            --writer.Indent;
            writer.WriteLine("}");
        }
    }
}
