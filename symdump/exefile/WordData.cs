namespace symdump.exefile
{
	public class WordData : Instruction
	{
		public readonly uint data;

		public override IOperand[] operands { get; } = new IOperand[0];

		public WordData(uint data)
		{
			this.data = data;
		}

		public override string ToString()
		{
			return $".word 0x{data:x}";
		}

		public override string asReadable()
		{
			return ToString();
		}
	}
}
