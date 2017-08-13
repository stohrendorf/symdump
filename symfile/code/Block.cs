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
                        return $"{TypeDecoration.AsDeclaration(Name)}; /* sp {FileEntry.Value} */";
                    case ClassType.Register:
                        return $"{TypeDecoration.AsDeclaration(Name)}; /* ${(Register) FileEntry.Value} */";
                    case ClassType.Static:
                        return $"static {TypeDecoration.AsDeclaration(Name)}; // offset 0x{FileEntry.Value:x}";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public readonly uint EndLine;
        public readonly uint EndOffset;

        public readonly Function Function;
        public readonly List<NamedLocation> Labels = new List<NamedLocation>();
        public readonly uint StartLine;

        public readonly uint StartOffset;
        public readonly List<Block> SubBlocks = new List<Block>();
        public readonly Dictionary<string, TypeDecoration> Typedefs = new Dictionary<string, TypeDecoration>();
        public readonly Dictionary<string, VarInfo> Vars = new Dictionary<string, VarInfo>();

        public Block(uint ofs, uint ln, Function f, IDebugSource debugSource)
            : this(null, ofs, ln, f, debugSource)
        {
        }

        public Block(BinaryReader reader, uint ofs, uint ln, Function f, IDebugSource debugSource)
        {
            StartOffset = ofs;
            StartLine = ln;
            Function = f;

            if (reader == null)
                return;

            while (true)
            {
                var typedValue = new FileEntry(reader);

                if (reader.SkipSld(typedValue))
                    continue;

                switch (typedValue.Type & 0x7f)
                {
                    case 16:
                        SubBlocks.Add(new Block(reader, (uint) typedValue.Value, reader.ReadUInt32(), Function, debugSource));
                        break;
                    case 18:
                        EndOffset = (uint) typedValue.Value;
                        EndLine = reader.ReadUInt32();
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
                                Vars.Add(memberName, new VarInfo(memberName, ti, typedValue));
                                break;
                            case ClassType.Typedef:
                                Typedefs.Add(memberName, ti);
                                break;
                            case ClassType.Label:
                                Labels.Add(new NamedLocation((uint) typedValue.Value, memberName));
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
                                Vars.Add(memberName, new VarInfo(memberName, ti, typedValue));
                                break;
                            case ClassType.Typedef:
                                Typedefs.Add(memberName, ti);
                                break;
                            case ClassType.Label:
                                Labels.Add(new NamedLocation((uint) typedValue.Value, memberName));
                                break;
                            default:
                                throw new Exception($"Unexpected class type {ti.ClassType}");
                        }
                        break;
                    }
                }
            }
        }

        public void Dump(IndentedTextWriter writer)
        {
            writer.WriteLine($"{{ // line {StartLine}, offset 0x{StartOffset:x}");
            ++writer.Indent;
            foreach (var t in Typedefs)
                writer.WriteLine($"typedef {t.Value.AsDeclaration(t.Key)};");
            foreach (var varInfo in Vars)
                writer.WriteLine(varInfo.Value);
            foreach (var l in Labels)
                writer.WriteLine(l);
            SubBlocks.ForEach(b => b.Dump(writer));
            --writer.Indent;
            writer.WriteLine($"}} // line {EndLine}, offset 0x{EndOffset:x}");
        }
    }
}
