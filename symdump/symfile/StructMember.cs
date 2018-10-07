using System;
using System.IO;
using symdump.symfile.util;

namespace symdump.symfile
{
    public class StructMember : IEquatable<StructMember>
    {
        private readonly string _name;
        public readonly TypedValue TypedValue;
        public readonly TypeInfo TypeInfo;

        public StructMember(TypedValue tv, BinaryReader reader, bool extended)
        {
            TypeInfo = reader.ReadTypeInfo(extended);
            _name = reader.ReadPascalString();
            TypedValue = tv;

            if (TypeInfo.ClassType != ClassType.Bitfield && TypeInfo.ClassType != ClassType.StructMember &&
                TypeInfo.ClassType != ClassType.UnionMember && TypeInfo.ClassType != ClassType.EndOfStruct)
                throw new Exception($"Unexpected class {TypeInfo.ClassType}");
        }

        public bool Equals(StructMember other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(_name, other._name) && TypeInfo.Equals(other.TypeInfo) &&
                   TypedValue.Equals(other.TypedValue);
        }

        public override string ToString()
        {
            switch (TypeInfo.ClassType)
            {
                case ClassType.Bitfield:
                    return TypeInfo.AsCode(_name) +
                           $" : {TypeInfo.Size}; // offset={TypedValue.Value / 8}.{TypedValue.Value % 8}";
                case ClassType.StructMember:
                case ClassType.UnionMember:
                    return TypeInfo.AsCode(_name) +
                           $"; // size={TypeInfo.Size}, offset={TypedValue.Value}";
                default:
                    throw new Exception($"Unexpected class {TypeInfo.ClassType}");
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
                hashCode = (hashCode * 397) ^ (TypeInfo != null ? TypeInfo.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (TypedValue != null ? TypedValue.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
