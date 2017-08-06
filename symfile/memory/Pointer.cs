using System;
using core;
using core.util;

namespace symfile.memory
{
    public class Pointer : IMemoryLayout, IEquatable<Pointer>
    {
        public int Precedence => Operator.Dereference.GetPrecedence(false);

        public string FundamentalType => Inner.FundamentalType;

        public uint DataSize => 4; // TODO assumes 32 bit architecture

        public IMemoryLayout Inner { get; }

        public IMemoryLayout Pointee => Inner;

        public Pointer(IMemoryLayout inner)
        {
            Inner = inner;
        }
        
        public string AsIncompleteDeclaration(string identifier, string argList)
        {
            var innerCode = Inner.AsIncompleteDeclaration(identifier, argList);
            return Inner.Precedence >= Precedence
                ? $"*({innerCode})"
                : $"*{innerCode}";
        }

        public bool Equals(Pointer other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Inner, other.Inner);
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
            return (Inner != null ? Inner.GetHashCode() : 0);
        }

        public string GetAccessPathTo(uint offset)
        {
            if(offset != 0)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "Can only access pointers at offset 0");

            return null;
        }
    }
}
