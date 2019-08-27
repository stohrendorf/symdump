using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using symdump.symfile.util;
using symdump.util;

namespace symdump.symfile
{
    public class SymFile
    {
        private readonly Dictionary<string, EnumDef> _enums = new Dictionary<string, EnumDef>();
        private readonly SortedSet<string> _externs = new SortedSet<string>();
        private readonly Dictionary<string, string> _funcTypes = new Dictionary<string, string>();
        private readonly SortedSet<string> _overlays = new SortedSet<string>();
        private readonly SortedSet<string> _setOverlays = new SortedSet<string>();
        private readonly Dictionary<string, StructDef> _structs = new Dictionary<string, StructDef>();
        private readonly byte _targetUnit;
        private readonly Dictionary<string, TaggedSymbol> _typedefs = new Dictionary<string, TaggedSymbol>();
        private readonly Dictionary<string, UnionDef> _unions = new Dictionary<string, UnionDef>();
        private readonly byte _version;
        public readonly List<Function> Functions = new List<Function>();
        internal readonly Dictionary<uint, List<Label>> Labels = new Dictionary<uint, List<Label>>();
        private string _mxInfo;

        public SymFile(BinaryReader stream)
        {
            stream.BaseStream.Seek(0, SeekOrigin.Begin);

            stream.Skip(3);
            _version = stream.ReadByte();
            _targetUnit = stream.ReadByte();

            stream.Skip(3);
            while (stream.BaseStream.Position < stream.BaseStream.Length)
                DumpEntry(stream);
        }

        public void Dump(TextWriter output)
        {
            var writer = new IndentedTextWriter(output);
            writer.WriteLine($"Version = {_version}, targetUnit = {_targetUnit}");

            writer.WriteLine();
            writer.WriteLine($"// {_enums.Count} enums");
            foreach (var e in _enums.Values)
                e.Dump(writer);

            writer.WriteLine();
            writer.WriteLine($"// {_unions.Count} unions");
            foreach (var e in _unions.Values)
                e.Dump(writer);

            writer.WriteLine();
            writer.WriteLine($"// {_structs.Count} structs");
            foreach (var e in _structs.Values)
                e.Dump(writer);

            writer.WriteLine();
            writer.WriteLine($"// {_typedefs.Count} typedefs");
            foreach (var t in _typedefs)
                writer.WriteLine($"typedef {t.Value.AsCode(t.Key)};");

            writer.WriteLine();
            writer.WriteLine($"// {Labels.Count} labels");
            foreach (var l in Labels)
            foreach (var l2 in l.Value)
                writer.WriteLine(l2);

            writer.WriteLine();
            writer.WriteLine($"// {_externs.Count} external declarations");
            foreach (var e in _externs)
                writer.WriteLine(e);

            writer.WriteLine();
            writer.WriteLine($"// {Functions.Count} functions");
            foreach (var f in Functions)
                f.Dump(writer);

            writer.WriteLine();
            writer.WriteLine($"// {_overlays.Count} overlays");
            foreach (var o in _overlays)
                writer.WriteLine(o);

            writer.WriteLine();
            writer.WriteLine($"// {_setOverlays.Count} set overlays");
            foreach (var o in _setOverlays)
                writer.WriteLine(o);
        }

        private void DumpEntry(BinaryReader stream)
        {
            var typedValue = new TypedValue(stream);
            if (typedValue.Type == 8)
            {
                _mxInfo = $"${typedValue.Value:X} MX-info {stream.ReadByte():X}";
                return;
            }

            if (typedValue.IsLabel)
            {
                var lbl = new Label(typedValue, stream);

                if (!Labels.ContainsKey(lbl.Offset))
                    Labels.Add(lbl.Offset, new List<Label>());

                Labels[lbl.Offset].Add(lbl);
                return;
            }

            switch (typedValue.Type & 0x7f)
            {
                case TypedValue.IncSLD:
#if WITH_SLD
                writer.WriteLine($"${typedValue.value:X} Inc SLD linenum");
#endif
                    break;
                case TypedValue.AddSLD1:
#if WITH_SLD
                writer.WriteLine($"${typedValue.value:X} Inc SLD linenum by byte {stream.ReadU1()}");
#else
                    stream.Skip(1);
#endif
                    break;
                case TypedValue.AddSLD2:
#if WITH_SLD
                writer.WriteLine($"${typedValue.value:X} Inc SLD linenum by word {stream.ReadUInt16()}");
#else
                    stream.Skip(2);
#endif
                    break;
                case TypedValue.SetSLD:
#if WITH_SLD
                writer.WriteLine($"${typedValue.value:X} Set SLD linenum to {stream.ReadUInt32()}");
#else
                    stream.Skip(4);
#endif
                    break;
                case TypedValue.SetSLDFile:
#if WITH_SLD
                writer.WriteLine($"${typedValue.value:X} Set SLD to line {stream.ReadUInt32()} of file " +
                    stream.readPascalString());
#else
                    stream.Skip(4);
                    stream.Skip(stream.ReadByte());
#endif
                    break;
                case TypedValue.EndSLDInfo:
#if WITH_SLD
                writer.WriteLine($"${typedValue.value:X} End SLD info");
#endif
                    break;
                case TypedValue.Function:
                    ReadFunction(stream, typedValue.Value);
                    break;
                case TypedValue.Definition:
                    ReadTopLevelDef(stream, typedValue.Value);
                    break;
                case TypedValue.ArrayDefinition:
                    ReadTopLevelArrayDef(stream, typedValue.Value);
                    break;
                case TypedValue.Overlay:
                    ReadOverlayDef(stream, typedValue.Value);
                    break;
                case TypedValue.SetOverlay:
                    ReadSetOverlay(typedValue.Value);
                    break;
                default:
                    throw new Exception($"Unhandled debug type 0x{typedValue.Type:X}");
            }
        }

        private void ReadFunction(BinaryReader stream, int offset)
        {
            Functions.Add(new Function(stream, (uint) offset, _funcTypes));
            //writer.WriteLine("{");
            //++writer.Indent;
        }

        private void ReadEnum(BinaryReader reader, string name)
        {
            var e = new EnumDef(reader, name);

            if (_enums.TryGetValue(name, out var already))
            {
                if (!e.Equals(already))
                    throw new Exception($"Non-uniform definitions of enum {name}");

                return;
            }

            _enums.Add(name, e);
        }

        private void ReadUnion(BinaryReader reader, string name)
        {
            var e = new UnionDef(reader, name);

            if (_unions.TryGetValue(name, out var already))
            {
                if (e.Equals(already))
                    return;

                if (!e.IsFake)
                    throw new Exception($"Non-uniform definitions of union {name}");

                // generate new "fake fake" name
                var n = 0;
                while (_unions.ContainsKey($"{name}.{n}"))
                    ++n;

                _unions.Add($"{name}.{n}", e);

                return;
            }

            _unions.Add(name, e);
        }

        private void ReadStruct(BinaryReader reader, string name)
        {
            var e = new StructDef(reader, name);

            if (_structs.TryGetValue(name, out var already))
            {
                if (e.Equals(already))
                    return;

                if (!e.IsFake)
                {
                    var writer = new IndentedTextWriter(Console.Out);
                    writer.WriteLine($"WARNING: Non-uniform definitions of struct {name}");
                    writer.WriteLine("This is the definition already present:");
                    writer.Indent++;
                    already.Dump(writer);
                    writer.Indent--;
                    writer.WriteLine("This is the new definition:");
                    writer.Indent++;
                    e.Dump(writer);
                    writer.Indent--;
                }

                // generate new "fake fake" name
                var n = 0;
                while (_structs.ContainsKey($"{name}.{n}"))
                    ++n;

                _structs.Add($"{name}.{n}", e);

                return;
            }

            _structs.Add(name, e);
        }

        private void AddTypedef(string name, TaggedSymbol taggedSymbol)
        {
            if (_typedefs.TryGetValue(name, out var already))
            {
                if (!taggedSymbol.Equals(already))
                    throw new Exception($"Non-uniform definitions of typedef for {name}");

                return;
            }

            _typedefs.Add(name, taggedSymbol);
        }

        private void ReadTopLevelDef(BinaryReader stream, int offset)
        {
            var taggedSymbol = stream.ReadTaggedSymbol(false);
            var name = stream.ReadPascalString();

            switch (taggedSymbol.Type)
            {
                case SymbolType.Enum when taggedSymbol.DerivedTypeDef.Type == PrimitiveType.EnumDef:
                    ReadEnum(stream, name);
                    return;
                case SymbolType.FileName:
                    return;
                case SymbolType.Struct when taggedSymbol.DerivedTypeDef.Type == PrimitiveType.StructDef:
                    ReadStruct(stream, name);
                    break;
                case SymbolType.Union when taggedSymbol.DerivedTypeDef.Type == PrimitiveType.UnionDef:
                    ReadUnion(stream, name);
                    break;
                case SymbolType.Typedef:
                    AddTypedef(name, taggedSymbol);
                    break;
                case SymbolType.External when taggedSymbol.DerivedTypeDef.IsFunctionReturnType:
                    _funcTypes[name] = taggedSymbol.AsCode("").Trim();
                    break;
                case SymbolType.External:
                    _externs.Add($"extern {taggedSymbol.AsCode(name)}; // offset 0x{offset:X}");
                    break;
                case SymbolType.Static when taggedSymbol.DerivedTypeDef.IsFunctionReturnType:
                    _funcTypes[name] = taggedSymbol.AsCode("").Trim();
                    break;
                case SymbolType.Static:
                    _externs.Add($"static {taggedSymbol.AsCode(name)}; // offset 0x{offset:X}");
                    break;
                default:
                    throw new Exception($"Failed to handle {nameof(TaggedSymbol)}");
            }
        }

        private void ReadTopLevelArrayDef(BinaryReader stream, int offset)
        {
            var taggedSymbol = stream.ReadTaggedSymbol(true);
            var name = stream.ReadPascalString();

            switch (taggedSymbol.Type)
            {
                case SymbolType.Enum when taggedSymbol.DerivedTypeDef.Type == PrimitiveType.EnumDef:
                    ReadEnum(stream, name);
                    break;
                case SymbolType.Typedef:
                    AddTypedef(name, taggedSymbol);
                    break;
                case SymbolType.External when taggedSymbol.DerivedTypeDef.IsFunctionReturnType:
                    _funcTypes[name] = taggedSymbol.AsCode("").Trim();
                    break;
                case SymbolType.External:
                    _externs.Add($"extern {taggedSymbol.AsCode(name)}; // offset 0x{offset:X}");
                    break;
                case SymbolType.Static when taggedSymbol.DerivedTypeDef.IsFunctionReturnType:
                    _funcTypes[name] = taggedSymbol.AsCode("").Trim();
                    break;
                case SymbolType.Static:
                    _externs.Add($"static {taggedSymbol.AsCode(name)}; // offset 0x{offset:X}");
                    break;
                default:
                    throw new Exception($"Failed to handle {nameof(TaggedSymbol)}");
            }
        }

        private void ReadOverlayDef(BinaryReader stream, int offset)
        {
            var overlay = new Overlay(stream);
            _overlays.Add($"Overlay {overlay} // offset 0x{offset:X}");
        }

        private void ReadSetOverlay(int offset)
        {
            _setOverlays.Add($"Set overlay 0x{offset:X}");
        }

        public Function FindFunction(uint addr)
        {
            return Functions.FirstOrDefault(f => f.Address == addr);
        }
    }
}
