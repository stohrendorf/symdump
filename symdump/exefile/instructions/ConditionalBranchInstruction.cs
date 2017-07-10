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

        public readonly Operation operation;

        public ConditionalBranchInstruction(Operation operation, IOperand lhs, IOperand rhs, IOperand target)
        {
            this.operation = operation;
            operands = new[] {lhs, rhs, target};
        }

        public IOperand lhs => operands[0];
        public IOperand rhs => operands[1];
        public IOperand target => operands[2];

        public override IOperand[] operands { get; }

        public override string asReadable()
        {
            string op;
            switch (operation)
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

            return $"if({lhs} {op} {rhs}) goto {target}";
        }
    }
}