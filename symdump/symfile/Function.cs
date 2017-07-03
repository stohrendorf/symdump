using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using symdump;
using symfile.util;

namespace symfile
{
    public class Function
    {
        public readonly uint address;
        private readonly Register stackBase;
        private readonly uint stackFrameSize;
        private readonly Register register;
        private readonly uint mask;
        private readonly int maskOffs;
        private readonly uint line;
        private readonly string file;
        private readonly string name;
        private readonly uint lastLine;
        private readonly string returnType;

        private readonly List<string> parameters = new List<string>();
        private readonly List<Block> blocks = new List<Block>();

        private IEnumerable<Register> savedRegisters => Enumerable.Range(0, 32)
            .Where(i => ((1 << i) & mask) != 0)
            .Select(i => (Register) i);

        public Function(BinaryReader reader, uint ofs, IReadOnlyDictionary<string, string> funcTypes)
        {
            address = ofs;

            stackBase = (Register)reader.ReadUInt16();
            stackFrameSize = reader.ReadUInt32();
            register = (Register) reader.ReadUInt16();
            mask = reader.ReadUInt32();
            maskOffs = reader.ReadInt32();

            line = reader.ReadUInt32();
            file = reader.readPascalString();
            name = reader.readPascalString();

            if (!funcTypes.TryGetValue(name, out returnType))
                returnType = "__UNKNOWN__";

            while (true)
            {
                var typedValue = new TypedValue(reader);

                if (reader.skipSLD(typedValue))
                    continue;

                TypeInfo ti = null;
                string memberName = null;
                switch (typedValue.type & 0x7f)
                {
                    case 14: // end of function
                        lastLine = reader.ReadUInt32();
                        return;
                    case 16: // begin of block
                        blocks.Add(new Block(reader, (uint) typedValue.value, reader.ReadUInt32(), this));
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
                {
                    parameters.Add($"{ti.asCode(memberName)} /*stack {typedValue.value}*/");
                }
                else if (ti.classType == ClassType.RegParam)
                {
                    parameters.Add($"{ti.asCode(memberName)} /*${(Register)typedValue.value}*/");
                }
            }
        }

        public void dump(IndentedTextWriter writer)
        {
            writer.WriteLine("/*");
            writer.WriteLine($" * Offset 0x{address:X}");
            writer.WriteLine($" * {file} (line {line})");
            writer.WriteLine($" * Stack frame base ${stackBase}, size {stackFrameSize}");
            if(mask != 0)
                writer.WriteLine($" * Saved registers at offset {maskOffs}: {string.Join(" ", savedRegisters)}");
            writer.WriteLine(" */");

            writer.WriteLine(getSignature());

            blocks.ForEach(b => b.dump(writer));

            if (blocks.Count == 0)
            {
                writer.WriteLine("{");
                writer.WriteLine("}");
            }
        }

        public string getSignature()
        {
            return $"{returnType} /*${register}*/ {name}({string.Join(", ", parameters)})";
        }
    }
}