using System;
using core;
using core.util;

namespace symfile.type
{
    public class Function : IMemoryLayout, IEquatable<Function>
    {
        public int precedence => Operator.FunctionCall.getPrecedence(false);

        public string fundamentalType => inner.fundamentalType;

        public uint dataSize => 4; // TODO assumes 32 bit architecture

        public IMemoryLayout inner { get; }

        public Function(IMemoryLayout inner)
        {
            this.inner = inner;
        }

        public string asIncompleteDeclaration(string identifier, string argList)
        {
            var innerCode = inner.asIncompleteDeclaration(identifier, argList);

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

        public string getAccessPathTo(uint offset)
        {
            throw new NotImplementedException();
        }
    }
}
