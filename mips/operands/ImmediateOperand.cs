using core;
using core.expression;

namespace mips.operands
{
    public class ImmediateOperand : IOperand
    {
        public readonly long Value;

        public ImmediateOperand(long value)
        {
            Value = value;
        }

        public bool Equals(IOperand other)
        {
            var o = other as ImmediateOperand;
            return Value == o?.Value;
        }

        public IExpressionNode ToExpressionNode(IDataFlowState dataFlowState)
        {
            return new ValueNode(Value);
        }

        public override string ToString()
        {
            return Value >= 0 ? $"0x{Value:X}" : $"-0x{-Value:X}";
        }
    }
}
