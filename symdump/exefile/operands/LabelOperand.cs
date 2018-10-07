namespace symdump.exefile.operands
{
    public class LabelOperand : IOperand
    {
        private readonly string _label;

        public LabelOperand(string label)
        {
            _label = label;
        }

        public bool Equals(IOperand other)
        {
            var o = other as LabelOperand;
            return _label == o?._label;
        }

        public override string ToString()
        {
            return _label;
        }
    }
}
