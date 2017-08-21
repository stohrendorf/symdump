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
    public class IfNode : Node
    {
        [NotNull] protected readonly INode Condition;

        [NotNull] protected readonly INode Body;

        protected readonly bool InvertedCondition;

        public IfNode([NotNull] INode condition)
            : base(condition.Graph)
        {
            Debug.Assert(condition.Outs.Count() == 2);
            Debug.Assert(condition.Outs.Count(e => e is TrueEdge) == 1);
            Debug.Assert(condition.Outs.Count(e => e is FalseEdge) == 1);

            var trueEdge = condition.Outs.First(e => e is TrueEdge);
            var falseEdge = condition.Outs.First(e => e is FalseEdge);

            var trueNode = trueEdge.To;
            var falseNode = falseEdge.To;
            InvertedCondition = trueNode.Outs.Count() != 1 || !(trueNode.Outs.First() is AlwaysEdge);

            INode body, common;
            if (!InvertedCondition)
            {
                body = trueNode;
                common = body.Outs.First().To;
                Debug.Assert(common.Equals(falseNode));
            }
            else
            {
                body = falseNode;
                common = body.Outs.First().To;
                Debug.Assert(common.Equals(trueNode));
            }

            Debug.Assert(body.Outs.Count() == 1);
            Debug.Assert(body.Outs.First() is AlwaysEdge);

            Condition = condition;
            Body = body;

            Graph.ReplaceNode(condition, this);
            Graph.RemoveNode(body);
            Graph.AddEdge(new AlwaysEdge(this, common));
        }

        public override SortedDictionary<uint, Instruction> Instructions
        {
            get
            {
                var tmp = new SortedDictionary<uint, Instruction>();
                foreach (var insn in Condition.Instructions) tmp.Add(insn.Key, insn.Value);
                foreach (var insn in Body.Instructions) tmp.Add(insn.Key, insn.Value);
                return tmp;
            }
        }

        public override bool ContainsAddress(uint address) =>
            Condition.ContainsAddress(address) || Body.ContainsAddress(address);

        public override string ToString()
        {
            var sb = new StringBuilder();
            var writer = new IndentedTextWriter(new StringWriter(sb));
            Dump(writer);
            return sb.ToString();
        }

        public override void Dump(IndentedTextWriter writer)
        {
            writer.WriteLine(InvertedCondition ? "if_not{" : "if{");
            ++writer.Indent;
            Condition.Dump(writer);
            --writer.Indent;
            writer.WriteLine("} {");
            ++writer.Indent;
            Body.Dump(writer);
            --writer.Indent;
            writer.WriteLine("}");
        }
    }
}
