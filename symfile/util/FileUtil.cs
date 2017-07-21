using System.IO;
using core;
using symfile.type;

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

        public static TypeDecoration readTypeDecoration(this BinaryReader s, bool withDimensions, IDebugSource debugSource)
        {
            return new TypeDecoration(s, withDimensions, debugSource);
        }

        public static ClassType readClassType(this BinaryReader s)
        {
            return (ClassType) s.ReadUInt16();
        }

        public static bool skipSld(this BinaryReader reader, FileEntry fileEntry)
        {
            switch (fileEntry.type & 0x7f)
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
