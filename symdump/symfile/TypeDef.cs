using System;
using System.Diagnostics;
using System.IO;
using symdump.symfile.type;

namespace symdump.symfile
{
    public class TypeDef : IEquatable<TypeDef>
    {
        public readonly BaseType baseType;

        public IWrappedType wrappedType { get; private set; }

        public bool isFunctionReturnType { get; private set; }

        private readonly DerivedType[] m_derivedTypes = new DerivedType[6];

        public TypeDef(BinaryReader reader)
        {
            var val = reader.ReadUInt16();
            baseType = (BaseType) (val & 0x0f);

            for (var i = 0; i < 6; ++i)
            {
                var x = (val >> (i * 2 + 4)) & 3;
                m_derivedTypes[i] = (DerivedType) x;
            }
        }

        public void applyTypeInfo(TypeInfo typeInfo)
        {
            IWrappedType wrapped = new NameWrapped();
            var dimIdx = 0;

            foreach (var dt in m_derivedTypes)
            {
                switch (dt)
                {
                    case DerivedType.None:
                        continue;
                    case DerivedType.Array:
                        wrapped = new ArrayWrapped(typeInfo.dims[dimIdx], wrapped);
                        ++dimIdx;
                        break;
                    case DerivedType.FunctionReturnType:
                        wrapped = new FunctionWrapped(wrapped);
                        isFunctionReturnType = true;
                        break;
                    case DerivedType.Pointer:
                        wrapped = new PointerWrapped(wrapped);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            wrappedType = wrapped;
        }

        public override string ToString()
        {
            return wrappedType.asCode("__NAME__", null);
        }

        public bool isStruct => baseType == BaseType.StructDef;

        public string asCode(string name, TypeInfo typeInfo, string argList)
        {
            string ctype;
            switch (baseType)
            {
                case BaseType.StructDef:
                    Debug.Assert(!string.IsNullOrEmpty(typeInfo.tag));
                    ctype = $"struct {typeInfo.tag}";
                    break;
                case BaseType.UnionDef:
                    Debug.Assert(!string.IsNullOrEmpty(typeInfo.tag));
                    ctype = $"union {typeInfo.tag}";
                    break;
                case BaseType.EnumDef:
                    Debug.Assert(!string.IsNullOrEmpty(typeInfo.tag));
                    ctype = $"enum {typeInfo.tag}";
                    break;
                case BaseType.Char:
                    ctype = "char";
                    break;
                case BaseType.Short:
                    ctype = "short";
                    break;
                case BaseType.Int:
                    ctype = "int";
                    break;
                case BaseType.Long:
                    ctype = "long";
                    break;
                case BaseType.Float:
                    ctype = "float";
                    break;
                case BaseType.Double:
                    ctype = "double";
                    break;
                case BaseType.UChar:
                    ctype = "unsigned char";
                    break;
                case BaseType.UShort:
                    ctype = "unsigned short";
                    break;
                case BaseType.UInt:
                    ctype = "unsigned int";
                    break;
                case BaseType.ULong:
                    ctype = "unsigned long";
                    break;
                case BaseType.Void:
                    ctype = "void";
                    break;
                default:
                    throw new Exception($"Unexpected base type {baseType}");
            }

            return ctype + " " + wrappedType.asCode(string.IsNullOrEmpty(name) ? "__NAME__" : name, argList);
        }

        public bool Equals(TypeDef other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return baseType == other.baseType && Equals(wrappedType, other.wrappedType) &&
                   isFunctionReturnType == other.isFunctionReturnType;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TypeDef) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) baseType;
                hashCode = (hashCode * 397) ^ (wrappedType != null ? wrappedType.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ isFunctionReturnType.GetHashCode();
                return hashCode;
            }
        }
    }
}
