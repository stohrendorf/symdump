namespace symdump.exefile.operands
{
    public class LabelOperand : IOperand
    {
        public readonly string label;

        public LabelOperand(string label)
        {
            this.label = label;
        }

        public bool Equals(IOperand other)
        {
            var o = other as LabelOperand;
            return label == o?.label;
        }

        public override string ToString()
        {
            return label;
        }
    }
}