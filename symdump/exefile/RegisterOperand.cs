using symfile;

namespace symdump.exefile
{
	public class RegisterOperand : IOperand
	{
		public readonly Register register;

		public RegisterOperand(Register register)
		{
			this.register = register;
		}

		public RegisterOperand(uint data, int offset)
			: this((Register)((data >> offset) & 0x1f))
		{
		}

		public override string ToString()
		{
			return $"${register}";
		}
	}
}
