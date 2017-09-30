using System.Collections.Generic;
using core;
using core.expression;
using mips.operands;
using static mips.disasm.RegisterUtil;

namespace mips.instructions
{
    public class DataCopyInstruction : Instruction
    {
        public override IOperand[] Operands { get; }
        public readonly byte SrcSize;
        public readonly byte DstSize;

        public override uint? JumpTarget => null;

        public DataCopyInstruction(IOperand dst, byte dstSize, IOperand src, byte srcSize)
        {
            Operands = new[] {dst, src};
            DstSize = dstSize;
            SrcSize = srcSize;
        }

        public IOperand Src => Operands[1];
        public IOperand Dst => Operands[0];

        public override IEnumerable<int> OutputRegisters
        {
            get
            {
                switch (Dst)
                {
                    case RegisterOperand r:
                        yield return ToInt(r.Register);
                        break;
                    case C0RegisterOperand r:
                        yield return ToInt(r.Register);
                        break;
                    case C2RegisterOperand r:
                        yield return ToInt(r.Register);
                        break;
                }
            }
        }

        public override IEnumerable<int> InputRegisters
        {
            get
            {
                switch (Src)
                {
                    case RegisterOperand r:
                        yield return ToInt(r.Register);
                        break;
                    case RegisterOffsetOperand r:
                        yield return ToInt(r.Register);
                        break;
                    case C0RegisterOperand r:
                        yield return ToInt(r.Register);
                        break;
                    case C2RegisterOperand r:
                        yield return ToInt(r.Register);
                        break;
                }
            }
        }

        public override string AsReadable()
        {
            return $"{Dst} = {Src}";
        }

        public override IExpressionNode ToExpressionNode(IDataFlowState dataFlowState)
        {
            return new DataCopyNode(Dst.ToExpressionNode(dataFlowState), DstSize, Src.ToExpressionNode(dataFlowState), SrcSize);
        }
    }
}
