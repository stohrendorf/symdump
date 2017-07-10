using symdump.symfile;

namespace symdump.exefile.operands
{
    public class RegisterOperand : IOperand
    {
        public readonly Register register;

        public RegisterOperand(Register register)
        {
            this.register = register;
        }

        public RegisterOperand(uint data, int offset)
            : this((Register) ((data >> offset) & 0x1f))
        {
        }
        
        public bool Equals(IOperand other)
        {
            var o = other as RegisterOperand;
            return register == o?.register;
        }

        public override string ToString()
        {
            return $"${register}";
        }
    }
}