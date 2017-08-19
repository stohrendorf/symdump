using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using core;
using core.util;
using JetBrains.Annotations;

namespace exefile.controlflow
{
    public class WhileBlock : IBlock
    {
        [NotNull]
        public IBlock Condition { get; }

        [NotNull]
        public IBlock Body { get; }

        [NotNull]
        public IBlock Exit { get; private set; }

        public readonly bool InvertedCondition;

        public WhileBlock([NotNull] IBlock condition, [NotNull] IBlock body, [NotNull] IBlock exit,
            bool invertedCondition)
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

        public void UpdateReferences(IReadOnlyDictionary<uint, IBlock> blocks, ISet<uint> processed)
        {
            if (!processed.Add(Start))
                return;
            
            if (blocks.ContainsKey(Exit.Start))
                Exit = blocks[Exit.Start];
            Exit.UpdateReferences(blocks, processed);
        }
    }
}
