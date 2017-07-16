using System;

namespace core.util
{
    public static class PrecedenceHelper
    {
        public static int getPrecedence(this Operator @operator, bool inplace)
        {
            // from http://en.cppreference.com/w/c/language/operator_precedence
            switch (@operator)
            {
                case Operator.FunctionCall:
                case Operator.Array:
                case Operator.MemberAccess:
                    return 1;
                case Operator.Cast:
                case Operator.Dereference:
                    return 2;
                case Operator.Mul:
                case Operator.Div:
                case Operator.Mod:
                    return inplace ? 14 : 3;
                case Operator.Add:
                case Operator.Sub:
                    return inplace ? 14 : 4;
                case Operator.Shl:
                case Operator.Shr:
                case Operator.Sar:
                    return inplace ? 14 : 5;
                case Operator.Less:
                case Operator.SignedLess:
                case Operator.Greater:
                case Operator.SignedGreater:
                case Operator.LessEqual:
                case Operator.SignedLessEqual:
                case Operator.GreaterEqual:
                case Operator.SignedGreaterEqual:
                    return 6;
                case Operator.Equal:
                case Operator.NotEqual:
                    return 7;
                case Operator.BitAnd:
                    return inplace ? 14 : 8;
                case Operator.BitXor:
                    return inplace ? 14 : 9;
                case Operator.BitOr:
                    return inplace ? 14 : 10;
                default:
                    throw new ArgumentOutOfRangeException(nameof(@operator), @operator, null);
            }
        }
    }
}
