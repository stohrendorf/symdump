using symdump.exefile.disasm;

namespace symdump.exefile
{
	public class C2RegisterOperand : IOperand
	{
		public readonly C2DataRegister register;

		public C2RegisterOperand(C2DataRegister register)
		{
			this.register = register;
		}

		public C2RegisterOperand(uint data, int offset)
			: this((C2DataRegister)(((int)data >> offset) & 0x1f))
		{
		}

		public override string ToString()
		{
			return $"${register}";
		}
	}
}

