using System.Linq;

namespace symdump.exefile
{
    public class SimpleInstruction : Instruction
    {
        public readonly string format;
        public readonly string mnemonic;

        public SimpleInstruction(string mnemonic, string format, bool isBranchDelaySlot, params IOperand[] operands)
            : base(isBranchDelaySlot)
        {
            this.mnemonic = mnemonic;
            this.operands = operands;
            this.format = format;
        }

        public SimpleInstruction(string mnemonic, string format, params IOperand[] operands)
            : this(mnemonic, format, false, operands)
        {
        }

        public override IOperand[] operands { get; }

        public override string ToString()
        {
            var args = string.Join(", ", operands.Select(o => o.ToString()));
            return $"{mnemonic} {args}".Trim();
        }

        public override string asReadable()
        {
            return format == null ? ToString() : string.Format(format, operands);
        }
    }
}