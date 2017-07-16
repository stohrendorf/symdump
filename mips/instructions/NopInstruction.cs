using core;

namespace mips.instructions
{
    public class NopInstruction : Instruction
    {
        public override IOperand[] operands { get; }

        public override string asReadable()
        {
            return "nop";
        }

        public override IExpressionNode toExpressionNode(IDataFlowState dataFlowState)
        {
            throw new System.NotImplementedException();
        }
    }
}
