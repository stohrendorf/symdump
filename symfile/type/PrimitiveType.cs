using System;
using core;

namespace symfile.type
{
    public class PrimitiveType : IMemoryLayout, IEquatable<PrimitiveType>
    {
        public uint dataSize { get; }

        public int precedence => int.MinValue;

        public string fundamentalType { get; }

        public PrimitiveType(BaseType baseType)
        {
            switch (baseType)
            {
                case BaseType.Void:
                    fundamentalType = "void";
                    dataSize = 0;
                    break;
                case BaseType.Char:
                    fundamentalType = "char";
                    dataSize = 1;
                    break;
                case BaseType.Short:
                    fundamentalType = "short";
                    dataSize = 2;
                    break;
                case BaseType.Int:
                    fundamentalType = "int";
                    dataSize = 4;
                    break;
                case BaseType.Long:
                    fundamentalType = "long";
                    dataSize = 4;
                    break;
                case BaseType.Float:
                    fundamentalType = "float";
                    dataSize = 4;
                    break;
                case BaseType.Double:
                    fundamentalType = "double";
                    dataSize = 8;
                    break;
                case BaseType.UChar:
                    fundamentalType = "unsigned char";
                    dataSize = 1;
                    break;
                case BaseType.UShort:
                    fundamentalType = "unsigned short";
                    dataSize = 2;
                    break;
                case BaseType.UInt:
                    fundamentalType = "unsigned int";
                    dataSize = 4;
                    break;
                case BaseType.ULong:
                    fundamentalType = "unsigned long";
                    dataSize = 4;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(baseType), baseType, null);
            }
        }
        
        public string asIncompleteDeclaration(string identifier, string argList)
        {
            return identifier;
        }

        public string getAccessPathTo(uint offset)
        {
            if(offset != 0)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, null);

            return null;
        }

        public bool Equals(PrimitiveType other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(fundamentalType, other.fundamentalType) && dataSize == other.dataSize;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PrimitiveType) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (fundamentalType.GetHashCode() * 397) ^ (int) dataSize;
            }
        }
    }
}
