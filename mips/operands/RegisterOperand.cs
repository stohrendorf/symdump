using core;
using core.expression;
using mips.disasm;

namespace mips.operands
{
    public class RegisterOperand : IOperand
    {
        public readonly Register register;

        public RegisterOperand(Register register)
        {
            this.register = register;
        }

        public RegisterOperand(uint data, int offset)
            : this((Register) ((data >> offset) & 0x1f))
        {
        }

        public bool Equals(IOperand other)
        {
            var o = other as RegisterOperand;
            return register == o?.register;
        }

        public IExpressionNode toExpressionNode(IDataFlowState dataFlowState)
        {
            var expression = dataFlowState.getRegisterExpression((int) register);
            return expression ?? new RegisterNode((int) register);
        }

        public override string ToString()
        {
            return $"${register}";
        }
    }
}
