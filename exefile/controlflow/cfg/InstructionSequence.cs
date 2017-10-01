using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using core;
using core.util;
using JetBrains.Annotations;
using static mips.disasm.RegisterUtil;

namespace exefile.controlflow.cfg
{
    public class InstructionSequence : Node
    {
        public override string Id => $"insnseq_{InstructionList.Keys.First():x8}";

        public override IEnumerable<int> InputRegisters
            => InstructionList.Values.SelectMany(i => i.InputRegisters).Distinct();
        
        public override IEnumerable<int> OutputRegisters
            => InstructionList.Values.SelectMany(i => i.OutputRegisters).Distinct();

        public SortedDictionary<uint, Instruction> InstructionList { get; } = new SortedDictionary<uint, Instruction>();

        public InstructionSequence([NotNull] IGraph graph)
            : base(graph)
        {
        }

        public override IEnumerable<Instruction> Instructions => InstructionList.Values;

        public override bool ContainsAddress(uint address)
        {
            if (InstructionList.Count == 0)
                return false;

            return address >= InstructionList.Keys.First() && address <= InstructionList.Keys.Last();
        }

        public override void Dump(IndentedTextWriter writer, IDataFlowState dataFlowState)
        {
            foreach (var edge in Outs)
            {
                writer.WriteLine($"// {edge}");
            }

            {
                var inputs = InputRegisters.Select(RegisterStringFromInt);
                writer.WriteLine($"// input {string.Join(", ", inputs)}");
            }

            {
                var outputs = OutputRegisters.Select(RegisterStringFromInt);
                writer.WriteLine($"// output {string.Join(", ", outputs)}");
            }

            foreach (var insn in InstructionList)
            {
                dataFlowState?.Apply(insn.Value, null);
                writer.WriteLine($"0x{insn.Key:X}  {insn.Value.AsReadable()}");
            }
            dataFlowState?.DumpState(writer);
        }

        public InstructionSequence Chop(uint from)
        {
            Debug.Assert(ContainsAddress(from));
            
            var result = new InstructionSequence(Graph);
            foreach (var split in InstructionList.Where(i => i.Key >= from))
            {
                result.InstructionList.Add(split.Key, split.Value);
            }
            foreach (var rm in result.InstructionList.Keys)
            {
                InstructionList.Remove(rm);
            }

            Debug.Assert(!ContainsAddress(from));
            Debug.Assert(result.ContainsAddress(from));

            return result;
        }
    }
}
