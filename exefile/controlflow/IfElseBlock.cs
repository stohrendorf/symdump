using System.Collections.Generic;
using System.IO;
using System.Text;
using core;
using core.util;
using JetBrains.Annotations;

namespace exefile.controlflow
{
    public class IfElseBlock : IBlock
    {
        [NotNull]
        public IBlock Condition { get; private set; }

        [NotNull]
        public IBlock TrueBody { get; private set; }

        [NotNull]
        public IBlock FalseBody { get; private set; }

        [NotNull]
        public IBlock Exit { get; private set; }

        public IfElseBlock([NotNull] IBlock condition, [NotNull] IBlock trueBody, [NotNull] IBlock falseBody,
            [NotNull] IBlock exit)
        {
            Condition = condition;
            TrueBody = trueBody;
            FalseBody = falseBody;
            Exit = exit;
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
                foreach (var insn in TrueBody.Instructions) tmp.Add(insn.Key, insn.Value);
                foreach (var insn in FalseBody.Instructions) tmp.Add(insn.Key, insn.Value);
                return tmp;
            }
        }

        public ExitType? ExitType => controlflow.ExitType.Unconditional;

        public bool ContainsAddress(uint address) =>
            Condition.ContainsAddress(address) || TrueBody.ContainsAddress(address) ||
            FalseBody.ContainsAddress(address);

        public override string ToString()
        {
            var sb = new StringBuilder();
            var writer = new IndentedTextWriter(new StringWriter(sb));
            Dump(writer);
            return sb.ToString();
        }

        public void Dump(IndentedTextWriter writer)
        {
            writer.WriteLine("if{");
            ++writer.Indent;
            Condition.Dump(writer);
            --writer.Indent;
            writer.WriteLine("} {");
            ++writer.Indent;
            TrueBody.Dump(writer);
            --writer.Indent;
            writer.WriteLine("} else {");
            ++writer.Indent;
            FalseBody.Dump(writer);
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
