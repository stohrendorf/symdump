using System;
using symdump.exefile.expression;
using symdump.exefile.instructions;

namespace symdump.symfile.type
{
    public class Pointer : ITypeDecorator, IEquatable<Pointer>
    {
        public int precedence => Operator.Dereference.getPrecedence(false);
        
        public ITypeDecorator inner { get; }

        public Pointer(ITypeDecorator inner)
        {
            this.inner = inner;
        }
        
        public string asDeclaration(string identifier, string argList)
        {
            var innerCode = inner.asDeclaration(identifier, argList);
            return inner.precedence >= precedence
                ? $"*({innerCode})"
                : $"*{innerCode}";
        }

        public bool Equals(Pointer other)
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
            return Equals((Pointer) obj);
        }

        public override int GetHashCode()
        {
            return (inner != null ? inner.GetHashCode() : 0);
        }
    }
}
