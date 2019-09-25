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
        private readonly List<Block> _blocks = new List<Block>();
        private readonly string _file;
        private readonly uint _lastLine;
        private readonly uint _line;
        private readonly uint _mask;
        private readonly int _maskOffs;
        private readonly string _name;

        private readonly List<string> _parameters = new List<string>();
        private readonly Register _register;
        private readonly string _returnType;
        private readonly Register _stackBase;
        private readonly uint _stackFrameSize;
        public readonly uint Address;

        public Function(BinaryReader reader, uint ofs, ObjectFile objectFile)
        {
            Address = ofs;

            _stackBase = (Register) reader.ReadUInt16();
            _stackFrameSize = reader.ReadUInt32();
            _register = (Register) reader.ReadUInt16();
            _mask = reader.ReadUInt32();
            _maskOffs = reader.ReadInt32();

            _line = reader.ReadUInt32();
            _file = reader.ReadPascalString();
            _name = reader.ReadPascalString();

            if (!objectFile.FuncTypes.TryGetValue(_name, out var fnType))
            {
                _returnType = "__UNKNOWN__";
            }
            else
            {
                fnType.ResolveTypedef(objectFile);
                _returnType = fnType.AsCode("").Trim();
                if (!_returnType.EndsWith("()"))
                    throw new Exception($"Expected function return type to end with '()', got {_returnType}");

                _returnType = _returnType.Substring(0, _returnType.Length - 2).Trim();
            }

            while (true)
            {
                var typedValue = new TypedValue(reader);

                if (reader.SkipSld(typedValue))
                    continue;

                TaggedSymbol taggedSymbol;
                string symbolName;
                switch (typedValue.Type & 0x7f)
                {
                    case TypedValue.FunctionEnd:
                        _lastLine = reader.ReadUInt32();
                        return;
                    case TypedValue.Block:
                        _blocks.Add(new Block(reader, (uint) typedValue.Value, reader.ReadUInt32(), this, objectFile));
                        continue;
                    case TypedValue.Definition:
                        taggedSymbol = reader.ReadTaggedSymbol(false);
                        symbolName = reader.ReadPascalString();
                        break;
                    case TypedValue.ArrayDefinition:
                        taggedSymbol = reader.ReadTaggedSymbol(true);
                        symbolName = reader.ReadPascalString();
                        break;
                    default:
                        throw new Exception($"Unexpected function definition type {typedValue.Type}");
                }

                if (taggedSymbol == null || symbolName == null)
                    break;

                taggedSymbol.ResolveTypedef(objectFile);

                switch (taggedSymbol.Type)
                {
                    case SymbolType.AutoVar:
                    case SymbolType.Argument:
                        _parameters.Add($"{taggedSymbol.AsCode(symbolName)} /*stack {typedValue.Value}*/");
                        break;
                    case SymbolType.RegParam:
                    case SymbolType.Register:
                        _parameters.Add($"{taggedSymbol.AsCode(symbolName)} /*${(Register) typedValue.Value}*/");
                        break;
                    default:
                        throw new Exception($"Unexpected parameter type {taggedSymbol.Type}");
                }
            }
        }

        private IEnumerable<Register> SavedRegisters => Enumerable.Range(0, 32)
            .Where(i => ((1 << i) & _mask) != 0)
            .Select(i => (Register) i);

        public void Dump(IndentedTextWriter writer)
        {
            writer.WriteLine("/*");
            writer.WriteLine($" * Offset 0x{Address:X}, from {_file} (lines {_line}..{_lastLine})");
            writer.WriteLine($" * Stack frame base ${_stackBase}, size {_stackFrameSize}");
            if (_mask != 0)
                writer.WriteLine($" * Saved registers at offset {_maskOffs}: {string.Join(" ", SavedRegisters)}");
            writer.WriteLine(" */");

            writer.WriteLine(GetSignature());

            _blocks.ForEach(b => b.Dump(writer));

            if (_blocks.Count != 0)
                return;

            writer.WriteLine("{");
            writer.WriteLine("}");
        }

        public string GetSignature()
        {
            return $"{_returnType} /*${_register}*/ {_name}({string.Join(", ", _parameters)})";
        }
    }
}
