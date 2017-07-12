using symdump.exefile.dataflow;
using symdump.exefile.expression;
using symdump.exefile.operands;
using symdump.symfile;

namespace symdump.exefile.instructions
{
    public class ArithmeticInstruction : Instruction
    {
        public readonly Operation operation;

        public ArithmeticInstruction(Operation operation, IOperand dest, IOperand lhs, IOperand rhs)
        {
            this.operation = operation;
            if ((operation == Operation.Add || operation == Operation.Sub) && dest.Equals(lhs) && (dest is RegisterOperand) &&
                ((RegisterOperand) dest).register == Register.sp && (rhs is ImmediateOperand))
            {
                rhs = new ImmediateOperand((short)((ImmediateOperand) rhs).value);
            }
            operands = new[] {dest, lhs, rhs};
        }

        public override IOperand[] operands { get; }

        public IOperand destination => operands[0];
        public IOperand lhs => operands[1];
        public IOperand rhs => operands[2];

        public bool isInplace => destination.Equals(lhs);

        public override string asReadable()
        {
            var op = operation.toCode();

            return isInplace
                ? $"{destination} {op}= {rhs}"
                : $"{destination} = {lhs} {op} {rhs}";
        }

        public override IExpressionNode toExpressionNode(DataFlowState dataFlowState)
        {
            return new ExpressionNode(operation, lhs.toExpressionNode(dataFlowState), rhs.toExpressionNode(dataFlowState));
        }
    }
}
