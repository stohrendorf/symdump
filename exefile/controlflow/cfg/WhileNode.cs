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
    }
}
