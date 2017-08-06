using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;

namespace core.expression
{
    public class RegisterOffsetNode : IExpressionNode
    {
        public readonly int RegisterId;

        public readonly int Offset;

        [CanBeNull]
        public IMemoryLayout MemoryLayout { get; set; }

        public IEnumerable<int> UsedRegisters => Enumerable.Repeat(RegisterId, 1);
        public IEnumerable<int> UsedStack => Enumerable.Empty<int>();
        public IEnumerable<uint> UsedMemory => Enumerable.Empty<uint>();

        public RegisterOffsetNode(int registerId, int offset)
        {
            RegisterId = registerId;
            Offset = offset;
        }

        public string ToCode()
        {
            return Offset >= 0
                ? $"*(${RegisterId}+{Offset})"
                : $"*(${RegisterId}-{-Offset})";
        }

        public string TryDeref()
        {
            Debug.Assert(MemoryLayout != null);

            return MemoryLayout.GetAccessPathTo((uint) Offset);
        }

        public override string ToString()
        {
            return $"offset=${RegisterId}+{Offset} memoryLayout={MemoryLayout}";
        }
    }
}
