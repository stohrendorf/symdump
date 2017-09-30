using System.Collections.Generic;
using System.Linq;
using core;
using core.expression;
using mips.operands;
using static mips.disasm.RegisterUtil;

namespace mips.instructions
{
    public class ConditionalBranchInstruction : Instruction
    {
        public readonly Operator Operator;

        public override uint? JumpTarget => (Target as LabelOperand)?.Address;

        public ConditionalBranchInstruction(Operator @operator, IOperand lhs, IOperand rhs, IOperand target)
        {
            Operator = @operator;
            Operands = new[] {lhs, rhs, target};
        }

        public IOperand Lhs => Operands[0];
        public IOperand Rhs => Operands[1];
        public IOperand Target => Operands[2];

        public override IOperand[] Operands { get; }

        public override IEnumerable<int> OutputRegisters => Enumerable.Empty<int>();

        public override IEnumerable<int> InputRegisters
        {
            get
            {
                switch (Lhs)
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

                switch (Rhs)
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
            var op = Operator.AsCode();

            return $"if({Lhs} {op} {Rhs}) goto {Target}";
        }

        public override IExpressionNode ToExpressionNode(IDataFlowState dataFlowState)
        {
            return new ConditionalBranchNode(Operator, Lhs.ToExpressionNode(dataFlowState),
                Rhs.ToExpressionNode(dataFlowState), Target.ToExpressionNode(dataFlowState) as NamedMemoryLayout);
        }
    }
}
