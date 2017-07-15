using System;
using symdump.exefile.expression;
using symdump.exefile.instructions;

namespace symdump.symfile.type
{
    public class FunctionWrapped : IWrappedType, IEquatable<FunctionWrapped>
    {
        public int precedence => Operator.FunctionCall.getPrecedence(false);

        public IWrappedType inner { get; }

        public FunctionWrapped(IWrappedType inner)
        {
            this.inner = inner;
        }

        public string asCode(string name, string argList)
        {
            var innerCode = inner.asCode(name, argList);

            if (argList == null)
                argList = "";
            return inner.precedence >= precedence
                ? $"({innerCode})({argList})"
                : $"{innerCode}({argList})";
        }

        public bool Equals(FunctionWrapped other)
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
            return Equals((FunctionWrapped) obj);
        }

        public override int GetHashCode()
        {
            return (inner != null ? inner.GetHashCode() : 0);
        }
    }
}
