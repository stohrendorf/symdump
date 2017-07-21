using System;
using JetBrains.Annotations;

namespace core
{
    public static class OperatorUtil
    {
        [NotNull]
        public static string asCode(this Operator @operator)
        {
            switch (@operator)
            {
                case Operator.Add:
                    return "+";
                case Operator.Sub:
                    return "-";
                case Operator.Mul:
                    return "*";
                case Operator.Div:
                    return "/";
                case Operator.Shl:
                    return "<<";
                case Operator.Shr:
                    return ">>>";
                case Operator.Sar:
                    return ">>";
                case Operator.BitAnd:
                    return "&";
                case Operator.BitOr:
                    return "|";
                case Operator.BitXor:
                    return "^";
                case Operator.Less:
                    return "<";
                case Operator.SignedLess:
                    return "<";
                case Operator.LessEqual:
                    return "<=";
                case Operator.SignedLessEqual:
                    return "<=";
                case Operator.Equal:
                    return "==";
                case Operator.NotEqual:
                    return "!=";
                case Operator.Greater:
                    return ">";
                case Operator.SignedGreater:
                    return ">";
                case Operator.GreaterEqual:
                    return ">=";
                case Operator.SignedGreaterEqual:
                    return ">=";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public enum Operator
    {
        Add,
        Sub,
        Mul,
        Div,
        Mod,
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
        SignedGreaterEqual,
        FunctionCall,
        Array,
        MemberAccess,
        Cast,
        Dereference
    }
}