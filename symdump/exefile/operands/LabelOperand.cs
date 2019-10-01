using System;
using JetBrains.Annotations;

namespace symdump.exefile.operands
{
    public class LabelOperand : IOperand
    {
        [NotNull] private readonly string _label;
        private readonly uint _offset;

        public LabelOperand([NotNull] string label, uint offset)
        {
            if (string.IsNullOrEmpty(label))
                throw new ArgumentException("Label must not be empty", nameof(label));
            _label = label;
            _offset = offset;
        }

        public bool Equals(IOperand other)
        {
            var o = other as LabelOperand;
            return _label == o?._label && _offset == o?._offset;
        }

        public override string ToString()
        {
            return $"&{_label}";
        }
    }
}
