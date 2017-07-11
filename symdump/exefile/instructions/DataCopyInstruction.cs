using System.Collections.Generic;
using symdump.exefile.expression;
using symdump.exefile.operands;
using symdump.symfile;

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

        public override IExpressionNode toExpressionNode(IReadOnlyDictionary<Register, IExpressionNode> registers)
        {
            return new DataCopyNode(to.toExpressionNode(registers), from.toExpressionNode(registers));
        }
    }
}