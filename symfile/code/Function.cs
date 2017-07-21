using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using core;
using core.util;
using mips.disasm;
using symfile.type;
using symfile.util;

namespace symfile.code
{
    public class Function : IFunction
    {
        public class ArgumentInfo : IDeclaration
        {
            public string name { get; }

            public IMemoryLayout memoryLayout => typeDecoration.memoryLayout;

            public readonly TypeDecoration typeDecoration;
            public readonly Register stackBase;
            public readonly uint? stackOffset;
            public readonly Register? register;

            public ArgumentInfo(string name, TypeDecoration typeDecoration, Register stackBase, uint? stackOffset, Register? register)
            {
                this.name = name;
                this.typeDecoration = typeDecoration;
                this.stackBase = stackBase;
                this.stackOffset = stackOffset;
                this.register = register;
            }

            public override string ToString()
            {
                if (typeDecoration.classType == ClassType.Argument)
                {
                    Debug.Assert(stackOffset != null);
                    return $"{typeDecoration.asDeclaration(name)} /*${stackBase} {stackOffset}*/";
                }
                else if (typeDecoration.classType == ClassType.RegParam)
                {
                    Debug.Assert(register != null);
                    return $"{typeDecoration.asDeclaration(name)} /*${register}*/";
                }
                else
                    throw new Exception("Meh");
            }
        }

        public uint address { get; }
        private readonly Block m_body;
        private readonly string m_file;
        private readonly uint m_lastLine;
        private readonly uint m_line;
        private readonly uint m_mask;
        private readonly int m_maskOffs;
        public string name { get; }

        private readonly IDictionary<Register, ArgumentInfo> m_registerParameters =
            new SortedDictionary<Register, ArgumentInfo>();

        public IEnumerable<KeyValuePair<int, IDeclaration>> registerParameters =>
            m_registerParameters.Select(p => new KeyValuePair<int, IDeclaration>((int) p.Key, p.Value));

        private readonly IDictionary<int, ArgumentInfo> m_stackParameters = new SortedDictionary<int, ArgumentInfo>();
        private readonly Register m_returnAddressRegister;
        private readonly TypeDecoration m_returnType;
        private readonly Register m_stackBase;
        private readonly uint m_stackFrameSize;

        public Function(BinaryReader reader, uint ofs, SymFile symFile)
        {
            address = ofs;

            m_stackBase = (Register) reader.ReadUInt16();
            m_stackFrameSize = reader.ReadUInt32();
            m_returnAddressRegister = (Register) reader.ReadUInt16();
            m_mask = reader.ReadUInt32();
            m_maskOffs = reader.ReadInt32();

            m_line = reader.ReadUInt32();
            m_file = reader.readPascalString();
            name = reader.readPascalString();

            m_body = new Block(address, m_line, this, symFile);

            symFile.funcTypes.TryGetValue(name, out m_returnType);

            while (true)
            {
                var typedValue = new FileEntry(reader);

                if (reader.skipSld(typedValue))
                    continue;

                TypeDecoration ti;
                string memberName;
                switch (typedValue.type & 0x7f)
                {
                    case 14: // end of function
                        m_lastLine = reader.ReadUInt32();
                        return;
                    case 16: // begin of block
                        m_body.subBlocks.Add(new Block(reader, (uint) typedValue.value, reader.ReadUInt32(), this,
                            symFile));
                        continue;
                    case 20:
                        ti = reader.readTypeDecoration(false, symFile);
                        memberName = reader.readPascalString();
                        break;
                    case 22:
                        ti = reader.readTypeDecoration(true, symFile);
                        memberName = reader.readPascalString();
                        break;
                    default:
                        throw new Exception("Nope");
                }

                if (ti == null || memberName == null)
                    break;

                switch (ti.classType)
                {
                    case ClassType.Argument:
                        //Debug.Assert(m_registerParameters.Count >= 4);
                        m_stackParameters[m_stackParameters.Count * 4] = new ArgumentInfo(memberName, ti, m_stackBase, (uint) (m_stackParameters.Count * 4), null);
                        break;
                    case ClassType.RegParam:
                        Debug.Assert(m_registerParameters.Count < 4);
                        m_registerParameters[Register.a0 + m_registerParameters.Count] = new ArgumentInfo(memberName, ti, m_stackBase, null, Register.a0 + m_registerParameters.Count);
                        break;
                    default:
                        m_body.vars.Add(memberName, new Block.VarInfo(memberName, ti, typedValue));
                        break;
                }
            }

            throw new Exception("Should never reach this");
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
            writer.WriteLine($" * Caller return address in ${m_returnAddressRegister}");
            if (m_mask != 0)
                writer.WriteLine($" * Saved registers at offset {m_maskOffs}: {string.Join(" ", savedRegisters)}");
            writer.WriteLine(" */");

            writer.WriteLine(getSignature());

            m_body.dump(writer);
        }

        public string getSignature()
        {
            var parameters = m_registerParameters.Values.Concat(m_stackParameters.Values);
            return m_returnType?.asDeclaration(name, string.Join(", ", parameters));
        }
    }
}
