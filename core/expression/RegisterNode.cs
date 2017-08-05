using System.Collections.Generic;
using System.Linq;

namespace core.expression
{
    public class RegisterNode : IExpressionNode
    {
        public readonly int registerId;

        public IEnumerable<int> usedRegisters => Enumerable.Repeat(registerId, 1);
        public IEnumerable<int> usedStack => Enumerable.Empty<int>();
        public IEnumerable<uint> usedMemory => Enumerable.Empty<uint>();

        public RegisterNode(int registerId)
        {
            this.registerId = registerId;
        }

        public string toCode()
        {
            return $"${registerId}";
        }

        public override string ToString()
        {
            return $"${registerId}";
        }
    }
}
