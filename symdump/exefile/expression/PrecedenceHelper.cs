using System;
using symdump.exefile.instructions;

namespace symdump.exefile.expression
{
    public static class PrecedenceHelper
    {
        public static int getPrecedence(this Operation operation)
        {
            // from http://en.cppreference.com/w/c/language/operator_precedence
            switch (operation)
            {
                case Operation.Mul:
                case Operation.Div:
                    return 3;
                case Operation.Add:
                case Operation.Sub:
                    return 4;
                case Operation.Shl:
                case Operation.Shr:
                case Operation.Sar:
                    return 5;
                case Operation.Less:
                case Operation.SignedLess:
                case Operation.Greater:
                case Operation.SignedGreater:
                case Operation.LessEqual:
                case Operation.SignedLessEqual:
                case Operation.GreaterEqual:
                case Operation.SignedGreaterEqual:
                    return 6;
                case Operation.Equal:
                case Operation.NotEqual:
                    return 7;
                case Operation.BitAnd:
                    return 8;
                case Operation.BitXor:
                    return 9;
                case Operation.BitOr:
                    return 10;
                default:
                    throw new ArgumentOutOfRangeException(nameof(operation), operation, null);
            }
        }
    }
}