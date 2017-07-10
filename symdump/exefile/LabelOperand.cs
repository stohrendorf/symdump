namespace symdump.exefile
{
    public class LabelOperand : IOperand
    {
        public readonly string label;

        public LabelOperand(string label)
        {
            this.label = label;
        }

        public override string ToString()
        {
            return label;
        }
    }
}