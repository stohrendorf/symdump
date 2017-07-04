using symdump.exefile.disasm;

namespace symdump.exefile
{
	public class C0RegisterOperand : IOperand
	{
		public readonly C0Register register;

		public C0RegisterOperand(C0Register register)
		{
			this.register = register;
		}

		public C0RegisterOperand(uint data, int offset)
			: this((C0Register)(((int)data >> offset) & 0x1f))
		{
		}

		public override string ToString()
		{
			return $"${register}";
		}
	}
}

