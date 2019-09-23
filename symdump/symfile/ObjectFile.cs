using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using symdump.symfile.util;
using symdump.util;

namespace symdump.symfile
{
    public class ObjectFile
    {
        private readonly IDictionary<string, IComplexType> _complexTypes = new Dictionary<string, IComplexType>();
        private readonly IList<ExternStatic> _externStatics = new List<ExternStatic>();
        private readonly List<Function> _functions = new List<Function>();
        private readonly Dictionary<uint, List<Label>> _labels = new Dictionary<uint, List<Label>>();
        private readonly ISet<string> _srcFile = new SortedSet<string>();
        private readonly IDictionary<string, TaggedSymbol> _typedefs = new Dictionary<string, TaggedSymbol>();
        public readonly Dictionary<string, TaggedSymbol> FuncTypes = new Dictionary<string, TaggedSymbol>();
        private int? _overlayId;

        public ObjectFile(BinaryReader stream)
        {
            // For compiler information:
            //    a) (optional) set overlay
            //    b) SLD info with filename
            //    c) List of SLD entries
            //    d) SLD end entry
            //    e) definitions
            // For linker information:
            //    a) (optional) set overlay
            //    b) List of labels
            ReadOverlayId(stream);
            SkipSLD(stream);

            while (stream.BaseStream.Position < stream.BaseStream.Length && ReadEntry(stream))
            {
            }

            ResolveTypedefs();
        }

        private void ResolveTypedefs()
        {
            foreach (var typedef in _typedefs.Values) typedef.ResolveTypedef(this);

            foreach (var externStatic in _externStatics) externStatic.ResolveTypedef(this);

            foreach (var complexType in _complexTypes.Values) complexType.ResolveTypedefs(this);
        }

        public void Dump(TextWriter output)
        {
            foreach (var k in _complexTypes.Keys.Where(_ => _.IsFake()).ToList())
                _complexTypes.Remove(k);

            var writer = new IndentedTextWriter(output);

            writer.WriteLine(
                $"Source files {string.Join("+", _srcFile)}, overlay id {_overlayId?.ToString() ?? "<none>"}");

            writer.WriteLine();
            writer.WriteLine($"// {_complexTypes.Count} complex types");
            foreach (var e in _complexTypes.Values)
                e.Dump(writer, false);

            writer.WriteLine();
            writer.WriteLine($"// {_typedefs.Count} typedefs");
            foreach (var (typename, typedef) in _typedefs)
                writer.WriteLine($"typedef {typedef.AsCode(typename)};");

            writer.WriteLine();
            writer.WriteLine($"// {_labels.Count} labels");
            foreach (var l2 in _labels.SelectMany(_ => _.Value))
                writer.WriteLine(l2);

            writer.WriteLine();
            writer.WriteLine($"// {_externStatics.Count} external declarations");
            foreach (var e in _externStatics)
                writer.WriteLine(e);

            writer.WriteLine();
            writer.WriteLine($"// {_functions.Count} functions");
            foreach (var f in _functions)
                f.Dump(writer);
        }

        private void ReadOverlayId(BinaryReader stream)
        {
            var pos = stream.BaseStream.Position;
            var typedValue = new TypedValue(stream);
            if (typedValue.Type == (0x80 | TypedValue.SetOverlay))
                _overlayId = typedValue.Value;
            else
                stream.BaseStream.Position = pos;
        }

        private void SkipSLD(BinaryReader stream)
        {
            while (stream.BaseStream.Position < stream.BaseStream.Length)
            {
                var basePos = stream.BaseStream.Position;
                var typedValue = new TypedValue(stream);
                if (typedValue.Type == 8)
                    throw new Exception("Unexpected MX-Info");

                if (typedValue.IsLabel)
                {
                    // linker information: overlay followed by label list
                    stream.BaseStream.Position = basePos;
                    return;
                }

                switch (typedValue.Type & 0x7f)
                {
                    case TypedValue.BeginSLD:
                        stream.Skip(4);
                        _srcFile.Add(stream.ReadPascalString());
                        break;
                    case TypedValue.IncSLD:
                        break;
                    case TypedValue.AddSLD1:
                        stream.Skip(1);
                        break;
                    case TypedValue.AddSLD2:
                        stream.Skip(2);
                        break;
                    case TypedValue.SetSLD:
                        stream.Skip(4);
                        break;
                    case TypedValue.EndSLDInfo:
                        return;
                    default:
                        throw new Exception($"Unhandled SLD type 0x{typedValue.Type:X}");
                }
            }

            throw new Exception("SLD information without end marker");
        }

        private bool ReadEntry(BinaryReader stream)
        {
            var basePos = stream.BaseStream.Position;
            var typedValue = new TypedValue(stream);
            if (typedValue.Type == 8)
                throw new Exception("Unexpected MX-Info");

            if (typedValue.IsLabel)
            {
                var lbl = new Label(typedValue, stream);

                if (!_labels.ContainsKey(lbl.Offset))
                    _labels.Add(lbl.Offset, new List<Label>());

                _labels[lbl.Offset].Add(lbl);
                return true;
            }

            switch (typedValue.Type & 0x7f)
            {
                case TypedValue.SetOverlay:
                case TypedValue.BeginSLD:
                case TypedValue.EndSLDInfo:
                    stream.BaseStream.Position = basePos;
                    return false;
                case TypedValue.IncSLD:
                case TypedValue.AddSLD1:
                case TypedValue.AddSLD2:
                case TypedValue.SetSLD:
                    throw new Exception($"Unexpected SLD entry 0x{typedValue.Type & 0x7f:X}");
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
                    throw new Exception("Unexpected overlay definition");
                default:
                    throw new Exception($"Unhandled debug type 0x{typedValue.Type:X}");
            }

            return true;
        }

        private void ReadFunction(BinaryReader stream, int offset)
        {
            _functions.Add(new Function(stream, (uint) offset, this));
        }

        private void AddComplexType(IComplexType type)
        {
            if (!type.IsFake && _complexTypes.TryGetValue(type.Name, out var already) && !type.Equals(already))
                throw new Exception($"Non-uniform definition of complex type {type.Name}");

            _complexTypes[type.Name] = type;
        }

        private void ReadEnum(BinaryReader reader, string name)
        {
            AddComplexType(new EnumDef(reader, name));
        }

        private void ReadUnion(BinaryReader reader, string name)
        {
            AddComplexType(new UnionDef(reader, name));
        }

        private void ReadStruct(BinaryReader reader, string name)
        {
            AddComplexType(new StructDef(reader, name));
        }

        private void AddTypedef(string name, TaggedSymbol taggedSymbol)
        {
            if (name.IsFake())
                throw new Exception($"Found fake typedef name {name}");

            if (taggedSymbol.IsFake && _complexTypes.ContainsKey(taggedSymbol.Tag))
                // found a typedef for a fake symbol, e.g. typedef struct .123fake {} FOO
                _complexTypes[taggedSymbol.Tag].Typedefs.Add(name, taggedSymbol);

            if (_typedefs.TryGetValue(name, out var already))
            {
                if (taggedSymbol.Equals(already))
                    return;

                var writer = new IndentedTextWriter(Console.Out);
                writer.WriteLine($"WARNING: Non-uniform definitions of typedef {name}");
                writer.WriteLine("This is the definition already present:");
                writer.Indent++;
                writer.WriteLine(already.ToString());
                writer.WriteLine(already.AsCode(name));
                _complexTypes[already.Tag].Dump(writer, false);

                writer.Indent--;
                writer.WriteLine("This is the new definition:");
                writer.Indent++;
                writer.WriteLine(taggedSymbol.ToString());
                writer.WriteLine(taggedSymbol.AsCode(name));
                _complexTypes[taggedSymbol.Tag].Dump(writer, false);

                writer.Indent--;

                throw new Exception($"Non-uniform definition of typedef {name}");
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
                    break;
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
                case SymbolType.Static when taggedSymbol.DerivedTypeDef.IsFunctionReturnType:
                    FuncTypes[name] = taggedSymbol;
                    break;
                case SymbolType.External:
                case SymbolType.Static:
                    _externStatics.Add(new ExternStatic(taggedSymbol, name, offset));
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
                case SymbolType.Static when taggedSymbol.DerivedTypeDef.IsFunctionReturnType:
                    FuncTypes[name] = taggedSymbol;
                    break;
                case SymbolType.External:
                case SymbolType.Static:
                    _externStatics.Add(new ExternStatic(taggedSymbol, name, offset));
                    break;
                default:
                    throw new Exception($"Failed to handle {nameof(TaggedSymbol)}");
            }
        }

        public Function FindFunction(uint addr)
        {
            return _functions.FirstOrDefault(f => f.Address == addr);
        }

        public string ReverseTypedef(TaggedSymbol taggedSymbol, out int droppedDerived)
        {
            droppedDerived = 0;

            var complexType = _complexTypes[taggedSymbol.Tag];
            var typedefs = complexType.Typedefs
                .Where(_ => _.Value.Equals(taggedSymbol))
                .ToList();
            if (typedefs.Count >= 1)
                return typedefs[0].Key;

            /*
             * Now the fun part... resolving
             *   typedef struct .fake123 {} FOO;
             *   struct .fake123* foo;
             * to
             *   FOO* foo;
             */

            for (var i = 1; i <= DerivedTypeDef.MaxDerivedTypes; ++i)
            {
                droppedDerived = i;
                var partialTypedefs = complexType.Typedefs
                    .Where(_ => _.Value.IsPartOf(taggedSymbol, i))
                    .ToList();
                if (partialTypedefs.Count >= 1)
                    return partialTypedefs[0].Key;
            }

            return null;
        }
    }
}
