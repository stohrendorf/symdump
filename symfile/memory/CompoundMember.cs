using System;
using System.IO;
using core;
using symfile.type;
using symfile.util;

namespace symfile.memory
{
    public class CompoundMember : IEquatable<CompoundMember>
    {
        public readonly string Name;
        public readonly FileEntry FileEntry;
        public readonly TypeDecoration TypeDecoration;
        public IMemoryLayout MemoryLayout => TypeDecoration.MemoryLayout;

        public CompoundMember(FileEntry fileEntry, BinaryReader reader, bool extended, IDebugSource debugSource)
        {
            FileEntry = fileEntry;
            TypeDecoration = reader.ReadTypeDecoration(extended, debugSource);
            Name = reader.ReadPascalString();

            if (TypeDecoration.ClassType != ClassType.Bitfield && TypeDecoration.ClassType != ClassType.StructMember &&
                TypeDecoration.ClassType != ClassType.UnionMember && TypeDecoration.ClassType != ClassType.EndOfStruct)
                throw new ArgumentOutOfRangeException(nameof(TypeDecoration.ClassType),
                    $"Unexpected class {TypeDecoration.ClassType}");
        }

        public override string ToString()
        {
            switch (TypeDecoration.ClassType)
            {
                case ClassType.Bitfield:
                    return TypeDecoration.AsDeclaration(Name) +
                           $" : {TypeDecoration.Size}; // offset={FileEntry.Value / 8}.{FileEntry.Value % 8}";
                case ClassType.StructMember:
                case ClassType.UnionMember:
                    return TypeDecoration.AsDeclaration(Name) +
                           $"; // size={TypeDecoration.Size}, offset={FileEntry.Value}";
                default:
                    throw new Exception($"Unexpected class {TypeDecoration.ClassType}");
            }
        }

        public bool Equals(CompoundMember other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name) && Equals(FileEntry, other.FileEntry) &&
                   Equals(TypeDecoration, other.TypeDecoration);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((CompoundMember) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (FileEntry != null ? FileEntry.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (TypeDecoration != null ? TypeDecoration.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
