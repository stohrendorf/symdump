using core;
using mips.disasm;

namespace mips.operands
{
    public class C2RegisterOperand : IOperand
    {
        public readonly C2Register Register;

        public C2RegisterOperand(C2Register register)
        {
            Register = register;
        }

        public C2RegisterOperand(uint data, int offset)
            : this((C2Register) (((int) data >> offset) & 0x1f))
        {
        }

        public bool Equals(IOperand other)
        {
            var o = other as C2RegisterOperand;
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
