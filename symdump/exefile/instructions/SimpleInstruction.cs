using System.Linq;
using JetBrains.Annotations;
using symdump.exefile.operands;

namespace symdump.exefile.instructions
{
    public class SimpleInstruction : Instruction
    {
        [CanBeNull] public readonly string Format;
        [NotNull] public readonly string Mnemonic;

        private SimpleInstruction([NotNull] string mnemonic, [CanBeNull] string format, bool isBranchDelaySlot,
            [NotNull] [ItemNotNull] params IOperand[] operands)
            : base(isBranchDelaySlot)
        {
            Mnemonic = mnemonic;
            Operands = operands;
            Format = format;
        }

        public SimpleInstruction([NotNull] string mnemonic, [CanBeNull] string format,
            [NotNull] [ItemNotNull] params IOperand[] operands)
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