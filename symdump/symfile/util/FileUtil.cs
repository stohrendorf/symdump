using System.IO;

namespace symdump.symfile.util
{
    public static class FileUtil
    {
        public static void Skip(this BinaryReader s, int n)
        {
            s.BaseStream.Seek(n, SeekOrigin.Current);
        }

        public static string ReadPascalString(this BinaryReader fs)
        {
            var length = fs.ReadByte();
            var result = "";
            while (length-- > 0)
            {
                var b = fs.ReadByte();
                if (b >= 0x20 && b < 0x80)
                    result += (char) b;
                else
                    result += $"\\x{b:x2}";
            }

            return result;
        }

        public static DerivedTypeDef ReadDerivedTypeDef(this BinaryReader s)
        {
            return new DerivedTypeDef(s);
        }

        public static TaggedSymbol ReadTaggedSymbol(this BinaryReader s, bool isArray)
        {
            return new TaggedSymbol(s, isArray);
        }

        public static SymbolType ReadSymbolType(this BinaryReader s)
        {
            return (SymbolType) s.ReadUInt16();
        }

        public static bool SkipSld(this BinaryReader reader, TypedValue typedValue)
        {
            switch (typedValue.Type & 0x7f)
            {
                case TypedValue.IncSld:
                    return true;
                case TypedValue.AddSld1:
                    reader.Skip(1);
                    return true;
                case TypedValue.AddSld2:
                    reader.Skip(2);
                    return true;
                case TypedValue.SetSld:
                    reader.Skip(4);
                    return true;
                case TypedValue.BeginSld:
                    reader.Skip(4);
                    reader.Skip(reader.ReadByte());
                    return true;
                case TypedValue.EndSldInfo:
                    return true;
            }

            return false;
        }
    }
}
