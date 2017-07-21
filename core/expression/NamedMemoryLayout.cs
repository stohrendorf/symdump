using System.Diagnostics;
using JetBrains.Annotations;

namespace core.expression
{
    public class NamedMemoryLayout : IExpressionNode
    {
        [NotNull] public readonly string label;

        [NotNull]
        public IMemoryLayout memoryLayout { get; }

        public NamedMemoryLayout(string label, [NotNull] IMemoryLayout memoryLayout)
        {
            Debug.Assert(!string.IsNullOrEmpty(label));

            this.label = label;
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
