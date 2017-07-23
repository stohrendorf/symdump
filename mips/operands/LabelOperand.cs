using core;
using core.expression;

namespace mips.operands
{
    public class LabelOperand : IOperand
    {
        public readonly string label;
        public readonly uint address;

        public LabelOperand(string label, uint address)
        {
            this.label = label;
            this.address = address;
        }

        public bool Equals(IOperand other)
        {
            var o = other as LabelOperand;
            return label == o?.label;
        }

        public IExpressionNode toExpressionNode(IDataFlowState dataFlowState)
        {
            return new NamedMemoryLayout(label, address, dataFlowState.debugSource.findTypeDefinitionForLabel(label));
        }

        public override string ToString()
        {
            return label;
        }
    }
}
