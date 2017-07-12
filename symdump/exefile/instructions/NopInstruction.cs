using symdump.exefile.dataflow;
using symdump.exefile.expression;
using symdump.exefile.operands;

namespace symdump.exefile.instructions
{
    public class NopInstruction : Instruction
    {
        public override IOperand[] operands { get; }

        public override string asReadable()
        {
            return "nop";
        }

        public override IExpressionNode toExpressionNode(DataFlowState dataFlowState)
        {
            throw new System.NotImplementedException();
        }
    }
}