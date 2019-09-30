using System;
using symdump.exefile.operands;

namespace symdump.exefile.instructions
{
    public class ArithmeticInstruction : Instruction
    {
        public enum MathOperation
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

        private readonly bool _unchecked;

        public readonly MathOperation Operation;

        public ArithmeticInstruction(MathOperation operation, IOperand dest, IOperand lhs, IOperand rhs,
            bool @unchecked)
        {
            Operation = operation;
            Operands = new[] {dest, lhs, rhs};
            _unchecked = @unchecked;
        }

        public override IOperand[] Operands { get; }

        private IOperand Destination => Operands[0];
        private IOperand Lhs => Operands[1];
        private IOperand Rhs => Operands[2];

        public bool IsInplace => Destination.Equals(Lhs);

        public override string AsReadable()
        {
            string op;
            switch (Operation)
            {
                case MathOperation.Add:
                    op = "+";
                    break;
                case MathOperation.Sub:
                    op = "-";
                    break;
                case MathOperation.Mul:
                    op = "*";
                    break;
                case MathOperation.Div:
                    op = "/";
                    break;
                case MathOperation.Shl:
                    op = "<<";
                    break;
                case MathOperation.Shr:
                    op = ">>>";
                    break;
                case MathOperation.Sar:
                    op = ">>";
                    break;
                case MathOperation.BitAnd:
                    op = "&";
                    break;
                case MathOperation.BitOr:
                    op = "|";
                    break;
                case MathOperation.BitXor:
                    op = "^";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var result = IsInplace
                ? $"{Destination} {op}= {Rhs}"
                : $"{Destination} = {Lhs} {op} {Rhs}";

            if (!_unchecked)
                return result + " // exception on overflow";
            return result;
        }
    }
}
