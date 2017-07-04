namespace symdump.exefile
{
	public class WordData : IInstruction
	{
		public readonly uint data;

		public IOperand[] operands { get; } = new IOperand[0];

		public WordData(uint data)
		{
			this.data = data;
		}

		public override string ToString()
		{
			return $".word 0x{data:x}";
		}

		public string asReadable()
		{
			return ToString();
		}
	}
}
