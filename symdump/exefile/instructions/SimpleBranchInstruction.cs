using symdump.exefile.operands;

namespace symdump.exefile.instructions
{
    public class SimpleBranchInstruction : SimpleInstruction
    {
        public readonly bool isUnconditional;

        public SimpleBranchInstruction(string mnemonic, string format, bool isUnconditional, params IOperand[] operands)
            : base(mnemonic, format, false, operands)
        {
            this.isUnconditional = isUnconditional;
        }
    }
}