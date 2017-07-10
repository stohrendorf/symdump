namespace symdump.exefile
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
    }
}