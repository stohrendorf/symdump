using System;
using core;
using symfile.type;

namespace symfile.memory
{
    public class PrimitiveType : IMemoryLayout, IEquatable<PrimitiveType>
    {
        public uint DataSize { get; }

        public int Precedence => int.MinValue;

        public string FundamentalType { get; }

        public IMemoryLayout Pointee => null;

        public PrimitiveType(BaseType baseType)
        {
            switch (baseType)
            {
                case BaseType.Void:
                    FundamentalType = "void";
                    DataSize = 0;
                    break;
                case BaseType.Char:
                    FundamentalType = "char";
                    DataSize = 1;
                    break;
                case BaseType.Short:
                    FundamentalType = "short";
                    DataSize = 2;
                    break;
                case BaseType.Int:
                    FundamentalType = "int";
                    DataSize = 4;
                    break;
                case BaseType.Long:
                    FundamentalType = "long";
                    DataSize = 4;
                    break;
                case BaseType.Float:
                    FundamentalType = "float";
                    DataSize = 4;
                    break;
                case BaseType.Double:
                    FundamentalType = "double";
                    DataSize = 8;
                    break;
                case BaseType.UChar:
                    FundamentalType = "unsigned char";
                    DataSize = 1;
                    break;
                case BaseType.UShort:
                    FundamentalType = "unsigned short";
                    DataSize = 2;
                    break;
                case BaseType.UInt:
                    FundamentalType = "unsigned int";
                    DataSize = 4;
                    break;
                case BaseType.ULong:
                    FundamentalType = "unsigned long";
                    DataSize = 4;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(baseType), baseType, null);
            }
        }
        
        public string AsIncompleteDeclaration(string identifier, string argList)
        {
            return identifier;
        }

        public string GetAccessPathTo(uint offset)
        {
            if(offset != 0)
                throw new UnalignedAccessException(offset, DataSize);

            return null;
        }

        public bool Equals(PrimitiveType other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(FundamentalType, other.FundamentalType) && DataSize == other.DataSize;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((PrimitiveType) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (FundamentalType.GetHashCode() * 397) ^ (int) DataSize;
            }
        }
    }
}
