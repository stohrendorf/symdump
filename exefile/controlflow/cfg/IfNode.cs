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
            Debug.Assert(IsCandidate(condition));

            Debug.Assert(condition.Outs.Count() == 2);
            
            var trueEdge = condition.Outs.First(e => e is TrueEdge);
            var falseEdge = condition.Outs.First(e => e is FalseEdge);

            var trueNode = trueEdge.To;
            var falseNode = falseEdge.To;
            InvertedCondition = trueNode.Ins.Count() != 1 || trueNode.Outs.Count() != 1 || !(trueNode.Outs.First() is AlwaysEdge);

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
            var outs = Outs.ToList();
            foreach(var e in outs)
                Graph.RemoveEdge(e);
            
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

        public static bool IsCandidate([NotNull] INode condition)
        {
            if (condition is EntryNode || condition is ExitNode)
                return false;
            
            if (condition.Outs.Count() != 2)
                return false;

            var trueNode = condition.Outs.FirstOrDefault(e => e is TrueEdge)?.To;
            if (trueNode == null)
                return false;

            var falseNode = condition.Outs.FirstOrDefault(e => e is FalseEdge)?.To;
            if (falseNode == null)
                return false;

            // if(condition) trueNode;
            if (trueNode.Ins.Count() == 1 && trueNode.Outs.Count() == 1 && trueNode.Outs.First() is AlwaysEdge)
            {
                if (trueNode.Outs.First().To.Equals(falseNode))
                {
                    return true;
                }
            }

            // ReSharper disable once InvertIf
            // if(!condition) falseNode;
            if (falseNode.Ins.Count() == 1 && falseNode.Outs.Count() == 1 && falseNode.Outs.First() is AlwaysEdge)
            {
                // ReSharper disable once InvertIf
                if (falseNode.Outs.First().To.Equals(trueNode))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
