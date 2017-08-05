using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;

namespace core.expression
{
    public class RegisterOffsetNode : IExpressionNode
    {
        public readonly int registerId;

        public readonly int offset;

        [CanBeNull]
        public IMemoryLayout memoryLayout { get; set; }

        public IEnumerable<int> usedRegisters => Enumerable.Repeat(registerId, 1);
        public IEnumerable<int> usedStack => Enumerable.Empty<int>();
        public IEnumerable<uint> usedMemory => Enumerable.Empty<uint>();

        public RegisterOffsetNode(int registerId, int offset)
        {
            this.registerId = registerId;
            this.offset = offset;
        }

        public string toCode()
        {
            return offset >= 0
                ? $"*(${registerId}+{offset})"
                : $"*(${registerId}-{-offset})";
        }

        public string tryDeref()
        {
            Debug.Assert(memoryLayout != null);

            return memoryLayout.getAccessPathTo((uint) offset);
        }

        public override string ToString()
        {
            return $"offset=${registerId}+{offset} memoryLayout={memoryLayout}";
        }
    }
}
