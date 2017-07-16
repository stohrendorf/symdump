using System;
using symdump.exefile.expression;
using symdump.exefile.instructions;

namespace symdump.symfile.type
{
    public class Array : ITypeDecorator, IEquatable<Array>
    {
        public int precedence => Operator.Array.getPrecedence(false);

        public ITypeDecorator inner { get; }

        public string asDeclaration(string identifier, string argList)
        {
            var innerCode = inner.asDeclaration(identifier, argList);
            return inner.precedence >= precedence
                ? $"({innerCode})[{dimension}]"
                : $"{innerCode}[{dimension}]";
        }

        public readonly uint dimension;

        public Array(uint dimension, ITypeDecorator inner)
        {
            this.dimension = dimension;
            this.inner = inner;
        }

        public bool Equals(Array other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return dimension == other.dimension && Equals(inner, other.inner);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Array) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) dimension * 397) ^ (inner != null ? inner.GetHashCode() : 0);
            }
        }
    }
}
