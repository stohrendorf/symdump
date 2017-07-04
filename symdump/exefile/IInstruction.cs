namespace symdump.exefile
{
	public interface IInstruction
	{
		IOperand[] operands { get; }

		string asReadable();
	}
}

