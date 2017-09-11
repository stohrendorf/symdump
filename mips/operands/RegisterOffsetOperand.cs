using System.Diagnostics;
using System.Reflection.Metadata;
using core;
using core.expression;
using mips.disasm;

namespace mips.operands
{
    public class RegisterOffsetOperand : IOperand
    {
        public readonly int Offset;
        public readonly Register Register;

        public RegisterOffsetOperand(Register register, int offset)
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

        public IExpressionNode ToExpressionNode(IDataFlowState dataFlowState)
        {
            var expression = dataFlowState.GetRegisterExpression((int) Register);
            if (expression == null)
                return new RegisterOffsetNode((int) Register, Offset);

            if (!(expression is ValueNode))
                return new DerefNode(new ExpressionNode(Operator.Add, expression, new ValueNode(Offset)));

            var address = (uint) (((ValueNode) expression).Value + Offset);
            var name = dataFlowState.DebugSource.GetSymbolName(address);
            var typeDef = dataFlowState.DebugSource.FindTypeDefinitionForLabel(name);
            Debug.Assert(typeDef != null);
            return new NamedMemoryLayout(name, address, typeDef);
        }

        public override string ToString()
        {
            return $"{Offset}(${Register})";
        }
    }
}
