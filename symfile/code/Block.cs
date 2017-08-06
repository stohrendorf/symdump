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
            public readonly string Name;
            public readonly TypeDecoration TypeDecoration;
            public readonly FileEntry FileEntry;

            public VarInfo(string name, TypeDecoration typeDecoration, FileEntry fileEntry)
            {
                Name = name;
                TypeDecoration = typeDecoration;
                FileEntry = fileEntry;
            }

            public override string ToString()
            {
                switch (TypeDecoration.ClassType)
                {
                    case ClassType.AutoVar:
                        return $"{TypeDecoration.AsDeclaration(Name)}; /* sp {FileEntry.value} */";
                    case ClassType.Register:
                        return $"{TypeDecoration.AsDeclaration(Name)}; /* ${(Register) FileEntry.value} */";
                    case ClassType.Static:
                        return $"static {TypeDecoration.AsDeclaration(Name)}; // offset 0x{FileEntry.value:x}";
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

                if (reader.SkipSld(typedValue))
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
                        var ti = reader.ReadTypeDecoration(false, debugSource);
                        var memberName = reader.ReadPascalString();
                        Debug.Assert(!string.IsNullOrEmpty(memberName));
                        switch (ti.ClassType)
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
                                throw new Exception($"Unexpected class type {ti.ClassType}");
                        }
                        break;
                    }
                    case 22:
                    {
                        var ti = reader.ReadTypeDecoration(true, debugSource);
                        var memberName = reader.ReadPascalString();
                        Debug.Assert(!string.IsNullOrEmpty(memberName));
                        switch (ti.ClassType)
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
                                throw new Exception($"Unexpected class type {ti.ClassType}");
                        }
                        break;
                    }
                }
            }
        }

        public void dump(IndentedTextWriter writer)
        {
            writer.WriteLine($"{{ // line {startLine}, offset 0x{startOffset:x}");
            ++writer.Indent;
            foreach (var t in typedefs)
                writer.WriteLine($"typedef {t.Value.AsDeclaration(t.Key)};");
            foreach (var varInfo in vars)
                writer.WriteLine(varInfo.Value);
            foreach (var l in labels)
                writer.WriteLine(l);
            subBlocks.ForEach(b => b.dump(writer));
            --writer.Indent;
            writer.WriteLine($"}} // line {endLine}, offset 0x{endOffset:x}");
        }
    }
}
