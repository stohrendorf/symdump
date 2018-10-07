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
                result += fs.ReadChar();
            return result;
        }

        public static TypeDef ReadTypeDef(this BinaryReader s)
        {
            return new TypeDef(s);
        }

        public static TypeInfo ReadTypeInfo(this BinaryReader s, bool withDimensions)
        {
            return new TypeInfo(s, withDimensions);
        }

        public static ClassType ReadClassType(this BinaryReader s)
        {
            return (ClassType) s.ReadUInt16();
        }

        public static bool SkipSld(this BinaryReader reader, TypedValue typedValue)
        {
            switch (typedValue.Type & 0x7f)
            {
                case 0:
                    return true;
                case 2:
                    reader.Skip(1);
                    return true;
                case 4:
                    reader.Skip(2);
                    return true;
                case 6:
                    reader.Skip(4);
                    return true;
                case 8:
                    reader.Skip(4);
                    reader.Skip(reader.ReadByte());
                    return true;
                case 10:
                    return true;
            }

            return false;
        }
    }
}
