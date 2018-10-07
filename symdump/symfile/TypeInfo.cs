using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using symdump.symfile.util;

namespace symdump.symfile
{
    public class TypeInfo : IEquatable<TypeInfo>
    {
        public readonly ClassType ClassType;
        public readonly uint[] Dims;
        public readonly uint Size;
        public readonly string Tag;
        public readonly TypeDef TypeDef;

        public TypeInfo(BinaryReader reader, bool extended)
        {
            ClassType = reader.ReadClassType();
            TypeDef = reader.ReadTypeDef();
            Size = reader.ReadUInt32();

            if (extended)
            {
                var n = reader.ReadUInt16();
                Dims = new uint[n];
                for (var i = 0; i < n; ++i)
                    Dims[i] = reader.ReadUInt32();

                Tag = reader.ReadPascalString();
            }
            else
            {
                Dims = new uint[0];
                Tag = null;
            }
        }

        private bool IsFake => Tag != null && new Regex(@"^\.\d+fake$").IsMatch(Tag);

        public bool Equals(TypeInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            if (IsFake)
                return ClassType == other.ClassType && TypeDef.Equals(other.TypeDef) && Size == other.Size &&
                       Dims.SequenceEqual(other.Dims) && other.IsFake;
            return ClassType == other.ClassType && TypeDef.Equals(other.TypeDef) && Size == other.Size &&
                   Dims.SequenceEqual(other.Dims) && string.Equals(Tag, other.Tag);
        }

        public override string ToString()
        {
            return $"classType={ClassType} typeDef={TypeDef} size={Size}, dims=[{string.Join(",", Dims)}]";
        }

        public string AsCode(string name)
        {
            return TypeDef.AsCode(name, this);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((TypeInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) ClassType;
                hashCode = (hashCode * 397) ^ TypeDef.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) Size;
                hashCode = (hashCode * 397) ^ Dims.GetHashCode();
                hashCode = (hashCode * 397) ^ (!IsFake ? Tag.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
