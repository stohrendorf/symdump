using System;
using core;
using core.util;

namespace symfile.type
{
    public class Function : ITypeDecorator, IEquatable<Function>
    {
        public int precedence => Operator.FunctionCall.getPrecedence(false);

        public ITypeDecorator inner { get; }

        public Function(ITypeDecorator inner)
        {
            this.inner = inner;
        }

        public string asDeclaration(string identifier, string argList)
        {
            var innerCode = inner.asDeclaration(identifier, argList);

            if (argList == null)
                argList = "";
            return inner.precedence >= precedence
                ? $"({innerCode})({argList})"
                : $"{innerCode}({argList})";
        }

        public bool Equals(Function other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(inner, other.inner);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Function) obj);
        }

        public override int GetHashCode()
        {
            return (inner != null ? inner.GetHashCode() : 0);
        }
    }
}
