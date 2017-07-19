using System.IO;
using core;

namespace symfile.util
{
    public static class FileUtil
    {
        public static void skip(this BinaryReader s, int n)
        {
            s.BaseStream.Seek(n, SeekOrigin.Current);
        }

        public static string readPascalString(this BinaryReader fs)
        {
            var length = fs.ReadByte();
            var result = "";
            while (length-- > 0)
                result += fs.ReadChar();
            return result;
        }

        public static TypeDef readTypeDef(this BinaryReader reader)
        {
            return new TypeDef(reader);
        }

        public static TypeInfo readTypeInfo(this BinaryReader s, bool withDimensions, IDebugSource debugSource)
        {
            return new TypeInfo(s, withDimensions, debugSource);
        }

        public static ClassType readClassType(this BinaryReader s)
        {
            return (ClassType) s.ReadUInt16();
        }

        public static bool skipSld(this BinaryReader reader, TypedValue typedValue)
        {
            switch (typedValue.type & 0x7f)
            {
                case 0:
                    return true;
                case 2:
                    reader.skip(1);
                    return true;
                case 4:
                    reader.skip(2);
                    return true;
                case 6:
                    reader.skip(4);
                    return true;
                case 8:
                    reader.skip(4);
                    reader.skip(reader.ReadByte());
                    return true;
                case 10:
                    return true;
            }

            return false;
        }
    }
}
