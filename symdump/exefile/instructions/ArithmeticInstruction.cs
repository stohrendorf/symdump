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

        public readonly Operation _operation;

        public ArithmeticInstruction(Operation operation, IOperand dest, IOperand lhs, IOperand rhs)
        {
            _operation = operation;
            Operands = new[] {dest, lhs, rhs};
        }

        public override IOperand[] Operands { get; }

        private IOperand Destination => Operands[0];
        private IOperand Lhs => Operands[1];
        private IOperand Rhs => Operands[2];

        public bool IsInplace => Destination.Equals(Lhs);

        public override string AsReadable()
        {
            string op;
            switch (_operation)
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

            return IsInplace
                ? $"{Destination} {op}= {Rhs}"
                : $"{Destination} = {Lhs} {op} {Rhs}";
        }
    }
}
