using core;
using core.expression;

namespace mips.operands
{
    public class LabelOperand : IOperand
    {
        public readonly string Label;
        public readonly uint Address;

        public LabelOperand(string label, uint address)
        {
            Label = label;
            Address = address;
        }

        public bool Equals(IOperand other)
        {
            var o = other as LabelOperand;
            return Label == o?.Label;
        }

        public IExpressionNode ToExpressionNode(IDataFlowState dataFlowState)
        {
            return new NamedMemoryLayout(Label, Address, dataFlowState.DebugSource.FindTypeDefinitionForLabel(Label));
        }

        public override string ToString()
        {
            return Label;
        }
    }
}
