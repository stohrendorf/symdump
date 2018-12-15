using System;
using core;

namespace symfile.memory
{
    public class Pointer : IMemoryLayout, IEquatable<Pointer>
    {
        private readonly IMemoryLayout _inner;

        public Pointer(IMemoryLayout inner)
        {
            _inner = inner;
        }

        public bool Equals(Pointer other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(_inner, other._inner);
        }

        public int Precedence => 2;

        public string FundamentalType => _inner.FundamentalType;

        public uint DataSize => 4; // TODO assumes 32 bit architecture

        public string AsIncompleteDeclaration(string identifier, string argList)
        {
            var innerCode = _inner.AsIncompleteDeclaration(identifier, argList);
            return _inner.Precedence >= Precedence
                ? $"*({innerCode})"
                : $"*{innerCode}";
        }

        public string GetAccessPathTo(uint offset)
        {
            if (offset != 0)
                throw new UnalignedAccessException(offset, "Can only access pointers at offset 0");

            return null;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Pointer) obj);
        }

        public override int GetHashCode()
        {
            return _inner != null ? _inner.GetHashCode() : 0;
        }
    }
}