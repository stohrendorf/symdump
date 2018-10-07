using System.IO;
using symdump.symfile.util;

namespace symdump.symfile
{
    public class Label
    {
        private readonly TypedValue _typedOffset;

        public Label(TypedValue typedValue, BinaryReader fs)
        {
            _typedOffset = typedValue;
            Name = fs.ReadPascalString();
        }

        public Label(TypedValue typedValue, string name)
        {
            _typedOffset = typedValue;
            Name = name;
        }

        public uint Offset => (uint) _typedOffset.Value;

        public string Name { get; }

        public override string ToString()
        {
            return $"0x{Offset:X} {Name}";
        }
    }
}
