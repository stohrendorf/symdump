namespace core
{
    public abstract class Instruction
    {
        public bool isBranchDelaySlot;

        protected Instruction(bool isBranchDelaySlot = false)
        {
            this.isBranchDelaySlot = isBranchDelaySlot;
        }

        public abstract IOperand[] operands { get; }

        public abstract string asReadable();

        public abstract IExpressionNode toExpressionNode(IDataFlowState dataFlowState);
    }
}
