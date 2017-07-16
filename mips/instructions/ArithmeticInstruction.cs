using System;
using core;
using core.expression;
using mips.disasm;
using mips.operands;

namespace mips.instructions
{
    public class ArithmeticInstruction : Instruction
    {
        public readonly Operator @operator;

        public ArithmeticInstruction(Operator @operator, IOperand dest, IOperand lhs, IOperand rhs)
        {
            this.@operator = @operator;
            if ((@operator == Operator.Add || @operator == Operator.Sub) && dest.Equals(lhs) && (dest is RegisterOperand) &&
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
            var op = @operator.toCode();

            return isInplace
                ? $"{destination} {op}= {rhs}"
                : $"{destination} = {lhs} {op} {rhs}";
        }

        public override IExpressionNode toExpressionNode(IDataFlowState dataFlowState)
        {
            var lhsExpr = lhs.toExpressionNode(dataFlowState);
            var rhsExpr = rhs.toExpressionNode(dataFlowState);
            
            if (!(lhsExpr is ValueNode) || !(rhsExpr is ValueNode))
            {
                return new ExpressionNode(@operator, lhsExpr, rhsExpr);
            }

            var lhsVal = ((ValueNode) lhsExpr).value;
            var rhsVal = ((ValueNode) rhsExpr).value;

            switch (@operator)
            {
                case Operator.Add:
                    return new ValueNode(lhsVal + rhsVal);
                case Operator.Sub:
                    return new ValueNode(lhsVal - rhsVal);
                case Operator.Mul:
                    return new ValueNode(lhsVal * rhsVal);
                case Operator.Div:
                    return new ValueNode(lhsVal / rhsVal);
                case Operator.Shl:
                    return new ValueNode(lhsVal << (int)rhsVal);
                case Operator.Shr:
                    return new ValueNode(lhsVal >> (int)rhsVal);
                case Operator.Sar:
                    return new ValueNode(lhsVal >> (int)rhsVal);
                case Operator.BitAnd:
                    return new ValueNode(lhsVal & rhsVal);
                case Operator.BitOr:
                    return new ValueNode(lhsVal | rhsVal);
                case Operator.BitXor:
                    return new ValueNode(lhsVal ^ rhsVal);
                case Operator.Equal:
                case Operator.NotEqual:
                case Operator.Less:
                case Operator.SignedLess:
                case Operator.Greater:
                case Operator.SignedGreater:
                case Operator.LessEqual:
                case Operator.SignedLessEqual:
                case Operator.GreaterEqual:
                case Operator.SignedGreaterEqual:
                    return new ExpressionNode(@operator, lhs.toExpressionNode(dataFlowState), rhs.toExpressionNode(dataFlowState));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
