using symdump.symfile;

namespace symdump.exefile.operands
{
    public class RegisterOffsetOperand : IOperand
    {
        public readonly int offset;
        public readonly Register register;

        public RegisterOffsetOperand(Register register, int offset)
        {
            this.register = register;
            this.offset = offset;
        }

        public RegisterOffsetOperand(uint data, int shift, int offset)
            : this((Register) ((data >> shift) & 0x1f), offset)
        {
        }

        public bool Equals(IOperand other)
        {
            var o = other as RegisterOffsetOperand;
            return register == o?.register && offset == o.offset;
        }

        public override string ToString()
        {
            return $"{offset}(${register})";
        }
    }
}