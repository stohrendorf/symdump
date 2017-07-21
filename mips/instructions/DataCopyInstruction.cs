using core;
using core.expression;

namespace mips.instructions
{
    public class DataCopyInstruction : Instruction
    {
        public override IOperand[] operands { get; }
        public byte srcSize;
        public byte dstSize;

        public DataCopyInstruction(IOperand dst, byte dstSize, IOperand src, byte srcSize)
        {
            operands = new[] {dst, src};
            this.dstSize = dstSize;
            this.srcSize = srcSize;
        }

        public IOperand src => operands[1];
        public IOperand dst => operands[0];

        public override string asReadable()
        {
            return $"{dst} = {src}";
        }

        public override IExpressionNode toExpressionNode(IDataFlowState dataFlowState)
        {
            return new DataCopyNode(dst.toExpressionNode(dataFlowState), dstSize, src.toExpressionNode(dataFlowState), srcSize);
        }
    }
}
