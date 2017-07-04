namespace symdump.exefile
{
	public class Location
	{
		public readonly uint offset;

		public Location(uint offset)
		{
			this.offset = offset;
		}

		public IInstruction instruction { get; }
	}
}

