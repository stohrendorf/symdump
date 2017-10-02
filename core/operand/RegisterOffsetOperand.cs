using System.Collections.Generic;
using core.expression;

namespace core.operand
{
    public class RegisterOffsetOperand : IOperand
    {
        public readonly int Offset;
        public readonly int Register;

        public RegisterOffsetOperand(int register, int offset)
        {
            Register = register;
            Offset = offset;
        }

        public IEnumerable<int> TouchedRegisters
        {
            get { yield return Register; }
        }

        public bool Equals(IOperand other)
        {
            var o = other as RegisterOffsetOperand;
            return Register == o?.Register && Offset == o.Offset;
        }

        public IExpressionNode ToExpressionNode(IDataFlowState dataFlowState)
        {
            var expression = dataFlowState.GetRegisterExpression(Register);
            if (expression == null)
                return new RegisterOffsetNode(Register, Offset);

            if (!(expression is ValueNode))
                return new DerefNode(new ExpressionNode(Operator.Add, expression, new ValueNode(Offset)));

            var address = (uint) (((ValueNode) expression).Value + Offset);
            var name = dataFlowState.DebugSource?.GetSymbolName(address);
            var typeDef = dataFlowState.DebugSource?.FindTypeDefinitionForLabel(name);
            return new NamedMemoryLayout(name, address, typeDef ?? UndefinedMemoryLayout.Instance);
        }

        public override string ToString()
        {
            return $"({Offset} + ${Register})";
        }
    }
}
