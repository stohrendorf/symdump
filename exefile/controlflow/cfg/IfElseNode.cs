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
    public class IfElseNode : Node
    {
        [NotNull] private readonly INode _condition;

        [NotNull] private readonly INode _trueBody;
        [NotNull] private readonly INode _falseBody;

        public IfElseNode([NotNull] INode condition, IGraph graph) : base(graph)
        {
            Debug.Assert(condition.Outs.Count() == 2);

            var trueEdge = condition.Outs.First(e => e is TrueEdge);
            Debug.Assert(trueEdge != null);
            Debug.Assert(trueEdge.From.Equals(condition));

            var falseEdge = condition.Outs.First(e => e is TrueEdge);
            Debug.Assert(falseEdge != null);
            Debug.Assert(falseEdge.From.Equals(condition));

            Debug.Assert(!trueEdge.To.Equals(falseEdge.To));

            _condition = condition;
            _trueBody = trueEdge.To;
            Debug.Assert(_trueBody.Outs.Count() == 1);
            Debug.Assert(_trueBody.Outs.All(e => e is AlwaysEdge));

            _falseBody = falseEdge.To;
            Debug.Assert(_falseBody.Outs.Count() == 1);
            Debug.Assert(_falseBody.Outs.All(e => e is AlwaysEdge));

            Debug.Assert(_trueBody.Outs.First().To.Equals(_falseBody.Outs.First().To));
        }

        public override SortedDictionary<uint, Instruction> Instructions
        {
            get
            {
                var tmp = new SortedDictionary<uint, Instruction>();
                foreach (var insn in _condition.Instructions) tmp.Add(insn.Key, insn.Value);
                foreach (var insn in _trueBody.Instructions) tmp.Add(insn.Key, insn.Value);
                foreach (var insn in _falseBody.Instructions) tmp.Add(insn.Key, insn.Value);
                return tmp;
            }
        }

        public override bool ContainsAddress(uint address) =>
            _condition.ContainsAddress(address) || _trueBody.ContainsAddress(address) ||
            _falseBody.ContainsAddress(address);

        public override string ToString()
        {
            var sb = new StringBuilder();
            var writer = new IndentedTextWriter(new StringWriter(sb));
            Dump(writer);
            return sb.ToString();
        }

        public override void Dump(IndentedTextWriter writer)
        {
            writer.WriteLine("if{");
            ++writer.Indent;
            _condition.Dump(writer);
            --writer.Indent;
            writer.WriteLine("} {");
            ++writer.Indent;
            _trueBody.Dump(writer);
            --writer.Indent;
            writer.WriteLine("} else {");
            ++writer.Indent;
            _falseBody.Dump(writer);
            --writer.Indent;
            writer.WriteLine("}");
        }
    }
}
