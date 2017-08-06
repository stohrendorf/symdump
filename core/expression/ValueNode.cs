using System.Collections.Generic;
using System.Linq;

namespace core.expression
{
    public class ValueNode : IExpressionNode
    {
        public readonly long Value;

        public IEnumerable<int> UsedRegisters => Enumerable.Empty<int>();
        public IEnumerable<int> UsedStack => Enumerable.Empty<int>();
        public IEnumerable<uint> UsedMemory => Enumerable.Empty<uint>();

        public ValueNode(long value)
        {
            Value = value;
        }

        public string ToCode()
        {
            return Value.ToString();
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
