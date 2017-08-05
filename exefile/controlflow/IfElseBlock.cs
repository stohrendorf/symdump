using System.Collections.Generic;
using core;
using core.util;
using JetBrains.Annotations;

namespace exefile.controlflow
{
    public class IfElseBlock : IBlock
    {
        [NotNull] public readonly IBlock condition;
        [NotNull] public readonly IBlock trueBody;
        [NotNull] public readonly IBlock falseBody;
        [NotNull] public readonly IBlock exit;

        public IfElseBlock([NotNull] IBlock condition, [NotNull] IBlock trueBody, [NotNull] IBlock falseBody, [NotNull] IBlock exit)
        {
            this.condition = condition;
            this.trueBody = trueBody;
            this.falseBody = falseBody;
            this.exit = exit;
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
                foreach (var insn in trueBody.instructions) tmp.Add(insn.Key, insn.Value);
                foreach (var insn in falseBody.instructions) tmp.Add(insn.Key, insn.Value);
                return tmp;
            }
        }

        public ExitType? exitType => ExitType.Unconditional;

        public bool containsAddress(uint address) =>
            condition.containsAddress(address) || trueBody.containsAddress(address) || falseBody.containsAddress(address);

        public void dump(IndentedTextWriter writer)
        {
            writer.WriteLine("if{");
            ++writer.indent;
            condition.dump(writer);
            --writer.indent;
            writer.WriteLine("} {");
            ++writer.indent;
            trueBody.dump(writer);
            --writer.indent;
            writer.WriteLine("} else {");
            ++writer.indent;
            falseBody.dump(writer);
            --writer.indent;
            writer.WriteLine("}");
        }
    }
}
