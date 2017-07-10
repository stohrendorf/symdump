using System;
using symdump.exefile.operands;

namespace symdump.exefile.instructions
{
    public class ArithmeticInstruction : Instruction
    {
        public enum Operation
        {
            Add,
            Sub,
            Mul,
            Div,
            Shl,
            Shr,
            Sar,
            BitAnd,
            BitOr,
            BitXor
        }

        public readonly Operation operation;

        public ArithmeticInstruction(Operation operation, IOperand dest, IOperand lhs, IOperand rhs)
        {
            this.operation = operation;
            operands = new[] {dest, lhs, rhs};
        }

        public override IOperand[] operands { get; }

        public IOperand destination => operands[0];
        public IOperand lhs => operands[1];
        public IOperand rhs => operands[2];

        public bool isInplace => destination.Equals(lhs);

        public override string asReadable()
        {
            string op;
            switch (operation)
            {
                case Operation.Add:
                    op = "+";
                    break;
                case Operation.Sub:
                    op = "-";
                    break;
                case Operation.Mul:
                    op = "*";
                    break;
                case Operation.Div:
                    op = "/";
                    break;
                case Operation.Shl:
                    op = "<<";
                    break;
                case Operation.Shr:
                    op = ">>>";
                    break;
                case Operation.Sar:
                    op = ">>";
                    break;
                case Operation.BitAnd:
                    op = "&";
                    break;
                case Operation.BitOr:
                    op = "|";
                    break;
                case Operation.BitXor:
                    op = "^";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return isInplace
                ? $"{destination} {op}= {rhs}"
                : $"{destination} = {lhs} {op} {rhs}";
        }
    }
}