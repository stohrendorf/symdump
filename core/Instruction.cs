using JetBrains.Annotations;

namespace core
{
    public abstract class Instruction
    {
        public bool isBranchDelaySlot;

        protected Instruction(bool isBranchDelaySlot = false)
        {
            this.isBranchDelaySlot = isBranchDelaySlot;
        }

        [NotNull]
        [ItemNotNull]
        public abstract IOperand[] operands { get; }

        [NotNull]
        public abstract string asReadable();

        [NotNull]
        public abstract IExpressionNode toExpressionNode([NotNull] IDataFlowState dataFlowState);
    }
}