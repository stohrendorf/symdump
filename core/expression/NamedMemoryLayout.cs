using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;

namespace core.expression
{
    public class NamedMemoryLayout : IExpressionNode
    {
        [NotNull] public readonly string label;
        public readonly uint address;

        [NotNull]
        public IMemoryLayout memoryLayout { get; }

        public NamedMemoryLayout(string label, uint address, [NotNull] IMemoryLayout memoryLayout)
        {
            Debug.Assert(!string.IsNullOrEmpty(label));

            this.label = label;
            this.address = address;
            this.memoryLayout = memoryLayout;
        }

        public string toCode()
        {
            return label;
        }

        public IEnumerable<int> usedRegisters => Enumerable.Empty<int>();
        public IEnumerable<int> usedStack => Enumerable.Empty<int>();
        public IEnumerable<uint> usedMemory => Enumerable.Repeat(address, 1);

        public override string ToString()
        {
            return $"label={label} memoryLayout={memoryLayout}";
        }
    }
}
