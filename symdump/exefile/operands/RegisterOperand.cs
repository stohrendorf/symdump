using symdump.symfile;

namespace symdump.exefile.operands
{
    public class RegisterOperand : IOperand
    {
        public readonly Register Register;

        public RegisterOperand(Register register)
        {
            Register = register;
        }

        public RegisterOperand(uint data, int offset)
            : this((Register) ((data >> offset) & 0x1f))
        {
        }

        public bool Equals(IOperand other)
        {
            var o = other as RegisterOperand;
            return Register == o?.Register;
        }

        public override string ToString()
        {
            return $"${Register}";
        }
    }
}
