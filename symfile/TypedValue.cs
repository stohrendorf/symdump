using System;
using System.IO;

namespace symfile
{
    public class TypedValue : IEquatable<TypedValue>
    {
        public readonly byte type;
        public readonly int value;

        public TypedValue(BinaryReader fs)
        {
            value = fs.ReadInt32();
            type = fs.ReadByte();
        }

        public bool isLabel => (type & 0x80) == 0;

        public bool Equals(TypedValue other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return type == other.type && value == other.value;
        }

        public override string ToString()
        {
            return $"value={value} type={type} isLabel={isLabel}";
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((TypedValue) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (type.GetHashCode() * 397) ^ value;
            }
        }
    }
}
