using symdump.symfile;

namespace symdump.exefile
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

        public override string ToString()
        {
            return $"{offset}(${register})";
        }
    }
}