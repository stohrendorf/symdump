using symdump.symfile;

namespace symdump.exefile.operands
{
    public class RegisterOffsetOperand : IOperand
    {
        public readonly int Offset;
        public readonly Register Register;

        private RegisterOffsetOperand(Register register, int offset)
        {
            Register = register;
            Offset = offset;
        }

        public RegisterOffsetOperand(uint data, int shift, int offset)
            : this((Register) ((data >> shift) & 0x1f), offset)
        {
        }

        public bool Equals(IOperand other)
        {
            var o = other as RegisterOffsetOperand;
            return Register == o?.Register && Offset == o.Offset;
        }

        public override string ToString()
        {
            return Offset == 0 ? $"${Register}" : $"({Offset}+${Register})";
        }
    }
}
