using System;
using System.IO;
using System.Linq;
using symdump.symfile.util;
using symdump.util;

namespace symdump.symfile
{
    public class TaggedSymbol : IEquatable<TaggedSymbol>
    {
        public readonly DerivedTypeDef DerivedTypeDef;
        public readonly uint Size;
        public readonly SymbolType Type;
        public uint[] Extents;
        public bool IsResolvedTypedef;
        public string Tag;

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

        public string InnerCode { get; private set; }
        public bool IsFake => Tag?.IsFake() ?? false;

        public bool Equals(TaggedSymbol other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            var sameTag = Tag == other.Tag || IsFake || other.IsFake;
            if (!sameTag)
                return false;

            if (Type != other.Type && Type != SymbolType.Typedef && other.Type == SymbolType.Typedef)
                return false;

            if (Size != other.Size && Size != 0 && other.Size != 0)
                return false;

            return DerivedTypeDef.Equals(other.DerivedTypeDef)
                   && Extents.SequenceEqual(other.Extents);
        }

        public bool IsPartOf(TaggedSymbol other, int dropLast)
        {
            if (dropLast < 1)
                throw new ArgumentOutOfRangeException(nameof(dropLast));

            if (ReferenceEquals(null, other))
                return false;

            if (Type != SymbolType.Typedef)
                throw new Exception("IsPartOf only works for typedefs");

            var sameTag = Tag == other.Tag || IsFake || other.IsFake;
            if (!sameTag)
                return false;

            return DerivedTypeDef.IsPartOf(other.DerivedTypeDef, dropLast, out var droppedExtents)
                   && Extents.SequenceEqual(other.Extents.SkipLast(droppedExtents));
        }

        public override string ToString()
        {
            return
                $"{nameof(Tag)}={Tag} {nameof(Type)}={Type} {nameof(DerivedTypeDef)}={DerivedTypeDef} {nameof(Size)}={Size}, {nameof(Extents)}=[{string.Join(",", Extents)}]";
        }

        public string AsCode(string name, bool onlyDecorated = false)
        {
            return DerivedTypeDef.AsCode(name, this, onlyDecorated);
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

        public void ResolveTypedef(ObjectFile objectFile)
        {
            if (string.IsNullOrEmpty(Tag) || !IsFake || IsResolvedTypedef)
                return;

            var resolved = objectFile.ReverseTypedef(this, out var droppedDerived);
            if (resolved == null)
            {
                var sb = new StringWriter();
                var writer = new IndentedTextWriter(sb);
                objectFile.ComplexTypes[Tag].Dump(writer, true);
                objectFile.ComplexTypes.Remove(Tag);
                InnerCode = sb.ToString();
                IsResolvedTypedef = true;
                return;
            }

            Tag = resolved;
            IsResolvedTypedef = true;

            if (droppedDerived == 0)
                return;

            var droppedArrays = DerivedTypeDef.RetainDerived(droppedDerived);
            if (droppedArrays > 0)
                Extents = Extents.SkipLast(droppedArrays).ToArray();
        }
    }
}