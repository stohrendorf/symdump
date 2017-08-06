using core;
using core.expression;
using mips.disasm;

namespace mips.operands
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

        public IExpressionNode ToExpressionNode(IDataFlowState dataFlowState)
        {
            var expression = dataFlowState.GetRegisterExpression((int) Register);
            return expression ?? new RegisterNode((int) Register);
        }

        public override string ToString()
        {
            return $"${Register}";
        }
    }
}
