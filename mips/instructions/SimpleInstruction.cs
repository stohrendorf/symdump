using System;
using System.Linq;
using core;

namespace mips.instructions
{
    public class SimpleInstruction : Instruction
    {
        public readonly string Format;
        public readonly string Mnemonic;

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
