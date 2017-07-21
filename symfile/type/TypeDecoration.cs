using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using core;
using symfile.memory;
using symfile.util;

namespace symfile.type
{
    public class TypeDecoration : IEquatable<TypeDecoration>
    {
        public readonly ClassType classType;
        public readonly uint[] dimensions;
        public readonly uint size;
        public readonly string tag;

        public readonly BaseType baseType;

        public IMemoryLayout memoryLayout { get; }

        private readonly DerivedType[] m_derivedTypes = new DerivedType[6];

        public bool isFake => tag != null && new Regex(@"^\.\d+fake$").IsMatch(tag);

        public bool isFunctionReturnType { get; }

        public TypeDecoration(BinaryReader reader, bool extended, IDebugSource debugSource)
        {
            classType = reader.readClassType();

            var val = reader.ReadUInt16();
            baseType = (BaseType) (val & 0x0f);

            for (var i = 0; i < 6; ++i)
            {
                var x = (val >> (i * 2 + 4)) & 3;
                m_derivedTypes[i] = (DerivedType) x;
            }

            size = reader.ReadUInt32();

            if (extended)
            {
                var n = reader.ReadUInt16();
                dimensions = new uint[n];
                for (var i = 0; i < n; ++i)
                    dimensions[i] = reader.ReadUInt32();

                tag = reader.readPascalString();
            }
            else
            {
                dimensions = new uint[0];
                tag = null;
            }

            switch (classType)
            {
                case ClassType.Null:
                case ClassType.Label:
                case ClassType.UndefinedLabel:
                case ClassType.Struct:
                case ClassType.Union:
                case ClassType.Enum:
                case ClassType.LastEntry:
                case ClassType.EndOfStruct:
                case ClassType.FileName:
                    return;
                case ClassType.AutoVar:
                case ClassType.External:
                case ClassType.Static:
                case ClassType.Register:
                case ClassType.ExternalDefinition:
                case ClassType.StructMember:
                case ClassType.Argument:
                case ClassType.UnionMember:
                case ClassType.UndefinedStatic:
                case ClassType.Typedef:
                case ClassType.EnumMember:
                case ClassType.RegParam:
                case ClassType.Bitfield:
                case ClassType.AutoArgument:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(classType));
            }

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
                    Debug.Assert(debugSource.findTypeDefinition(tag) == null);
                    break;
                case BaseType.StructDef:
                case BaseType.UnionDef:
                case BaseType.EnumDef:
                case BaseType.EnumMember:
                    if (debugSource.findTypeDefinition(tag) == null)
                        return;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(baseType));
            }

            var memLayout = debugSource.findTypeDefinition(tag);
            try
            {
                memoryLayout = new PrimitiveType(baseType);

                if (memLayout != null)
                    throw new Exception("Primitive types must not have a memory layout");
            }
            catch (ArgumentOutOfRangeException)
            {
                if (memLayout == null)
                    throw new Exception("Non-primitive types must have a memory layout");

                memoryLayout = memLayout;
            }

            var dimIdx = 0;

            foreach (var derivedType in m_derivedTypes.Where(dt => dt != DerivedType.None))
            {
                switch (derivedType)
                {
                    case DerivedType.Array:
                        memoryLayout = new memory.Array(dimensions[dimIdx], memoryLayout);
                        ++dimIdx;
                        break;
                    case DerivedType.FunctionReturnType:
                        memoryLayout = new Function(memoryLayout);
                        isFunctionReturnType = true;
                        break;
                    case DerivedType.Pointer:
                        memoryLayout = new Pointer(memoryLayout);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(derivedType));
                }
            }
        }

        public override string ToString()
        {
            return
                $"classType={classType} annotated={asDeclaration(null)} size={size}, dims=[{string.Join(",", dimensions)}]";
        }

        public string asDeclaration(string name, string argList = null)
        {
            if (memoryLayout == null)
            {
                // FIXME can happen if a struct uses itself, e.g.:
                // struct Foo { struct Foo* next }
                return name;
            }

            return memoryLayout.fundamentalType + " " +
                   memoryLayout.asIncompleteDeclaration(string.IsNullOrEmpty(name) ? "__NAME__" : name, argList);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((TypeDecoration) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) classType;
                hashCode = (hashCode * 397) ^ (dimensions != null ? dimensions.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int) size;
                hashCode = (hashCode * 397) ^ (tag != null && !isFake ? tag.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int) baseType;
                hashCode = (hashCode * 397) ^ (m_derivedTypes != null ? m_derivedTypes.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (memoryLayout != null ? memoryLayout.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ isFunctionReturnType.GetHashCode();
                return hashCode;
            }
        }

        public bool Equals(TypeDecoration other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            if (isFake != other.isFake)
                return false;

            if (!isFake && !string.Equals(tag, other.tag))
                return false;
            
            return classType == other.classType && dimensions.SequenceEqual(other.dimensions) && size == other.size &&
                   baseType == other.baseType &&
                   m_derivedTypes.SequenceEqual(other.m_derivedTypes) && Equals(memoryLayout, other.memoryLayout) &&
                   isFunctionReturnType == other.isFunctionReturnType;
        }
    }
}
