using System.Collections.Generic;
using core;
using core.util;
using JetBrains.Annotations;

namespace exefile.controlflow
{
    public class IfBlock : IBlock
    {
        [NotNull] public readonly IBlock condition;
        [NotNull] public readonly IBlock body;
        [NotNull] public readonly IBlock exit;
        public readonly bool invertedCondition;

        public IfBlock([NotNull] IBlock condition, [NotNull] IBlock body, [NotNull] IBlock exit, bool invertedCondition)
        {
            this.condition = condition;
            this.body = body;
            this.exit = exit;
            this.invertedCondition = invertedCondition;
        }

        public IBlock trueExit => exit;
        public IBlock falseExit => null;
        public uint start => condition.start;

        public SortedDictionary<uint, Instruction> instructions
        {
            get
            {
                var tmp = new SortedDictionary<uint, Instruction>();
                foreach (var insn in condition.instructions) tmp.Add(insn.Key, insn.Value);
                foreach (var insn in body.instructions) tmp.Add(insn.Key, insn.Value);
                return tmp;
            }
        }

        public ExitType? exitType => ExitType.Unconditional;

        public bool containsAddress(uint address) =>
            condition.containsAddress(address) || body.containsAddress(address);

        public void dump(IndentedTextWriter writer)
        {
            writer.WriteLine(invertedCondition ? "if_not{" : "if{");
            ++writer.indent;
            condition.dump(writer);
            --writer.indent;
            writer.WriteLine("} {");
            ++writer.indent;
            body.dump(writer);
            --writer.indent;
            writer.WriteLine("}");
        }
    }
}
