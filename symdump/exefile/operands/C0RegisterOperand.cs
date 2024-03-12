using symdump.exefile.disasm;

namespace symdump.exefile.operands
{
    public class C0RegisterOperand : IOperand
    {
        private readonly C0Register _register;

        private C0RegisterOperand(C0Register register)
        {
            _register = register;
        }

        public C0RegisterOperand(uint data, int offset)
            : this((C0Register) (((int) data >> offset) & 0x1f))
        {
        }

        public bool Equals(IOperand? other)
        {
            var o = other as C0RegisterOperand;
            return _register == o?._register;
        }

        public override string ToString()
        {
            return $"${_register}";
        }
    }
}
