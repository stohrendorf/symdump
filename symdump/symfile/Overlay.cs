using System.IO;

namespace symdump.symfile
{
    public class Overlay(BinaryReader fs)
    {
        private readonly int _id = fs.ReadInt32();
        private readonly int _length = fs.ReadInt32();

        public override string ToString()
        {
            return $"length={_length} id={_id}";
        }
    }
}
