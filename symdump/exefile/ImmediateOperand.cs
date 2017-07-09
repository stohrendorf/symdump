﻿namespace symdump.exefile
{
	public class ImmediateOperand : IOperand
	{
		public readonly long value;

		public ImmediateOperand(long value)
		{
			this.value = value;
		}

		public override string ToString()
		{
			return value >= 0 ? $"0x{value:X}" : $"-0x{-value:X}";
		}
	}
}
