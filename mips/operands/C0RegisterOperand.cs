using core;
using mips.disasm;

namespace mips.operands
{
    public class C0RegisterOperand : IOperand
    {
        public readonly C0Register register;

        public C0RegisterOperand(C0Register register)
        {
            this.register = register;
        }

        public C0RegisterOperand(uint data, int offset)
            : this((C0Register) (((int) data >> offset) & 0x1f))
        {
        }

        public bool Equals(IOperand other)
        {
            var o = other as C0RegisterOperand;
            return register == o?.register;
        }

        public IExpressionNode toExpressionNode(IDataFlowState dataFlowState)
        {
            throw new System.NotImplementedException();
        }

        public override string ToString()
        {
            return $"${register}";
        }
    }
}
