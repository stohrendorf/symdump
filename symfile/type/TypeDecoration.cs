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
        public readonly ClassType ClassType;
        public readonly uint[] Dimensions;
        public readonly uint Size;
        public readonly string Tag;

        public readonly BaseType BaseType;

        public IMemoryLayout MemoryLayout { get; }

        private readonly DerivedType[] _derivedTypes = new DerivedType[6];

        public bool IsFake => Tag != null && new Regex(@"^\.\d+fake$").IsMatch(Tag);

        public bool IsFunctionReturnType { get; }

        public TypeDecoration(BinaryReader reader, bool extended, IDebugSource debugSource)
        {
            ClassType = reader.ReadClassType();

            var val = reader.ReadUInt16();
            BaseType = (BaseType) (val & 0x0f);

            for (var i = 0; i < 6; ++i)
            {
                var x = (val >> (i * 2 + 4)) & 3;
                _derivedTypes[i] = (DerivedType) x;
            }

            Size = reader.ReadUInt32();

            if (extended)
            {
                var n = reader.ReadUInt16();
                Dimensions = new uint[n];
                for (var i = 0; i < n; ++i)
                    Dimensions[i] = reader.ReadUInt32();

                Tag = reader.ReadPascalString();
            }
            else
            {
                Dimensions = new uint[0];
                Tag = null;
            }

            switch (ClassType)
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
                    throw new ArgumentOutOfRangeException(nameof(ClassType));
            }

            switch (BaseType)
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
                    Debug.Assert(debugSource.FindTypeDefinition(Tag) == null);
                    break;
                case BaseType.StructDef:
                case BaseType.UnionDef:
                case BaseType.EnumDef:
                case BaseType.EnumMember:
                    if (debugSource.FindTypeDefinition(Tag) == null)
                        return;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(BaseType));
            }

            var memLayout = debugSource.FindTypeDefinition(Tag);
            try
            {
                MemoryLayout = new PrimitiveType(BaseType);

                if (memLayout != null)
                    throw new Exception("Primitive types must not have a memory layout");
            }
            catch (ArgumentOutOfRangeException)
            {
                MemoryLayout = memLayout ?? throw new Exception("Non-primitive types must have a memory layout");
            }

            var dimIdx = 0;

            foreach (var derivedType in _derivedTypes.Where(dt => dt != DerivedType.None))
            {
                switch (derivedType)
                {
                    case DerivedType.Array:
                        MemoryLayout = new memory.Array(Dimensions[dimIdx], MemoryLayout);
                        ++dimIdx;
                        break;
                    case DerivedType.FunctionReturnType:
                        MemoryLayout = new Function(MemoryLayout);
                        IsFunctionReturnType = true;
                        break;
                    case DerivedType.Pointer:
                        MemoryLayout = new Pointer(MemoryLayout);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(derivedType));
                }
            }
        }

        public override string ToString()
        {
            return
                $"classType={ClassType} annotated={AsDeclaration(null)} size={Size}, dims=[{string.Join(",", Dimensions)}]";
        }

        public string AsDeclaration(string name, string argList = null)
        {
            if (MemoryLayout == null)
            {
                // FIXME can happen if a struct uses itself, e.g.:
                // struct Foo { struct Foo* next }
                return name;
            }

            return MemoryLayout.FundamentalType + " " +
                   MemoryLayout.AsIncompleteDeclaration(string.IsNullOrEmpty(name) ? "__NAME__" : name, argList);
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
                var hashCode = (int) ClassType;
                hashCode = (hashCode * 397) ^ (Dimensions != null ? Dimensions.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int) Size;
                hashCode = (hashCode * 397) ^ (Tag != null && !IsFake ? Tag.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int) BaseType;
                hashCode = (hashCode * 397) ^ (_derivedTypes != null ? _derivedTypes.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (MemoryLayout != null ? MemoryLayout.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ IsFunctionReturnType.GetHashCode();
                return hashCode;
            }
        }

        public bool Equals(TypeDecoration other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            if (IsFake != other.IsFake)
                return false;

            if (!IsFake && !string.Equals(Tag, other.Tag))
                return false;
            
            return ClassType == other.ClassType && Dimensions.SequenceEqual(other.Dimensions) && Size == other.Size &&
                   BaseType == other.BaseType &&
                   _derivedTypes.SequenceEqual(other._derivedTypes) && Equals(MemoryLayout, other.MemoryLayout) &&
                   IsFunctionReturnType == other.IsFunctionReturnType;
        }
    }
}
