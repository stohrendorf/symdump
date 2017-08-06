using core;
using mips.disasm;

namespace mips.operands
{
    public class C0RegisterOperand : IOperand
    {
        public readonly C0Register Register;

        public C0RegisterOperand(C0Register register)
        {
            Register = register;
        }

        public C0RegisterOperand(uint data, int offset)
            : this((C0Register) (((int) data >> offset) & 0x1f))
        {
        }

        public bool Equals(IOperand other)
        {
            var o = other as C0RegisterOperand;
            return Register == o?.Register;
        }

        public IExpressionNode ToExpressionNode(IDataFlowState dataFlowState)
        {
            throw new System.NotImplementedException();
        }

        public override string ToString()
        {
            return $"${Register}";
        }
    }
}
