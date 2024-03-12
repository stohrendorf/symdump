﻿using symdump.exefile.operands;

namespace symdump.exefile.instructions
{
    public abstract class Instruction
    {
        public bool IsBranchDelaySlot;

        protected Instruction(bool isBranchDelaySlot = false)
        {
            IsBranchDelaySlot = isBranchDelaySlot;
        }

        public abstract IOperand?[] Operands { get; }

        public abstract string AsReadable();
    }
}
