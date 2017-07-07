using System.Linq;

namespace symdump.exefile
{
	public class SimpleInstruction : Instruction
	{
		public readonly string mnemonic;

		public readonly string format;

		public override IOperand[] operands { get; }

		public SimpleInstruction(string mnemonic, string format, params IOperand[] operands)
		{
			this.mnemonic = mnemonic;
			this.operands = operands;
			this.format = format;
		}

		public override string ToString()
		{
			var args = string.Join(", ", operands.Select(o => o.ToString()));
			return $"{mnemonic} {args}".Trim();
		}

		public override string asReadable()
		{
			if(format == null)
				return ToString();

			return string.Format(format, operands);
		}
	}
}

