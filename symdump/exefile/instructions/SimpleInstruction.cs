using System.Linq;
using symdump.exefile.operands;

namespace symdump.exefile.instructions
{
    public class SimpleInstruction : Instruction
    {
        private readonly string _format;
        private readonly string _mnemonic;

        private SimpleInstruction(string mnemonic, string format, bool isBranchDelaySlot, params IOperand[] operands)
            : base(isBranchDelaySlot)
        {
            _mnemonic = mnemonic;
            Operands = operands;
            _format = format;
        }

        public SimpleInstruction(string mnemonic, string format, params IOperand[] operands)
            : this(mnemonic, format, false, operands)
        {
        }

        public override IOperand[] Operands { get; }

        public override string ToString()
        {
            var args = string.Join(", ", Operands.Select(o => o.ToString()));
            return $"{_mnemonic} {args}".Trim();
        }

        public override string AsReadable()
        {
            return _format == null ? ToString() : string.Format(_format, Operands);
        }
    }
}
