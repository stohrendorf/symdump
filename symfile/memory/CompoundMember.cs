using System;
using System.IO;
using core;
using symfile.type;
using symfile.util;

namespace symfile.memory
{
    public class CompoundMember : IEquatable<CompoundMember>
    {
        public readonly string name;
        public readonly FileEntry fileEntry;
        public readonly TypeDecoration typeDecoration;
        public IMemoryLayout memoryLayout => typeDecoration.memoryLayout;

        public CompoundMember(FileEntry fileEntry, BinaryReader reader, bool extended, IDebugSource debugSource)
        {
            this.fileEntry = fileEntry;
            typeDecoration = reader.readTypeDecoration(extended, debugSource);
            name = reader.readPascalString();

            if (typeDecoration.classType != ClassType.Bitfield && typeDecoration.classType != ClassType.StructMember &&
                typeDecoration.classType != ClassType.UnionMember && typeDecoration.classType != ClassType.EndOfStruct)
                throw new ArgumentOutOfRangeException(nameof(typeDecoration.classType),
                    $"Unexpected class {typeDecoration.classType}");
        }

        public override string ToString()
        {
            switch (typeDecoration.classType)
            {
                case ClassType.Bitfield:
                    return typeDecoration.asDeclaration(name) +
                           $" : {typeDecoration.size}; // offset={fileEntry.value / 8}.{fileEntry.value % 8}";
                case ClassType.StructMember:
                case ClassType.UnionMember:
                    return typeDecoration.asDeclaration(name) +
                           $"; // size={typeDecoration.size}, offset={fileEntry.value}";
                default:
                    throw new Exception($"Unexpected class {typeDecoration.classType}");
            }
        }

        public bool Equals(CompoundMember other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(name, other.name) && Equals(fileEntry, other.fileEntry) &&
                   Equals(typeDecoration, other.typeDecoration);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CompoundMember) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (name != null ? name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (fileEntry != null ? fileEntry.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (typeDecoration != null ? typeDecoration.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
