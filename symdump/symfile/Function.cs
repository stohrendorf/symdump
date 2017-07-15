using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using symdump.symfile.type;
using symdump.symfile.util;
using symdump.util;

namespace symdump.symfile
{
    public class Function
    {
        public class ArgumentInfo
        {
            public readonly string name;
            public readonly TypedValue typedValue;
            public readonly TypeInfo typeInfo;
            public readonly Register stackBase;

            public readonly ICompoundType compoundType;

            public ArgumentInfo(SymFile symFile, string name, TypedValue typedValue, TypeInfo typeInfo, Register stackBase)
            {
                this.name = name;
                this.typedValue = typedValue;
                this.typeInfo = typeInfo;
                this.stackBase = stackBase;
                this.compoundType = symFile.findTypeDefinition(typeInfo.tag);
            }

            public override string ToString()
            {
                if (typeInfo.classType == ClassType.Argument)
                    return $"{typeInfo.asCode(name)} /*${stackBase} {typedValue.value}*/";
                else if (typeInfo.classType == ClassType.RegParam)
                    return $"{typeInfo.asCode(name)} /*${(Register) typedValue.value}*/";
                else
                    throw new Exception("Meh");
            }

            public bool isStruct => typeInfo.isStruct;
        }

        public readonly uint address;
        private readonly Block m_body;
        private readonly string m_file;
        private readonly uint m_lastLine;
        private readonly uint m_line;
        private readonly uint m_mask;
        private readonly int m_maskOffs;
        public readonly string name;

        public readonly IDictionary<Register, List<ArgumentInfo>> registerParameters =
            new SortedDictionary<Register, List<ArgumentInfo>>();

        private readonly IDictionary<int, ArgumentInfo> m_stackParameters = new SortedDictionary<int, ArgumentInfo>();
        private readonly Register m_returnAddressRegister;
        private readonly TypeInfo m_returnType;
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
            
            m_body = new Block(address, m_line, this);

            symFile.funcTypes.TryGetValue(name, out m_returnType);

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
                        m_body.subBlocks.Add(new Block(reader, (uint) typedValue.value, reader.ReadUInt32(), this));
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

                var argInfo = new ArgumentInfo(symFile, memberName, typedValue, ti, m_stackBase);
                switch (ti.classType)
                {
                    case ClassType.Argument:
                        m_stackParameters.Add(typedValue.value, argInfo);
                        break;
                    case ClassType.RegParam:
                        List<ArgumentInfo> infoList;
                        if (!registerParameters.TryGetValue((Register) typedValue.value, out infoList))
                            registerParameters.Add((Register) typedValue.value, infoList = new List<ArgumentInfo>());
                        infoList.Add(argInfo);
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
            var parameters = registerParameters.Values.SelectMany(p => p).Concat(m_stackParameters.Values);
            return m_returnType?.asCode(name, string.Join(", ", parameters));
        }
    }
}
