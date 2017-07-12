using symdump.exefile.dataflow;
using symdump.exefile.expression;
using symdump.exefile.operands;

namespace symdump.exefile.instructions
{
    public class DataCopyInstruction : Instruction
    {
        public override IOperand[] operands { get; }

        public DataCopyInstruction(IOperand to, IOperand from)
        {
            operands = new[] {to, from};
        }

        public IOperand from => operands[1];
        public IOperand to => operands[0];

        public override string asReadable()
        {
            return $"{to} = {from}";
        }

        public override IExpressionNode toExpressionNode(DataFlowState dataFlowState)
        {
            return new DataCopyNode(to.toExpressionNode(dataFlowState), from.toExpressionNode(dataFlowState));
        }
    }
}