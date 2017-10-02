using System.Collections.Generic;
using core.expression;

namespace core.operand
{
    public class RegisterOperand : IOperand
    {
        public readonly int Register;

        public RegisterOperand(int register)
        {
            Register = register;
        }

        public IEnumerable<int> TouchedRegisters
        {
            get { yield return Register; }
        }

        public bool Equals(IOperand other)
        {
            var o = other as RegisterOperand;
            return Register == o?.Register;
        }

        public IExpressionNode ToExpressionNode(IDataFlowState dataFlowState)
        {
            var expression = dataFlowState.GetRegisterExpression(Register);
            return expression ?? new RegisterNode(Register);
        }

        public override string ToString()
        {
            return $"${Register}";
        }
    }
}
