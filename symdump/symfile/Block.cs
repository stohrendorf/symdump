using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using symdump.symfile.util;
using symdump.util;

namespace symdump.symfile
{
    public class Block
    {
        private readonly uint _endLine;
        private readonly uint _endOffset;

        private readonly Function _function;
        private readonly List<Label> _labels = new List<Label>();
        private readonly uint _startLine;

        private readonly uint _startOffset;
        private readonly List<Block> _subBlocks = new List<Block>();
        private readonly Dictionary<string, TypeInfo> _typedefs = new Dictionary<string, TypeInfo>();
        private readonly List<string> _vars = new List<string>();

        public Block(BinaryReader reader, uint ofs, uint ln, Function f)
        {
            _startOffset = ofs;
            _startLine = ln;
            _function = f;

            while (true)
            {
                var typedValue = new TypedValue(reader);

                if (reader.SkipSld(typedValue))
                    continue;

                switch (typedValue.Type & 0x7f)
                {
                    case 16:
                        _subBlocks.Add(new Block(reader, (uint) typedValue.Value, reader.ReadUInt32(), _function));
                        break;
                    case 18:
                        _endOffset = (uint) typedValue.Value;
                        _endLine = reader.ReadUInt32();
                        return;
                    case 20:
                    {
                        var ti = reader.ReadTypeInfo(false);
                        var memberName = reader.ReadPascalString();
                        Debug.Assert(!string.IsNullOrEmpty(memberName));
                        switch (ti.ClassType)
                        {
                            case ClassType.AutoVar:
                                _vars.Add($"{ti.AsCode(memberName)}; // stack offset {typedValue.Value}");
                                break;
                            case ClassType.Register:
                                _vars.Add($"{ti.AsCode(memberName)}; // ${(Register) typedValue.Value}");
                                break;
                            case ClassType.Static:
                                _vars.Add($"static {ti.AsCode(memberName)}; // offset 0x{typedValue.Value:x}");
                                break;
                            case ClassType.Typedef:
                                _typedefs.Add(memberName, ti);
                                break;
                            case ClassType.Label:
                                _labels.Add(new Label(typedValue, memberName));
                                break;
                            default:
                                throw new Exception($"Unexpected class type {ti.ClassType}");
                        }

                        break;
                    }
                    case 22:
                    {
                        var ti = reader.ReadTypeInfo(true);
                        var memberName = reader.ReadPascalString();
                        Debug.Assert(!string.IsNullOrEmpty(memberName));
                        switch (ti.ClassType)
                        {
                            case ClassType.AutoVar:
                                _vars.Add($"{ti.AsCode(memberName)}; // stack offset {typedValue.Value}");
                                break;
                            case ClassType.Register:
                                _vars.Add($"{ti.AsCode(memberName)}; // ${(Register) typedValue.Value}");
                                break;
                            case ClassType.Static:
                                _vars.Add($"static {ti.AsCode(memberName)}; // offset 0x{typedValue.Value:x}");
                                break;
                            case ClassType.Typedef:
                                _typedefs.Add(memberName, ti);
                                break;
                            case ClassType.Label:
                                _labels.Add(new Label(typedValue, memberName));
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
            writer.WriteLine($"{{ // line {_startLine}, offset 0x{_startOffset:x}");
            ++writer.Indent;
            foreach (var t in _typedefs)
                writer.WriteLine($"typedef {t.Value.AsCode(t.Key)};");
            _vars.ForEach(writer.WriteLine);
            foreach (var l in _labels)
                writer.WriteLine(l);
            _subBlocks.ForEach(b => b.Dump(writer));
            --writer.Indent;
            writer.WriteLine($"}} // line {_endLine}, offset 0x{_endOffset:x}");
        }
    }
}
