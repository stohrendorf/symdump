using symdump.exefile.dataflow;
using symdump.exefile.expression;

namespace symdump.exefile.operands
{
    public class LabelOperand : IOperand
    {
        // TODO address
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

        public IExpressionNode toExpressionNode(DataFlowState dataFlowState)
        {
            return new LabelNode(label, dataFlowState.symFile.findTypeDefinitionForLabel(label));
        }

        public override string ToString()
        {
            return label;
        }
    }
}
