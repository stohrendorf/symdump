using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using symdump.symfile.util;
using symdump.util;

namespace symdump.symfile
{
    public class Function
    {
        public readonly uint address;
        private readonly List<Block> m_blocks = new List<Block>();
        private readonly string m_file;
        private readonly uint m_lastLine;
        private readonly uint m_line;
        private readonly uint m_mask;
        private readonly int m_maskOffs;
        private readonly string m_name;

        private readonly List<string> m_parameters = new List<string>();
        private readonly Register m_register;
        private readonly string m_returnType;
        private readonly Register m_stackBase;
        private readonly uint m_stackFrameSize;

        public Function(BinaryReader reader, uint ofs, IReadOnlyDictionary<string, string> funcTypes)
        {
            address = ofs;

            m_stackBase = (Register) reader.ReadUInt16();
            m_stackFrameSize = reader.ReadUInt32();
            m_register = (Register) reader.ReadUInt16();
            m_mask = reader.ReadUInt32();
            m_maskOffs = reader.ReadInt32();

            m_line = reader.ReadUInt32();
            m_file = reader.readPascalString();
            m_name = reader.readPascalString();

            if (!funcTypes.TryGetValue(m_name, out m_returnType))
                m_returnType = "__UNKNOWN__";

            while (true)
            {
                var typedValue = new TypedValue(reader);

                if (reader.skipSld(typedValue))
                    continue;

                TypeInfo ti;
                string memberName;
                switch (typedValue.type & 0x7f)
                {
                    case 14: // end of function
                        m_lastLine = reader.ReadUInt32();
                        return;
                    case 16: // begin of block
                        m_blocks.Add(new Block(reader, (uint) typedValue.value, reader.ReadUInt32(), this));
                        continue;
                    case 20:
                        ti = reader.readTypeInfo(false);
                        memberName = reader.readPascalString();
                        break;
                    case 22:
                        ti = reader.readTypeInfo(true);
                        memberName = reader.readPascalString();
                        break;
                    default:
                        throw new Exception("Nope");
                }

                if (ti == null || memberName == null)
                    break;

                if (ti.classType == ClassType.Argument)
                    m_parameters.Add($"{ti.asCode(memberName)} /*stack {typedValue.value}*/");
                else if (ti.classType == ClassType.RegParam)
                    m_parameters.Add($"{ti.asCode(memberName)} /*${(Register) typedValue.value}*/");
            }
        }

        private IEnumerable<Register> savedRegisters => Enumerable.Range(0, 32)
            .Where(i => ((1 << i) & m_mask) != 0)
            .Select(i => (Register) i);

        public void dump(IndentedTextWriter writer)
        {
            writer.WriteLine("/*");
            writer.WriteLine($" * Offset 0x{address:X}");
            writer.WriteLine($" * {m_file} (line {m_line})");
            writer.WriteLine($" * Stack frame base ${m_stackBase}, size {m_stackFrameSize}");
            if (m_mask != 0)
                writer.WriteLine($" * Saved registers at offset {m_maskOffs}: {string.Join(" ", savedRegisters)}");
            writer.WriteLine(" */");

            writer.WriteLine(getSignature());

            m_blocks.ForEach(b => b.dump(writer));

            if (m_blocks.Count != 0)
                return;
            
            writer.WriteLine("{");
            writer.WriteLine("}");
        }

        public string getSignature()
        {
            return $"{m_returnType} /*${m_register}*/ {m_name}({string.Join(", ", m_parameters)})";
        }
    }
}