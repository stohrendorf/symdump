using System.IO;

namespace symfile
{
    public class TypedValue
    {
        public readonly byte type;
        public readonly int value;

        public TypedValue(BinaryReader fs)
        {
            value = fs.ReadInt32();
            type = fs.ReadByte();
        }

        public bool isLabel => (type & 0x80) == 0;

        public override string ToString()
        {
            return $"value={value} type={type} isLabel={isLabel}";
        }
    }
}