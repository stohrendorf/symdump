using System;
using core;

namespace symfile.type
{
    public class PrimitiveType : IMemoryLayout, IEquatable<PrimitiveType>
    {
        public uint dataSize { get; }

        public int precedence => int.MinValue;

        private readonly string m_ctype;

        public PrimitiveType(BaseType baseType)
        {
            switch (baseType)
            {
                case BaseType.Void:
                    m_ctype = "void";
                    dataSize = 0;
                    break;
                case BaseType.Char:
                    m_ctype = "char";
                    dataSize = 1;
                    break;
                case BaseType.Short:
                    m_ctype = "short";
                    dataSize = 2;
                    break;
                case BaseType.Int:
                    m_ctype = "int";
                    dataSize = 4;
                    break;
                case BaseType.Long:
                    m_ctype = "long";
                    dataSize = 4;
                    break;
                case BaseType.Float:
                    m_ctype = "float";
                    dataSize = 4;
                    break;
                case BaseType.Double:
                    m_ctype = "double";
                    dataSize = 8;
                    break;
                case BaseType.UChar:
                    m_ctype = "unsigned char";
                    dataSize = 1;
                    break;
                case BaseType.UShort:
                    m_ctype = "unsigned short";
                    dataSize = 2;
                    break;
                case BaseType.UInt:
                    m_ctype = "unsigned int";
                    dataSize = 4;
                    break;
                case BaseType.ULong:
                    m_ctype = "unsigned long";
                    dataSize = 4;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(baseType), baseType, null);
            }
        }
        
        public string asDeclaration(string identifier, string argList)
        {
            return $"{m_ctype} {identifier}";
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
            return string.Equals(m_ctype, other.m_ctype) && dataSize == other.dataSize;
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
                return ((m_ctype != null ? m_ctype.GetHashCode() : 0) * 397) ^ (int) dataSize;
            }
        }
    }
}
