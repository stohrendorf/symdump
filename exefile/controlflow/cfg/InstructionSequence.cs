using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using core;
using core.util;
using JetBrains.Annotations;

namespace exefile.controlflow.cfg
{
    public class InstructionSequence : Node
    {
        public override string Id => $"insnseq_{Start:x8}";

        public InstructionSequence([NotNull] IGraph graph)
            : base(graph)
        {
        }

        public override SortedDictionary<uint, Instruction> Instructions { get; } =
            new SortedDictionary<uint, Instruction>();

        public override bool ContainsAddress(uint address)
        {
            if (Instructions.Count == 0)
                return false;

            return address >= Instructions.Keys.First() && address <= Instructions.Keys.Last();
        }

        public override void Dump(IndentedTextWriter writer)
        {
            foreach (var edge in Outs)
            {
                writer.WriteLine($"// {edge}");
            }

            foreach (var insn in Instructions)
            {
                writer.WriteLine($"0x{insn.Key:X}  {insn.Value.AsReadable()}");
            }
        }

        public InstructionSequence Chop(uint from)
        {
            Debug.Assert(ContainsAddress(from));
            
            var result = new InstructionSequence(Graph);
            foreach (var split in Instructions.Where(i => i.Key >= from))
            {
                result.Instructions.Add(split.Key, split.Value);
            }
            foreach (var rm in result.Instructions.Keys)
            {
                Instructions.Remove(rm);
            }

            Debug.Assert(!ContainsAddress(from));
            Debug.Assert(result.ContainsAddress(from));

            return result;
        }
    }
}
