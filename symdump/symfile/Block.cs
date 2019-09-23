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
        private readonly Dictionary<string, TaggedSymbol> _typedefs = new Dictionary<string, TaggedSymbol>();
        private readonly List<string> _vars = new List<string>();

        public Block(BinaryReader reader, uint ofs, uint ln, Function f, ObjectFile objectFile)
        {
            _startOffset = ofs;
            _startLine = ln;
            _function = f;

            while (true)
            {
                var typedValue = new TypedValue(reader);

                if (reader.SkipSld(typedValue))
                    continue;

                // FIXME: fake type resolution
                switch (typedValue.Type & 0x7f)
                {
                    case TypedValue.Block:
                        _subBlocks.Add(new Block(reader, (uint) typedValue.Value, reader.ReadUInt32(), _function,
                            objectFile));
                        break;
                    case TypedValue.BlockEnd:
                        _endOffset = (uint) typedValue.Value;
                        _endLine = reader.ReadUInt32();
                        return;
                    case TypedValue.Definition:
                    {
                        var taggedSymbol = reader.ReadTaggedSymbol(false);
                        var memberName = reader.ReadPascalString();
                        Debug.Assert(!string.IsNullOrEmpty(memberName));
                        taggedSymbol.ResolveTypedef(objectFile);
                        switch (taggedSymbol.Type)
                        {
                            case SymbolType.AutoVar:
                                _vars.Add($"{taggedSymbol.AsCode(memberName)}; // stack offset {typedValue.Value}");
                                break;
                            case SymbolType.Register:
                                _vars.Add($"{taggedSymbol.AsCode(memberName)}; // ${(Register) typedValue.Value}");
                                break;
                            case SymbolType.Static:
                                _vars.Add(
                                    $"static {taggedSymbol.AsCode(memberName)}; // offset 0x{typedValue.Value:x}");
                                break;
                            case SymbolType.Typedef:
                                _typedefs.Add(memberName, taggedSymbol);
                                break;
                            case SymbolType.Label:
                                _labels.Add(new Label(typedValue, memberName));
                                break;
                            default:
                                throw new Exception($"Unexpected {nameof(TaggedSymbol)} {taggedSymbol.Type}");
                        }

                        break;
                    }

                    case TypedValue.ArrayDefinition:
                    {
                        var taggedSymbol = reader.ReadTaggedSymbol(true);
                        var memberName = reader.ReadPascalString();
                        Debug.Assert(!string.IsNullOrEmpty(memberName));
                        taggedSymbol.ResolveTypedef(objectFile);
                        switch (taggedSymbol.Type)
                        {
                            case SymbolType.AutoVar:
                                _vars.Add($"{taggedSymbol.AsCode(memberName)}; // stack offset {typedValue.Value}");
                                break;
                            case SymbolType.Register:
                                _vars.Add($"{taggedSymbol.AsCode(memberName)}; // ${(Register) typedValue.Value}");
                                break;
                            case SymbolType.Static:
                                _vars.Add(
                                    $"static {taggedSymbol.AsCode(memberName)}; // offset 0x{typedValue.Value:x}");
                                break;
                            case SymbolType.Typedef:
                                _typedefs.Add(memberName, taggedSymbol);
                                break;
                            case SymbolType.Label:
                                _labels.Add(new Label(typedValue, memberName));
                                break;
                            default:
                                throw new Exception($"Unexpected {nameof(TaggedSymbol)} {taggedSymbol.Type}");
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