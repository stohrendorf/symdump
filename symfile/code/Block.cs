using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using core;
using core.util;
using mips.disasm;
using symfile.type;
using symfile.util;

namespace symfile.code
{
    public class Block
    {
        public class VarInfo
        {
            public readonly string name;
            public readonly TypeDecoration typeDecoration;
            public readonly FileEntry fileEntry;

            public VarInfo(string name, TypeDecoration typeDecoration, FileEntry fileEntry)
            {
                this.name = name;
                this.typeDecoration = typeDecoration;
                this.fileEntry = fileEntry;
            }

            public override string ToString()
            {
                switch (typeDecoration.classType)
                {
                    case ClassType.AutoVar:
                        return $"{typeDecoration.asDeclaration(name)}; /* sp {fileEntry.value} */";
                    case ClassType.Register:
                        return $"{typeDecoration.asDeclaration(name)}; /* ${(Register) fileEntry.value} */";
                    case ClassType.Static:
                        return $"static {typeDecoration.asDeclaration(name)}; // offset 0x{fileEntry.value:x}";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public readonly uint endLine;
        public readonly uint endOffset;

        public readonly Function function;
        public readonly List<NamedLocation> labels = new List<NamedLocation>();
        public readonly uint startLine;

        public readonly uint startOffset;
        public readonly List<Block> subBlocks = new List<Block>();
        public readonly Dictionary<string, TypeDecoration> typedefs = new Dictionary<string, TypeDecoration>();
        public readonly Dictionary<string, VarInfo> vars = new Dictionary<string, VarInfo>();

        public Block(uint ofs, uint ln, Function f, IDebugSource debugSource)
            : this(null, ofs, ln, f, debugSource)
        {
        }

        public Block(BinaryReader reader, uint ofs, uint ln, Function f, IDebugSource debugSource)
        {
            startOffset = ofs;
            startLine = ln;
            function = f;

            if (reader == null)
                return;

            while (true)
            {
                var typedValue = new FileEntry(reader);

                if (reader.skipSld(typedValue))
                    continue;

                switch (typedValue.type & 0x7f)
                {
                    case 16:
                        subBlocks.Add(new Block(reader, (uint) typedValue.value, reader.ReadUInt32(), function, debugSource));
                        break;
                    case 18:
                        endOffset = (uint) typedValue.value;
                        endLine = reader.ReadUInt32();
                        return;
                    case 20:
                    {
                        var ti = reader.readTypeDecoration(false, debugSource);
                        var memberName = reader.readPascalString();
                        Debug.Assert(!string.IsNullOrEmpty(memberName));
                        switch (ti.classType)
                        {
                            case ClassType.AutoVar:
                            case ClassType.Register:
                            case ClassType.Static:
                                vars.Add(memberName, new VarInfo(memberName, ti, typedValue));
                                break;
                            case ClassType.Typedef:
                                typedefs.Add(memberName, ti);
                                break;
                            case ClassType.Label:
                                labels.Add(new NamedLocation((uint) typedValue.value, memberName));
                                break;
                            default:
                                throw new Exception($"Unexpected class type {ti.classType}");
                        }
                        break;
                    }
                    case 22:
                    {
                        var ti = reader.readTypeDecoration(true, debugSource);
                        var memberName = reader.readPascalString();
                        Debug.Assert(!string.IsNullOrEmpty(memberName));
                        switch (ti.classType)
                        {
                            case ClassType.AutoVar:
                            case ClassType.Register:
                            case ClassType.Static:
                                vars.Add(memberName, new VarInfo(memberName, ti, typedValue));
                                break;
                            case ClassType.Typedef:
                                typedefs.Add(memberName, ti);
                                break;
                            case ClassType.Label:
                                labels.Add(new NamedLocation((uint) typedValue.value, memberName));
                                break;
                            default:
                                throw new Exception($"Unexpected class type {ti.classType}");
                        }
                        break;
                    }
                }
            }
        }

        public void dump(IndentedTextWriter writer)
        {
            writer.WriteLine($"{{ // line {startLine}, offset 0x{startOffset:x}");
            ++writer.indent;
            foreach (var t in typedefs)
                writer.WriteLine($"typedef {t.Value.asDeclaration(t.Key)};");
            foreach (var varInfo in vars)
                writer.WriteLine(varInfo.Value);
            foreach (var l in labels)
                writer.WriteLine(l);
            subBlocks.ForEach(b => b.dump(writer));
            --writer.indent;
            writer.WriteLine($"}} // line {endLine}, offset 0x{endOffset:x}");
        }
    }
}
