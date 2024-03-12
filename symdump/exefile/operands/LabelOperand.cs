using System;

namespace symdump.exefile.operands
{
    public class LabelOperand : IOperand
    {
        private readonly string? _label;
        private readonly uint _offset;

        public LabelOperand(string? label, uint offset)
        {
            if (string.IsNullOrEmpty(label))
                throw new ArgumentException("Label must not be empty", nameof(label));
            _label = label;
            _offset = offset;
        }

        public bool Equals(IOperand? other)
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
