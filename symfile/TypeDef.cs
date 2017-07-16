using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using symfile.type;
using Array = symfile.type.Array;

namespace symfile
{
    public class TypeDef : IEquatable<TypeDef>
    {
        public readonly BaseType baseType;

        public ITypeDecorator typeDecorator { get; private set; }

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

        public void applyDecoration(uint[] arrayDims)
        {
            typeDecorator = new Identifier();
            var dimIdx = 0;

            foreach (var dt in m_derivedTypes.Where(dt => dt != DerivedType.None))
            {
                switch (dt)
                {
                    case DerivedType.Array:
                        typeDecorator = new Array(arrayDims[dimIdx], typeDecorator);
                        ++dimIdx;
                        break;
                    case DerivedType.FunctionReturnType:
                        typeDecorator = new type.Function(typeDecorator);
                        isFunctionReturnType = true;
                        break;
                    case DerivedType.Pointer:
                        typeDecorator = new Pointer(typeDecorator);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override string ToString()
        {
            return typeDecorator.asDeclaration("__NAME__", null);
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

            return ctype + " " + typeDecorator.asDeclaration(string.IsNullOrEmpty(name) ? "__NAME__" : name, argList);
        }

        public bool Equals(TypeDef other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return baseType == other.baseType && Equals(typeDecorator, other.typeDecorator) &&
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
                hashCode = (hashCode * 397) ^ (typeDecorator != null ? typeDecorator.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ isFunctionReturnType.GetHashCode();
                return hashCode;
            }
        }
    }
}
