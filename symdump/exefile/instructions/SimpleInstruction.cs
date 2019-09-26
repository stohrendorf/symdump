using System.Linq;
using symdump.exefile.operands;

namespace symdump.exefile.instructions
{
    public class SimpleInstruction : Instruction
    {
        public readonly string Format;
        public readonly string Mnemonic;

        private SimpleInstruction(string mnemonic, string format, bool isBranchDelaySlot, params IOperand[] operands)
            : base(isBranchDelaySlot)
        {
            Mnemonic = mnemonic;
            Operands = operands;
            Format = format;
        }

        public SimpleInstruction(string mnemonic, string format, params IOperand[] operands)
            : this(mnemonic, format, false, operands)
        {
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
    }
}
