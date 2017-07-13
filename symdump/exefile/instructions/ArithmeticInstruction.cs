using System;
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
            var lhsExpr = lhs.toExpressionNode(dataFlowState);
            var rhsExpr = rhs.toExpressionNode(dataFlowState);
            
            if (!(lhsExpr is ValueNode) || !(rhsExpr is ValueNode))
            {
                return new ExpressionNode(operation, lhsExpr, rhsExpr);
            }

            var lhsVal = ((ValueNode) lhsExpr).value;
            var rhsVal = ((ValueNode) rhsExpr).value;

            switch (operation)
            {
                case Operation.Add:
                    return new ValueNode(lhsVal + rhsVal);
                case Operation.Sub:
                    return new ValueNode(lhsVal - rhsVal);
                case Operation.Mul:
                    return new ValueNode(lhsVal * rhsVal);
                case Operation.Div:
                    return new ValueNode(lhsVal / rhsVal);
                case Operation.Shl:
                    return new ValueNode(lhsVal << (int)rhsVal);
                case Operation.Shr:
                    return new ValueNode(lhsVal >> (int)rhsVal);
                case Operation.Sar:
                    return new ValueNode(lhsVal >> (int)rhsVal);
                case Operation.BitAnd:
                    return new ValueNode(lhsVal & rhsVal);
                case Operation.BitOr:
                    return new ValueNode(lhsVal | rhsVal);
                case Operation.BitXor:
                    return new ValueNode(lhsVal ^ rhsVal);
                case Operation.Equal:
                case Operation.NotEqual:
                case Operation.Less:
                case Operation.SignedLess:
                case Operation.Greater:
                case Operation.SignedGreater:
                case Operation.LessEqual:
                case Operation.SignedLessEqual:
                case Operation.GreaterEqual:
                case Operation.SignedGreaterEqual:
                    return new ExpressionNode(operation, lhs.toExpressionNode(dataFlowState), rhs.toExpressionNode(dataFlowState));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
