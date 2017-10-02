using System.Collections.Generic;
using System.Linq;
using core.util;
using JetBrains.Annotations;

namespace core.cfg
{
    public class InstructionCollection : Node
    {
        public override string Id => $"insncoll_{InstructionList.First().Key:x8}";

        public override IEnumerable<int> InputRegisters
            => InstructionList.SelectMany(i => i.Value.InputRegisters).Distinct();

        public override IEnumerable<int> OutputRegisters
            => InstructionList.SelectMany(i => i.Value.OutputRegisters).Distinct();

        [NotNull] public IList<KeyValuePair<uint, Instruction>> InstructionList { get; }

        public InstructionCollection([NotNull] IGraph graph)
            : base(graph)
        {
            InstructionList = new List<KeyValuePair<uint, Instruction>>();
        }

        public InstructionCollection([NotNull] InstructionSequence sequence)
            : base(sequence.Graph)
        {
            InstructionList = new List<KeyValuePair<uint, Instruction>>(sequence.InstructionList);
        }

        public override IEnumerable<Instruction> Instructions => InstructionList.Select(i => i.Value);

        public override bool ContainsAddress(uint address)
        {
            return InstructionList.Any(i => i.Key == address);
        }

        public override void Dump(IndentedTextWriter writer, IDataFlowState dataFlowState)
        {
            foreach (var edge in Outs)
            {
                writer.WriteLine($"// {edge}");
            }

            {
                var inputs = InputRegisters.Select(i => $"${i}");
                writer.WriteLine($"// input {string.Join(", ", inputs)}");
            }

            {
                var outputs = OutputRegisters.Select(i => $"${i}");
                writer.WriteLine($"// output {string.Join(", ", outputs)}");
            }

            foreach (var insn in InstructionList)
            {
                dataFlowState?.Apply(insn.Value, null);
                writer.WriteLine($"0x{insn.Key:X}  {insn.Value.AsReadable()}");
            }
            dataFlowState?.DumpState(writer);
        }
    }
}
