using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using core;
using symfile.type;
using Array = symfile.type.Array;

namespace symfile
{
    public class TypeDef : IEquatable<TypeDef>
    {
        public readonly BaseType baseType;

        public IMemoryLayout typeDecorator { get; private set; }

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

        public void applyDecoration(uint[] arrayDims, IMemoryLayout inner)
        {
            switch (baseType)
            {
                case BaseType.Null:
                case BaseType.Void:
                case BaseType.Char:
                case BaseType.Short:
                case BaseType.Int:
                case BaseType.Long:
                case BaseType.Float:
                case BaseType.Double:
                case BaseType.UChar:
                case BaseType.UShort:
                case BaseType.UInt:
                case BaseType.ULong:
                    Debug.Assert(inner == null);
                    break;
                case BaseType.StructDef:
                case BaseType.UnionDef:
                case BaseType.EnumDef:
                case BaseType.EnumMember:
                    if(inner == null)
                        return;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            try
            {
                typeDecorator = new PrimitiveType(baseType);
                
                if(inner != null)
                    throw new Exception("Primitive types must not have a memory layout");
            }
            catch (ArgumentOutOfRangeException)
            {
                if(inner == null)
                    throw new Exception("Non-primitive types must have a memory layout");

                typeDecorator = inner;
            }

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
            return typeDecorator.asDeclaration(string.IsNullOrEmpty(name) ? "__NAME__" : name, argList);
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
