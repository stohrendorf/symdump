using System.Collections.Generic;
using symdump.exefile.expression;
using symdump.exefile.operands;
using symdump.symfile;

namespace symdump.exefile.instructions
{
    public class NopInstruction : Instruction
    {
        public override IOperand[] operands { get; }

        public override string asReadable()
        {
            return "nop";
        }

        public override IExpressionNode toExpressionNode(IReadOnlyDictionary<Register, IExpressionNode> registers)
        {
            throw new System.NotImplementedException();
        }
    }
}