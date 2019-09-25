using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using symdump.symfile.util;
using symdump.util;

namespace symdump.symfile
{
    public class Block
    {
        private readonly uint _endLine;

        private readonly List<Label> _labels = new List<Label>();
        private readonly uint _startLine;

        private readonly List<Block> _subBlocks = new List<Block>();
        private readonly Dictionary<string, TaggedSymbol> _typedefs = new Dictionary<string, TaggedSymbol>();
        private readonly List<string> _vars = new List<string>();
        public readonly uint EndOffset;
        public readonly uint StartOffset;

        public Block(BinaryReader reader, uint startOffset, uint startLine, ObjectFile objectFile)
        {
            StartOffset = startOffset;
            _startLine = startLine;

            while (true)
            {
                var typedValue = new TypedValue(reader);

                if (reader.SkipSld(typedValue))
                    continue;

                // FIXME: fake type resolution
                switch (typedValue.Type & 0x7f)
                {
                    case TypedValue.Block:
                        _subBlocks.Add(new Block(reader, (uint) typedValue.Value, reader.ReadUInt32(),
                            objectFile));
                        break;
                    case TypedValue.BlockEnd:
                        EndOffset = (uint) typedValue.Value;
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

        public IEnumerable<Block> FindBlocksStartingAt(uint addr)
        {
            if (StartOffset == addr)
                yield return this;

            foreach (var block in _subBlocks.SelectMany(_ => _.FindBlocksStartingAt(addr))) yield return block;
        }

        public IEnumerable<Block> FindBlocksEndingAt(uint addr)
        {
            if (EndOffset == addr)
                yield return this;

            foreach (var block in _subBlocks.SelectMany(_ => _.FindBlocksEndingAt(addr))) yield return block;
        }

        public IEnumerable<Block> AllBlocks()
        {
            yield return this;
            foreach (var block in _subBlocks.SelectMany(_ => _.AllBlocks())) yield return block;
        }

        public void Dump(IndentedTextWriter writer)
        {
            DumpStart(writer);
            ++writer.Indent;
            _subBlocks.ForEach(b => b.Dump(writer));
            --writer.Indent;
            DumpEnd(writer);
        }

        public void DumpEnd(IndentedTextWriter writer)
        {
            writer.WriteLine($"}} // line {_endLine}, offset 0x{EndOffset:x}");
        }

        public void DumpStart(IndentedTextWriter writer)
        {
            writer.WriteLine($"{{ // line {_startLine}, offset 0x{StartOffset:x}");
            ++writer.Indent;
            foreach (var t in _typedefs)
                writer.WriteLine($"typedef {t.Value.AsCode(t.Key)};");
            _vars.ForEach(writer.WriteLine);
            foreach (var l in _labels)
                writer.WriteLine(l);
            --writer.Indent;
        }
    }
}
