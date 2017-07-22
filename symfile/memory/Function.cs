using System;
using core;
using core.util;

namespace symfile.memory
{
    public class Function : IMemoryLayout, IEquatable<Function>
    {
        public int precedence => Operator.FunctionCall.getPrecedence(false);

        public string fundamentalType => m_inner.fundamentalType;

        public uint dataSize => 4; // TODO assumes 32 bit architecture

        private readonly IMemoryLayout m_inner;

        public IMemoryLayout pointee => null;

        public Function(IMemoryLayout inner)
        {
            m_inner = inner;
        }

        public string asIncompleteDeclaration(string identifier, string argList)
        {
            var innerCode = m_inner.asIncompleteDeclaration(identifier, argList);

            return m_inner.precedence >= precedence
                ? $"({innerCode})({argList})"
                : $"{innerCode}({argList})";
        }

        public bool Equals(Function other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(m_inner, other.m_inner);
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
            return (m_inner != null ? m_inner.GetHashCode() : 0);
        }

        public string getAccessPathTo(uint offset)
        {
            throw new NotImplementedException();
        }
    }
}
