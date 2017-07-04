namespace symdump.exefile
{
	public class ImmediateOperand : IOperand
	{
		public readonly int value;

		public ImmediateOperand(int value)
		{
			this.value = value;
		}

		public override string ToString()
		{
			return $"{value}";
		}
	}
}
