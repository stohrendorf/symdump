using System;
using System.IO;

namespace symfile
{
    public class FileEntry : IEquatable<FileEntry>
    {
        public readonly byte Type;
        public readonly int Value;

        public FileEntry(BinaryReader fs)
        {
            Value = fs.ReadInt32();
            Type = fs.ReadByte();
        }

        public bool IsLabel => (Type & 0x80) == 0;

        public bool Equals(FileEntry other)
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
            return Equals((FileEntry) obj);
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