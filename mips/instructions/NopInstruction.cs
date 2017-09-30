using System.Collections.Generic;
using System.Linq;
using core;

namespace mips.instructions
{
    public class NopInstruction : Instruction
    {
        public override IOperand[] Operands { get; } = new IOperand[0];

        public override uint? JumpTarget => null;
        public override IEnumerable<int> InputRegisters => Enumerable.Empty<int>();
        public override IEnumerable<int> OutputRegisters => Enumerable.Empty<int>();

        public override string AsReadable()
        {
            return "nop";
        }

        public override IExpressionNode ToExpressionNode(IDataFlowState dataFlowState)
        {
            throw new System.NotImplementedException();
        }
    }
}
