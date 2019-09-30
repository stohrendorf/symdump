using System;
using symdump.exefile.operands;

namespace symdump.exefile.instructions
{
    public class ConditionalBranchInstruction : Instruction
    {
        public enum BoolOperation
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

        public readonly BoolOperation Operation;

        public ConditionalBranchInstruction(BoolOperation boolOperation, IOperand lhs, IOperand rhs, IOperand target,
            bool likely = false)
        {
            Operation = boolOperation;
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
            switch (Operation)
            {
                case BoolOperation.Less:
                    op = "<";
                    break;
                case BoolOperation.SignedLess:
                    op = "<";
                    break;
                case BoolOperation.LessEqual:
                    op = "<=";
                    break;
                case BoolOperation.SignedLessEqual:
                    op = "<=";
                    break;
                case BoolOperation.Equal:
                    op = "==";
                    break;
                case BoolOperation.NotEqual:
                    op = "!=";
                    break;
                case BoolOperation.Greater:
                    op = ">";
                    break;
                case BoolOperation.SignedGreater:
                    op = ">";
                    break;
                case BoolOperation.GreaterEqual:
                    op = ">=";
                    break;
                case BoolOperation.SignedGreaterEqual:
                    op = ">=";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return $"if{(_likely ? " likely" : "")}({Lhs} {op} {Rhs}) goto {Target}";
        }
    }
}
