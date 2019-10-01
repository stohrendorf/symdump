using JetBrains.Annotations;
using symdump.exefile.operands;

namespace symdump.exefile.instructions
{
    public abstract class Instruction
    {
        public bool IsBranchDelaySlot;

        protected Instruction(bool isBranchDelaySlot = false)
        {
            IsBranchDelaySlot = isBranchDelaySlot;
        }

        [NotNull] [ItemNotNull] public abstract IOperand[] Operands { get; }

        [NotNull]
        public abstract string AsReadable();
    }
}