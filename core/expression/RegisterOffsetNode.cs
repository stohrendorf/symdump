using System.Diagnostics;
using JetBrains.Annotations;

namespace core.expression
{
    public class RegisterOffsetNode : IExpressionNode
    {
        public readonly int registerId;

        public readonly int offset;

        [CanBeNull]
        public IMemoryLayout memoryLayout { get; set; }

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
    }
}