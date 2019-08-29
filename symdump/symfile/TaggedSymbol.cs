using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using symdump.symfile.util;
using symdump.util;

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

        public string InnerCode { get; set; }

        public bool IsFake => Tag?.IsFake() ?? false;

        public bool Equals(TaggedSymbol other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            var sameTag = Tag == other.Tag || IsFake || other.IsFake;

            return Type == other.Type
                   && DerivedTypeDef.Equals(other.DerivedTypeDef)
                   && (Size == other.Size || Type == SymbolType.Typedef || Size == 0 || other.Size == 0)
                   && Extents.SequenceEqual(other.Extents)
                   && sameTag;
        }

        public void ApplyInline(IDictionary<string, EnumDef> enums, IDictionary<string, StructDef> structs,
            IDictionary<string, UnionDef> unions)
        {
            if (!IsFake || Type == SymbolType.EndOfStruct)
                return;

            var sb = new StringBuilder();
            var writer = new IndentedTextWriter(new StringWriter(sb));
            switch (DerivedTypeDef.Type)
            {
                case PrimitiveType.StructDef:
                    structs[Tag].Dump(writer, true);
                    break;
                case PrimitiveType.UnionDef:
                    unions[Tag].Dump(writer, true);
                    break;
                case PrimitiveType.EnumDef:
                    enums[Tag].Dump(writer, true);
                    break;
                default:
                    throw new Exception($"Cannot de-fake {DerivedTypeDef.Type} (Tag {Tag})");
            }

            InnerCode = sb.ToString();
            if (string.IsNullOrEmpty(InnerCode)) InnerCode = null;
        }

        public override string ToString()
        {
            return
                $"{nameof(Type)}={Type} {nameof(DerivedTypeDef)}={DerivedTypeDef} {nameof(Size)}={Size}, {nameof(Extents)}=[{string.Join(",", Extents)}]";
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
