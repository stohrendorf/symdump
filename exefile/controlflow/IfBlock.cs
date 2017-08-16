using System.Collections.Generic;
using System.IO;
using System.Text;
using core;
using core.util;
using JetBrains.Annotations;

namespace exefile.controlflow
{
    public class IfBlock : IBlock
    {
        [NotNull] public readonly IBlock Condition;
        [NotNull] public readonly IBlock Body;
        [NotNull] public readonly IBlock Exit;
        public readonly bool InvertedCondition;

        public IfBlock([NotNull] IBlock condition, [NotNull] IBlock body, [NotNull] IBlock exit, bool invertedCondition)
        {
            Condition = condition;
            Body = body;
            Exit = exit;
            InvertedCondition = invertedCondition;
        }

        public IBlock TrueExit => Exit;
        public IBlock FalseExit => null;
        public uint Start => Condition.Start;

        public SortedDictionary<uint, Instruction> Instructions
        {
            get
            {
                var tmp = new SortedDictionary<uint, Instruction>();
                foreach (var insn in Condition.Instructions) tmp.Add(insn.Key, insn.Value);
                foreach (var insn in Body.Instructions) tmp.Add(insn.Key, insn.Value);
                return tmp;
            }
        }

        public ExitType? ExitType => controlflow.ExitType.Unconditional;

        public bool ContainsAddress(uint address) =>
            Condition.ContainsAddress(address) || Body.ContainsAddress(address);

        public override string ToString()
        {
            var sb = new StringBuilder();
            var writer = new IndentedTextWriter(new StringWriter(sb));
            Dump(writer);
            return sb.ToString();
        }

        public void Dump(IndentedTextWriter writer)
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
