using System.IO;
using symdump;

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

        public static TypeDef readTypeDef(this BinaryReader s)
        {
            return new TypeDef(s);
        }

        public static ClassType readClassType(this BinaryReader s)
        {
            return (ClassType) s.ReadUInt16();
        }
    }
}