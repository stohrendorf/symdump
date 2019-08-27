using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using symdump.symfile.util;

namespace symdump.symfile
{
    public class TaggedSymbol : IEquatable<TaggedSymbol>
    {
        public readonly DerivedTypeDef DerivedTypeDef;
        public readonly uint[] Extents;
        public readonly uint Size;
        public readonly string Tag;
        public readonly SymbolType Type;

        public TaggedSymbol(BinaryReader reader, bool isArray)
        {
            Type = reader.ReadSymbolType();
            DerivedTypeDef = reader.ReadDerivedTypeDef();
            Size = reader.ReadUInt32();

            if (isArray)
            {
                var n = reader.ReadUInt16();
                Extents = new uint[n];
                for (var i = 0; i < n; ++i)
                    Extents[i] = reader.ReadUInt32();

                Tag = reader.ReadPascalString();
            }
            else
            {
                Extents = new uint[0];
                Tag = null;
            }
        }

        private bool IsFake => Tag != null && new Regex(@"^\.\d+fake$").IsMatch(Tag);

        public bool Equals(TaggedSymbol other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            var sameTag = Tag == other.Tag || IsFake || other.IsFake;

            return Type == other.Type && DerivedTypeDef.Equals(other.DerivedTypeDef) && Size == other.Size &&
                   Extents.SequenceEqual(other.Extents) && sameTag;
        }

        public override string ToString()
        {
            return $"classType={Type} typeDef={DerivedTypeDef} size={Size}, dims=[{string.Join(",", Extents)}]";
        }

        public string AsCode(string name)
        {
            return DerivedTypeDef.AsCode(name, this);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((TaggedSymbol) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) Type;
                hashCode = (hashCode * 397) ^ DerivedTypeDef.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) Size;
                hashCode = (hashCode * 397) ^ Extents.GetHashCode();
                hashCode = (hashCode * 397) ^ (!IsFake ? Tag.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}