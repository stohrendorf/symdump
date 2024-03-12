using System;
using System.IO;
using symdump.symfile.util;

namespace symdump.symfile
{
    /// <summary>
    ///     Member field of a struct or union.
    /// </summary>
    public class CompoundMember : IEquatable<CompoundMember>
    {
        private readonly string? _name;
        public readonly TaggedSymbol MemberType;
        private readonly TypedValue _typedValue;

        public CompoundMember(TypedValue typedValue, BinaryReader reader, bool isArray)
        {
            MemberType = reader.ReadTaggedSymbol(isArray);
            _name = reader.ReadPascalString();
            _typedValue = typedValue;

            if (MemberType.Type != SymbolType.Bitfield
                && MemberType.Type != SymbolType.StructMember
                && MemberType.Type != SymbolType.UnionMember
                && MemberType.Type != SymbolType.EndOfStruct)
                throw new Exception($"Unexpected {nameof(SymbolType)} {MemberType.Type}");
        }

        public bool Equals(CompoundMember? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(_name, other._name)
                   && MemberType.Equals(other.MemberType)
                   && _typedValue.Equals(other._typedValue);
        }

        public override string ToString()
        {
            switch (MemberType.Type)
            {
                case SymbolType.Bitfield:
                    return MemberType.AsCode(_name) +
                           $" : {MemberType.Size}; // offset={_typedValue.Value / 8}.{_typedValue.Value % 8}";
                case SymbolType.StructMember:
                case SymbolType.UnionMember:
                    return MemberType.AsCode(_name) +
                           $"; // size={MemberType.Size}, offset={_typedValue.Value}";
                default:
                    throw new Exception($"Unexpected {nameof(SymbolType)} {MemberType.Type}");
            }
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((CompoundMember) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = _name != null ? _name.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ MemberType.GetHashCode();
                hashCode = (hashCode * 397) ^ _typedValue.GetHashCode();
                return hashCode;
            }
        }

        public void ResolveTypedef(ObjectFile objectFile)
        {
            MemberType.ResolveTypedef(objectFile);
        }
    }
}
