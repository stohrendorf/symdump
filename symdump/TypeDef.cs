using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using symdump.symfile;

namespace symdump
{
    public class TypeDef : IEquatable<TypeDef>
    {
        public readonly BaseType BaseType;
        private readonly DerivedType[] _derivedTypes = new DerivedType[6];

        public TypeDef(BinaryReader fs)
        {
            var val = fs.ReadUInt16();
            BaseType = (BaseType) (val & 0x0f);
            for (var i = 0; i < 6; ++i)
            {
                var x = (val >> (i * 2 + 4)) & 3;
                _derivedTypes[i] = (DerivedType) x;
            }
        }

        public bool IsFunctionReturnType => _derivedTypes.Contains(DerivedType.FunctionReturnType);

        public bool Equals(TypeDef other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return BaseType == other.BaseType && _derivedTypes.SequenceEqual(other._derivedTypes);
        }

        public override string ToString()
        {
            var attributes = string.Join(",", _derivedTypes.Where(e => e != DerivedType.None));
            return attributes.Length == 0 ? BaseType.ToString() : $"{BaseType}({attributes})";
        }

        public string AsCode(string name, TypeInfo typeInfo)
        {
            var dimIdx = 0;

            string cType;
            switch (BaseType)
            {
                case BaseType.StructDef:
                    Debug.Assert(!string.IsNullOrEmpty(typeInfo.Tag));
                    cType = $"struct {typeInfo.Tag}";
                    break;
                case BaseType.UnionDef:
                    Debug.Assert(!string.IsNullOrEmpty(typeInfo.Tag));
                    cType = $"union {typeInfo.Tag}";
                    break;
                case BaseType.EnumDef:
                    Debug.Assert(!string.IsNullOrEmpty(typeInfo.Tag));
                    cType = $"enum {typeInfo.Tag}";
                    break;
                case BaseType.Char:
                    cType = "char";
                    break;
                case BaseType.Short:
                    cType = "short";
                    break;
                case BaseType.Int:
                    cType = "int";
                    break;
                case BaseType.Long:
                    cType = "long";
                    break;
                case BaseType.Float:
                    cType = "float";
                    break;
                case BaseType.Double:
                    cType = "double";
                    break;
                case BaseType.UChar:
                    cType = "unsigned char";
                    break;
                case BaseType.UShort:
                    cType = "unsigned short";
                    break;
                case BaseType.UInt:
                    cType = "unsigned int";
                    break;
                case BaseType.ULong:
                    cType = "unsigned long";
                    break;
                case BaseType.Void:
                    cType = "void";
                    break;
                default:
                    throw new Exception($"Unexpected base type {BaseType}");
            }

            var needsParens = false;
            foreach (var dt in _derivedTypes)
                switch (dt)
                {
                    case DerivedType.None:
                        continue;
                    case DerivedType.Array:
                        Debug.Assert(name != null);
                        name += $"[{typeInfo.Dims[dimIdx]}]";
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
            return Equals((TypeDef) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) BaseType * 397) ^ (_derivedTypes != null ? _derivedTypes.GetHashCode() : 0);
            }
        }
    }
}
