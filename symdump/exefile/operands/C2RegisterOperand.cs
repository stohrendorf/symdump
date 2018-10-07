using symdump.exefile.disasm;

namespace symdump.exefile.operands
{
    public class C2RegisterOperand : IOperand
    {
        private readonly C2Register _register;

        private C2RegisterOperand(C2Register register)
        {
            _register = register;
        }

        public C2RegisterOperand(uint data, int offset)
            : this((C2Register) (((int) data >> offset) & 0x1f))
        {
        }

        public bool Equals(IOperand other)
        {
            var o = other as C2RegisterOperand;
            return _register == o?._register;
        }

        public override string ToString()
        {
            return $"${_register}";
        }
    }
}
