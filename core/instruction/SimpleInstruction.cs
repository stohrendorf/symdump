using System;
using System.Collections.Generic;
using System.Linq;

namespace core.instruction
{
    public class SimpleInstruction : Instruction
    {
        public readonly string Format;
        public readonly string Mnemonic;

        public override uint? JumpTarget => null;
        public override IEnumerable<int> InputRegisters => OutputRegisters;

        public override IEnumerable<int> OutputRegisters
            => Operands.SelectMany(o => o.TouchedRegisters).Distinct();

        public SimpleInstruction(string mnemonic, string format, params IOperand[] operands)
        {
            Mnemonic = mnemonic;
            Operands = operands;
            Format = format;
        }

        public override IOperand[] Operands { get; }

        public override string ToString()
        {
            var args = string.Join(", ", Operands.Select(o => o.ToString()));
            return $"{Mnemonic} {args}".Trim();
        }

        public override string AsReadable()
        {
            return Format == null ? ToString() : string.Format(Format, Operands);
        }

        public override IExpressionNode ToExpressionNode(IDataFlowState dataFlowState)
        {
            throw new Exception("Cannot convert simple instruction to expression node");
        }
    }
}
