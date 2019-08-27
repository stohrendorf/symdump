using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using symdump.symfile;

namespace symdump
{
    public class DerivedTypeDef : IEquatable<DerivedTypeDef>
    {
        private readonly DerivedType[] _derivedTypes = new DerivedType[6];
        public readonly PrimitiveType Type;

        public DerivedTypeDef(BinaryReader fs)
        {
            var val = fs.ReadUInt16();
            Type = (PrimitiveType) (val & 0x0f);
            for (var i = 0; i < 6; ++i)
            {
                var x = (val >> (i * 2 + 4)) & 3;
                _derivedTypes[i] = (DerivedType) x;
            }
        }

        public bool IsFunctionReturnType => _derivedTypes.Contains(DerivedType.FunctionReturnType);

        public bool Equals(DerivedTypeDef other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Type == other.Type && _derivedTypes.SequenceEqual(other._derivedTypes);
        }

        public override string ToString()
        {
            var attributes = string.Join(",", _derivedTypes.Where(e => e != DerivedType.None));
            return attributes.Length == 0 ? Type.ToString() : $"{Type}({attributes})";
        }

        public string AsCode(string name, TaggedSymbol taggedSymbol)
        {
            var dimIdx = 0;

            string cType;
            switch (Type)
            {
                case PrimitiveType.StructDef:
                    Debug.Assert(!string.IsNullOrEmpty(taggedSymbol.Tag));
                    cType = $"struct {taggedSymbol.Tag}";
                    break;
                case PrimitiveType.UnionDef:
                    Debug.Assert(!string.IsNullOrEmpty(taggedSymbol.Tag));
                    cType = $"union {taggedSymbol.Tag}";
                    break;
                case PrimitiveType.EnumDef:
                    Debug.Assert(!string.IsNullOrEmpty(taggedSymbol.Tag));
                    cType = $"enum {taggedSymbol.Tag}";
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
                default:
                    throw new Exception($"Unexpected base type {Type}");
            }

            var needsParens = false;
            foreach (var dt in _derivedTypes)
                switch (dt)
                {
                    case DerivedType.None:
                        continue;
                    case DerivedType.Array:
                        Debug.Assert(name != null);
                        name += $"[{taggedSymbol.Extents[dimIdx]}]";
                        ++dimIdx;
                        needsParens = true;
                        break;
                    case DerivedType.FunctionReturnType:
                        if (name != "")
                        {
                            name = needsParens ? $"({name})()" : $"{name}()";
                            needsParens = true;
                        }

                        break;
                    case DerivedType.Pointer:
                        name = $"*{name}";
                        needsParens = true;
                        break;
                }

            return $"{cType} {name}";
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
                return ((int) Type * 397) ^ (_derivedTypes != null ? _derivedTypes.GetHashCode() : 0);
            }
        }
    }
}
