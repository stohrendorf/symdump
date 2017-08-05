using System.Collections.Generic;
using System.Linq;

namespace core.expression
{
    public class ValueNode : IExpressionNode
    {
        public readonly long value;

        public IEnumerable<int> usedRegisters => Enumerable.Empty<int>();
        public IEnumerable<int> usedStack => Enumerable.Empty<int>();
        public IEnumerable<uint> usedMemory => Enumerable.Empty<uint>();

        public ValueNode(long value)
        {
            this.value = value;
        }

        public string toCode()
        {
            return value.ToString();
        }

        public override string ToString()
        {
            return value.ToString();
        }
    }
}
