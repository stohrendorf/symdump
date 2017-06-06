using System.IO;

namespace symdump
{
    public static class FileUtil
    {
        public static uint ReadU4(this FileStream s)
        {
            var b = new byte[4];
            s.Read(b, 0, 4);
            return ((uint) b[0] << 0) | ((uint) b[1] << 8) | ((uint) b[2] << 16) | ((uint) b[3] << 24);
        }

        public static ushort ReadU2(this FileStream s)
        {
            var b = new byte[2];
            s.Read(b, 0, 2);
            return (ushort) ((b[0] << 0) | (b[1] << 8));
        }

        public static byte ReadU1(this FileStream s)
        {
            return (byte) s.ReadByte();
        }

        public static FileStream Skip(this FileStream s, int n)
        {
            s.Seek(n, SeekOrigin.Current);
            return s;
        }

        public static string readPascalString(this FileStream fs)
        {
            var length = fs.ReadU1();
            var result = "";
            while (length-- > 0)
                result += (char) fs.ReadU1();
            return result;
        }

        public static TypeDef readTypeDef(this FileStream s)
        {
            return new TypeDef(s);
        }

        public static ClassType readClassType(this FileStream s)
        {
            return (ClassType) s.ReadU2();
        }
    }
}