using System;
using core;
using core.util;

namespace symfile.memory
{
    public class Function : IMemoryLayout, IEquatable<Function>
    {
        public int Precedence => 1;

        public string FundamentalType => _inner.FundamentalType;

        public uint DataSize => 4; // TODO assumes 32 bit architecture

        private readonly IMemoryLayout _inner;

        public IMemoryLayout Pointee => null;

        public Function(IMemoryLayout inner)
        {
            _inner = inner;
        }

        public string AsIncompleteDeclaration(string identifier, string argList)
        {
            var innerCode = _inner.AsIncompleteDeclaration(identifier, argList);

            return _inner.Precedence >= Precedence
                ? $"({innerCode})({argList})"
                : $"{innerCode}({argList})";
        }

        public bool Equals(Function other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(_inner, other._inner);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Function) obj);
        }

        public override int GetHashCode()
        {
            return (_inner != null ? _inner.GetHashCode() : 0);
        }

        public string GetAccessPathTo(uint offset)
        {
            // TODO: throw new NotImplementedException();
            return $"<FUNC@{offset}>";
        }
    }
}
