using core;
using core.expression;
using mips.disasm;

namespace mips.operands
{
    public class RegisterOffsetOperand : IOperand
    {
        public readonly int offset;
        public readonly Register register;

        public RegisterOffsetOperand(Register register, int offset)
        {
            this.register = register;
            this.offset = offset;
        }

        public RegisterOffsetOperand(uint data, int shift, int offset)
            : this((Register) ((data >> shift) & 0x1f), offset)
        {
        }

        public bool Equals(IOperand other)
        {
            var o = other as RegisterOffsetOperand;
            return register == o?.register && offset == o.offset;
        }

        public IExpressionNode toExpressionNode(IDataFlowState dataFlowState)
        {
            var expression = dataFlowState.getRegisterExpression((int) register);
            if (expression == null)
                return new RegisterOffsetNode((int) register, offset);

            if (!(expression is ValueNode))
                return new DerefNode(new ExpressionNode(Operator.Add, expression, new ValueNode(offset)));

            var address = (uint) (((ValueNode) expression).value + offset);
            var name = dataFlowState.debugSource.getSymbolName(address);
            return new NamedMemoryLayout(name, address, dataFlowState.debugSource.findTypeDefinitionForLabel(name));
        }

        public override string ToString()
        {
            return $"{offset}(${register})";
        }
    }
}
