using System.Collections.Generic;
using System.Linq;

namespace core.expression
{
    public class RegisterNode : IExpressionNode
    {
        public readonly int RegisterId;

        public IEnumerable<int> UsedRegisters => Enumerable.Repeat(RegisterId, 1);
        public IEnumerable<int> UsedStack => Enumerable.Empty<int>();
        public IEnumerable<uint> UsedMemory => Enumerable.Empty<uint>();

        public RegisterNode(int registerId)
        {
            RegisterId = registerId;
        }

        public string ToCode()
        {
            return $"${RegisterId}";
        }

        public override string ToString()
        {
            return $"${RegisterId}";
        }
    }
}
