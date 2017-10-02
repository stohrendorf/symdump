using System.Collections.Generic;
using System.Linq;
using core.expression;

namespace core.operand
{
    public class ImmediateOperand : IOperand
    {
        public readonly long Value;

        public ImmediateOperand(long value)
        {
            Value = value;
        }

        public IEnumerable<int> TouchedRegisters => Enumerable.Empty<int>();

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
