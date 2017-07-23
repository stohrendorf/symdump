using System.Diagnostics;
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

        public override string ToString()
        {
            return $"label={label} memoryLayout={memoryLayout}";
        }
    }
}
