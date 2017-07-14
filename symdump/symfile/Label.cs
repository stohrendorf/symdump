using System.IO;
using symdump.symfile.util;

namespace symdump.symfile
{
    public class Label
    {
        private readonly TypedValue m_typedOffset;

        public Label(TypedValue typedValue, BinaryReader fs)
        {
            m_typedOffset = typedValue;
            name = fs.readPascalString();
        }

        public Label(TypedValue typedValue, string name)
        {
            m_typedOffset = typedValue;
            this.name = name;
        }

        public uint offset => (uint) m_typedOffset.value;

        public string name { get; }

        public override string ToString()
        {
            return $"0x{offset:X} {name}";
        }
    }
}
