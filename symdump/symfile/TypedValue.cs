using System;
using System.IO;

namespace symdump.symfile
{
    public class TypedValue : IEquatable<TypedValue>
    {
        public const int IncSLD = 0;
        public const int AddSLD1 = 2;
        public const int AddSLD2 = 4;
        public const int SetSLD = 6;
        public const int SetSLDFile = 8;
        public const int EndSLDInfo = 10;
        public const int Function = 12;
        public const int FunctionEnd = 14;
        public const int Block = 16;
        public const int BlockEnd = 18;
        public const int Definition = 20;
        public const int ArrayDefinition = 22;
        public const int Overlay = 24;
        public const int SetOverlay = 26;
        public readonly byte Type;
        public readonly int Value;

        public TypedValue(BinaryReader fs)
        {
            Value = fs.ReadInt32();
            Type = fs.ReadByte();
        }

        public bool IsLabel => (Type & 0x80) == 0;

        public bool Equals(TypedValue other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Type == other.Type && Value == other.Value;
        }

        public override string ToString()
        {
            return $"value={Value} type={Type} isLabel={IsLabel}";
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
                return (Type.GetHashCode() * 397) ^ Value;
            }
        }
    }
}