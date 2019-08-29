using System;
using System.Collections.Generic;
using System.IO;
using symdump.symfile.util;

namespace symdump.symfile
{
    public class StructMember : IEquatable<StructMember>
    {
        private readonly string _name;
        public readonly TaggedSymbol MemberType;
        public readonly TypedValue TypedValue;

        public StructMember(TypedValue tv, BinaryReader reader, bool extended)
        {
            MemberType = reader.ReadTaggedSymbol(extended);
            _name = reader.ReadPascalString();
            TypedValue = tv;

            if (MemberType.Type != SymbolType.Bitfield
                && MemberType.Type != SymbolType.StructMember
                && MemberType.Type != SymbolType.UnionMember
                && MemberType.Type != SymbolType.EndOfStruct)
                throw new Exception($"Unexpected {nameof(SymbolType)} {MemberType.Type}");
        }

        public bool Equals(StructMember other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(_name, other._name)
                   && MemberType.Equals(other.MemberType)
                   && TypedValue.Equals(other.TypedValue);
        }

        public void ApplyInline(IDictionary<string, EnumDef> enums, IDictionary<string, StructDef> structs,
            IDictionary<string, UnionDef> unions)
        {
            MemberType.ApplyInline(enums, structs, unions);
        }

        public override string ToString()
        {
            switch (MemberType.Type)
            {
                case SymbolType.Bitfield:
                    return MemberType.AsCode(_name) +
                           $" : {MemberType.Size}; // offset={TypedValue.Value / 8}.{TypedValue.Value % 8}";
                case SymbolType.StructMember:
                case SymbolType.UnionMember:
                    return MemberType.AsCode(_name) +
                           $"; // size={MemberType.Size}, offset={TypedValue.Value}";
                default:
                    throw new Exception($"Unexpected {nameof(SymbolType)} {MemberType.Type}");
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((StructMember) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = _name != null ? _name.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (MemberType != null ? MemberType.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (TypedValue != null ? TypedValue.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
