using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;

namespace core.expression
{
    public class NamedMemoryLayout : IExpressionNode
    {
        [NotNull] public readonly string Label;
        public readonly uint Address;

        [NotNull]
        public IMemoryLayout MemoryLayout { get; }

        public NamedMemoryLayout(string label, uint address, [NotNull] IMemoryLayout memoryLayout)
        {
            Debug.Assert(!string.IsNullOrEmpty(label));

            Label = label;
            Address = address;
            MemoryLayout = memoryLayout;
        }

        public string ToCode()
        {
            return Label;
        }

        public IEnumerable<int> UsedRegisters => Enumerable.Empty<int>();
        public IEnumerable<int> UsedStack => Enumerable.Empty<int>();
        public IEnumerable<uint> UsedMemory => Enumerable.Repeat(Address, 1);

        public override string ToString()
        {
            return $"label={Label} memoryLayout={MemoryLayout}";
        }
    }
}
