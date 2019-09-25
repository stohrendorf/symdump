using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NLog;

namespace symdump.symfile
{
    public class DerivedTypeDef : IEquatable<DerivedTypeDef>
    {
        public const int MaxDerivedTypes = 6;
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();
        public readonly PrimitiveType Type;
        private IReadOnlyList<DerivedType> _derivedTypes;

        public DerivedTypeDef(BinaryReader fs)
        {
            var val = fs.ReadUInt16();
            Type = (PrimitiveType) (val & 0x0f);
            var types = new List<DerivedType>();
            for (var i = 0; i < MaxDerivedTypes; ++i)
            {
                var derivedType = (DerivedType) ((val >> (i * 2 + 4)) & 3);
                if (derivedType != DerivedType.None)
                    types.Add(derivedType);
            }

            _derivedTypes = types;
        }

        public bool IsFunctionReturnType => _derivedTypes.Contains(DerivedType.FunctionReturnType);

        public bool Equals(DerivedTypeDef other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Type == other.Type && _derivedTypes.SequenceEqual(other._derivedTypes);
        }

        public bool IsPartOf(DerivedTypeDef other, int dropLast, out int droppedExtents)
        {
            droppedExtents = 0;
            if (ReferenceEquals(null, other))
                return false;

            if (dropLast > other._derivedTypes.Count)
                return false;

            droppedExtents = other._derivedTypes.TakeLast(dropLast).Count(_ => _ == DerivedType.Array);
            return Type == other.Type && _derivedTypes.SequenceEqual(other._derivedTypes.SkipLast(dropLast));
        }

        public override string ToString()
        {
            var modifiers = string.Join(",", _derivedTypes);
            return modifiers.Length == 0 ? Type.ToString() : $"{Type}({modifiers})";
        }

        public string AsCode(string name, TaggedSymbol taggedSymbol, bool onlyDecorated = false)
        {
            var dimIdx = 0;

            string cType;
            if (taggedSymbol.IsResolvedTypedef)
                cType = taggedSymbol.InnerCode ?? taggedSymbol.Tag;
            else
                switch (Type)
                {
                    case PrimitiveType.StructDef:
                        Debug.Assert(!string.IsNullOrEmpty(taggedSymbol.Tag));
                        cType = taggedSymbol.InnerCode ?? (taggedSymbol.IsResolvedTypedef
                                    ? taggedSymbol.Tag
                                    : $"struct {taggedSymbol.Tag}");
                        break;
                    case PrimitiveType.UnionDef:
                        Debug.Assert(!string.IsNullOrEmpty(taggedSymbol.Tag));
                        cType = taggedSymbol.InnerCode ?? (taggedSymbol.IsResolvedTypedef
                                    ? taggedSymbol.Tag
                                    : $"union {taggedSymbol.Tag}");
                        break;
                    case PrimitiveType.EnumDef:
                        Debug.Assert(!string.IsNullOrEmpty(taggedSymbol.Tag));
                        cType = taggedSymbol.InnerCode ?? (taggedSymbol.IsResolvedTypedef
                                    ? taggedSymbol.Tag
                                    : $"enum {taggedSymbol.Tag}");
                        break;
                    case PrimitiveType.Char:
                        cType = "char";
                        break;
                    case PrimitiveType.Short:
                        cType = "short";
                        break;
                    case PrimitiveType.Int:
                        cType = "int";
                        break;
                    case PrimitiveType.Long:
                        cType = "long";
                        break;
                    case PrimitiveType.Float:
                        cType = "float";
                        break;
                    case PrimitiveType.Double:
                        cType = "double";
                        break;
                    case PrimitiveType.UChar:
                        cType = "unsigned char";
                        break;
                    case PrimitiveType.UShort:
                        cType = "unsigned short";
                        break;
                    case PrimitiveType.UInt:
                        cType = "unsigned int";
                        break;
                    case PrimitiveType.ULong:
                        cType = "unsigned long";
                        break;
                    case PrimitiveType.Void:
                        cType = "void";
                        break;
                    case PrimitiveType.Null:
                        cType = "__NULL__";
                        logger.Warn($"Found Null primitive type for symbol {name}");
                        break;
                    default:
                        throw new Exception($"Unexpected base type {Type}");
                }

            var prevPrecedence = int.MaxValue;
            var decorated = name;
            foreach (var derivedType in _derivedTypes)
                switch (derivedType)
                {
                    case DerivedType.Array:
                        Debug.Assert(decorated != null);

                        if (prevPrecedence < 2)
                            decorated = $"({decorated})";

                        decorated += $"[{taggedSymbol.Extents[dimIdx]}]";
                        ++dimIdx;
                        prevPrecedence = 2;
                        break;
                    case DerivedType.FunctionReturnType:
                        if (prevPrecedence < 2)
                            decorated = $"({decorated})";

                        decorated += "()";
                        prevPrecedence = 2;

                        break;
                    case DerivedType.Pointer:
                        if (prevPrecedence < 2)
                            decorated = $"({decorated})";

                        decorated = $"*{decorated}";
                        prevPrecedence = 3;
                        break;
                    default:
                        throw new Exception($"Unexpected derived type {derivedType}");
                }

            return onlyDecorated ? decorated : $"{cType} {decorated}";
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((DerivedTypeDef) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) Type * 397) ^ (_derivedTypes?.GetHashCode() ?? 0);
            }
        }

        public int RetainDerived(int n)
        {
            if (n > _derivedTypes.Count)
                throw new ArgumentOutOfRangeException(nameof(n));

            var droppedArrays = _derivedTypes.SkipLast(n).Count(_ => _ == DerivedType.Array);
            _derivedTypes = _derivedTypes.TakeLast(n).ToList();
            return droppedArrays;
        }
    }
}
