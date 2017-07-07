using System.Collections.Generic;

namespace symdump.exefile
{
    public abstract class Instruction
    {
        public abstract IOperand[] operands { get; }

        public abstract string asReadable();
    }
}