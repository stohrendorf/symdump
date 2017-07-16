using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using symfile.util;

namespace symfile
{
    public class TypeInfo : IEquatable<TypeInfo>
    {
        public readonly ClassType classType;
        public readonly uint[] dims;
        public readonly uint size;
        public readonly string tag;
        public readonly TypeDef typeDef;

        public TypeInfo(BinaryReader reader, bool extended)
        {
            classType = reader.readClassType();
            typeDef = reader.readTypeDef();
            size = reader.ReadUInt32();

            if (extended)
            {
                var n = reader.ReadUInt16();
                dims = new uint[n];
                for (var i = 0; i < n; ++i)
                    dims[i] = reader.ReadUInt32();

                tag = reader.readPascalString();
            }
            else
            {
                dims = new uint[0];
                tag = null;
            }
            
            typeDef.applyDecoration(dims);
        }

        public bool isFake => tag != null && new Regex(@"^\.\d+fake$").IsMatch(tag);

        public bool Equals(TypeInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            if (isFake)
                return classType == other.classType && typeDef.Equals(other.typeDef) && size == other.size &&
                       dims.SequenceEqual(other.dims) && other.isFake;
            return classType == other.classType && typeDef.Equals(other.typeDef) && size == other.size &&
                   dims.SequenceEqual(other.dims) && string.Equals(tag, other.tag);
        }

        public override string ToString()
        {
            return $"classType={classType} typeDef={typeDef} size={size}, dims=[{string.Join(",", dims)}]";
        }

        public string asCode(string name, string argList = null)
        {
            return typeDef.asCode(name, this, argList);
        }

        public bool isStruct => typeDef.isStruct;
        
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
                var hashCode = (int) classType;
                hashCode = (hashCode * 397) ^ typeDef.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) size;
                hashCode = (hashCode * 397) ^ dims.GetHashCode();
                hashCode = (hashCode * 397) ^ (!isFake ? tag.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
