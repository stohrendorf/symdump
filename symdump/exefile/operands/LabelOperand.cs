namespace symdump.exefile.operands
{
    public class LabelOperand : IOperand
    {
        private readonly string _label;
        public readonly uint Offset;

        public LabelOperand(string label, uint offset)
        {
            _label = label;
            Offset = offset;
        }

        public bool Equals(IOperand other)
        {
            var o = other as LabelOperand;
            return _label == o?._label && Offset == o?.Offset;
        }

        public override string ToString()
        {
            return _label;
        }
    }
}
