using System.IO;

namespace symdump.symfile
{
    public class Overlay
    {
        public readonly int Id;
        public readonly int Length;

        public Overlay(BinaryReader fs)
        {
            Length = fs.ReadInt32();
            Id = fs.ReadInt32();
        }

        public override string ToString()
        {
            return $"length={Length} id={Id}";
        }
    }
}
