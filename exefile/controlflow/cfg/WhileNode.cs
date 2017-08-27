using System.Diagnostics;
using System.Linq;
using core.util;
using JetBrains.Annotations;

namespace exefile.controlflow.cfg
{
    public class WhileNode : IfNode
    {
        public WhileNode([NotNull] INode condition)
            : base(condition)
        {
            Debug.Assert(IsCandidate(condition));
            
            Debug.Assert(Condition.Equals(Body.Outs.First().To));
        }

        public override void Dump(IndentedTextWriter writer)
        {
            writer.WriteLine(InvertedCondition ? "while_not{" : "while{");
            ++writer.Indent;
            Condition.Dump(writer);
            --writer.Indent;
            writer.WriteLine("} {");
            ++writer.Indent;
            Body.Dump(writer);
            --writer.Indent;
            writer.WriteLine("}");
        }

        public new static bool IsCandidate([NotNull] INode condition)
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

            if (trueNode.Ins.Count() == 1 && trueNode.Outs.Count() == 1 && trueNode.Outs.First() is AlwaysEdge)
            {
                if (trueNode.Outs.First().To.Equals(condition))
                    return true;
            }

            if (falseNode.Ins.Count() == 1 && falseNode.Outs.Count() == 1 &&
                falseNode.Outs.First() is AlwaysEdge)
            {
                if (falseNode.Outs.First().To.Equals(condition))
                    return true;
            }

            return false;
        }
    }
}
