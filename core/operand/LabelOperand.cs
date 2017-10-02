using System.Collections.Generic;
using System.Linq;
using core.expression;

namespace core.operand
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

        public IEnumerable<int> TouchedRegisters => Enumerable.Empty<int>();

        public bool Equals(IOperand other)
        {
            var o = other as LabelOperand;
            return Label == o?.Label;
        }

        public IExpressionNode ToExpressionNode(IDataFlowState dataFlowState)
        {
            var typeDef = dataFlowState.DebugSource?.FindTypeDefinitionForLabel(Label);
            return new NamedMemoryLayout(Label, Address, typeDef ?? UndefinedMemoryLayout.Instance);
        }

        public override string ToString()
        {
            return Label;
        }
    }
}
