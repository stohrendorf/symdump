using System;

namespace symdump.exefile.instructions
{
    public static class OperationUtil
    {
        public static string toCode(this Operation operation)
        {
            switch (operation)
            {
                case Operation.Add:
                    return "+";
                case Operation.Sub:
                    return "-";
                case Operation.Mul:
                    return "*";
                case Operation.Div:
                    return "/";
                case Operation.Shl:
                    return "<<";
                case Operation.Shr:
                    return ">>>";
                case Operation.Sar:
                    return ">>";
                case Operation.BitAnd:
                    return "&";
                case Operation.BitOr:
                    return "|";
                case Operation.BitXor:
                    return "^";
                case Operation.Less:
                    return "<";
                case Operation.SignedLess:
                    return "<";
                case Operation.LessEqual:
                    return "<=";
                case Operation.SignedLessEqual:
                    return "<=";
                case Operation.Equal:
                    return "==";
                case Operation.NotEqual:
                    return "!=";
                case Operation.Greater:
                    return ">";
                case Operation.SignedGreater:
                    return ">";
                case Operation.GreaterEqual:
                    return ">=";
                case Operation.SignedGreaterEqual:
                    return ">=";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

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
        BitXor,
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
}