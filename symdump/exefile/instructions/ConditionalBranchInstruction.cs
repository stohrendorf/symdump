using System;
using symdump.exefile.operands;

namespace symdump.exefile.instructions
{
    public class ConditionalBranchInstruction : Instruction
    {
        public enum Operation
        {
            Equal,
            NotEqual,
            Less,
            SignedLess,
            Greater,
            SignedGreater,
            LessEqual,
            SignedLessEqual,
            GreaterEqual,
            SignedGreaterEqual
        }

        private readonly bool _likely;

        private readonly Operation _operation;

        public ConditionalBranchInstruction(Operation operation, IOperand lhs, IOperand rhs, IOperand target,
            bool likely = false)
        {
            _operation = operation;
            Operands = new[] {lhs, rhs, target};
            _likely = likely;
        }

        private IOperand Lhs => Operands[0];
        private IOperand Rhs => Operands[1];
        private IOperand Target => Operands[2];

        public override IOperand[] Operands { get; }

        public override string AsReadable()
        {
            string op;
            switch (_operation)
            {
                case Operation.Less:
                    op = "<";
                    break;
                case Operation.SignedLess:
                    op = "<";
                    break;
                case Operation.LessEqual:
                    op = "<=";
                    break;
                case Operation.SignedLessEqual:
                    op = "<=";
                    break;
                case Operation.Equal:
                    op = "==";
                    break;
                case Operation.NotEqual:
                    op = "!=";
                    break;
                case Operation.Greater:
                    op = ">";
                    break;
                case Operation.SignedGreater:
                    op = ">";
                    break;
                case Operation.GreaterEqual:
                    op = ">=";
                    break;
                case Operation.SignedGreaterEqual:
                    op = ">=";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return $"if{(_likely ? " likely" : "")}({Lhs} {op} {Rhs}) goto {Target}";
        }
    }
}
