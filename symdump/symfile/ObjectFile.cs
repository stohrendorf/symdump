using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using symdump.symfile.util;
using symdump.util;

namespace symdump.symfile
{
    public class ObjectFile
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private readonly IList<ExternStatic> _externStatics = new List<ExternStatic>();
        private readonly ISet<string> _srcFiles = new SortedSet<string>();
        private readonly IDictionary<string, TaggedSymbol> _typedefs = new Dictionary<string, TaggedSymbol>();
        public readonly IDictionary<string, IComplexType> ComplexTypes = new Dictionary<string, IComplexType>();
        public readonly List<Function> Functions = new List<Function>();
        public readonly Dictionary<string, TaggedSymbol> FuncTypes = new Dictionary<string, TaggedSymbol>();
        public readonly Dictionary<uint, List<Label>> Labels = new Dictionary<uint, List<Label>>();
        private string _objFilename;
        private int? _overlayId;

        public ObjectFile(BinaryReader stream)
        {
            logger.Info(
                $"Reading object file debug information at source file offset 0x{stream.BaseStream.Position:x8}");

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

            while (stream.BaseStream.Position < stream.BaseStream.Length && ReadEntry(stream))
                if (_objFilename == null)
                    _objFilename = string.Empty;

            ResolveTypedefs();

            if (_objFilename == "")
                _objFilename = null;
        }

        private void ResolveTypedefs()
        {
            logger.Info("Resolving typedef typedefs");
            foreach (var typedef in _typedefs.Values) typedef.ResolveTypedef(this);

            logger.Info("Resolving extern/static typedefs");
            foreach (var externStatic in _externStatics) externStatic.ResolveTypedef(this);

            logger.Info("Resolving function typedefs");
            foreach (var complexType in ComplexTypes.Values.ToList()) complexType.ResolveTypedefs(this);
        }

        public void Dump(TextWriter output)
        {
            foreach (var k in ComplexTypes.Keys.Where(_ => _.IsFake()).ToList())
                ComplexTypes.Remove(k);

            var writer = new IndentedTextWriter(output);

            var srcFiles = _srcFiles.Count == 0 ? "<none>" : string.Join("+", _srcFiles);
            writer.WriteLine(
                $"Source file(s) {srcFiles}, object file {_objFilename ?? "<none>"} overlay id {_overlayId?.ToString() ?? "<none>"}");

            writer.WriteLine();
            writer.WriteLine($"// {ComplexTypes.Count} complex types");
            foreach (var e in ComplexTypes.Values)
                e.Dump(writer, false);

            writer.WriteLine();
            writer.WriteLine($"// {_typedefs.Count} typedefs");
            foreach (var (typename, typedef) in _typedefs)
                writer.WriteLine($"typedef {typedef.AsCode(typename)};");

            writer.WriteLine();
            writer.WriteLine($"// {Labels.Count} labels");
            foreach (var l2 in Labels.SelectMany(_ => _.Value))
                writer.WriteLine(l2);

            writer.WriteLine();
            writer.WriteLine($"// {_externStatics.Count} external declarations");
            foreach (var e in _externStatics)
                writer.WriteLine(e);

            writer.WriteLine();
            writer.WriteLine($"// {Functions.Count} functions");
            foreach (var f in Functions)
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
            logger.Info("Skipping source line information");
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
                        _srcFiles.Add(stream.ReadPascalString());
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

                if (!Labels.ContainsKey(lbl.Offset))
                    Labels.Add(lbl.Offset, new List<Label>());

                Labels[lbl.Offset].Add(lbl);
                return true;
            }

            switch (typedValue.Type & 0x7f)
            {
                case TypedValue.BeginSLD:
                    stream.BaseStream.Position = basePos;
                    SkipSLD(stream);
                    return true;
                case TypedValue.SetOverlay:
                    stream.BaseStream.Position = basePos;
                    return false;
                case TypedValue.EndSLDInfo:
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
                    if (!ReadTopLevelDef(stream, typedValue.Value)) return false;

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
            Functions.Add(new Function(stream, (uint) offset, this));
        }

        private void AddComplexType(IComplexType type)
        {
            if (!type.IsFake && ComplexTypes.TryGetValue(type.Name, out var already) && !type.Equals(already))
                throw new Exception($"Non-uniform definition of complex type {type.Name}");

            ComplexTypes[type.Name] = type;
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

            if (taggedSymbol.IsFake && ComplexTypes.ContainsKey(taggedSymbol.Tag))
                // found a typedef for a fake symbol, e.g. typedef struct .123fake {} FOO
                ComplexTypes[taggedSymbol.Tag].Typedefs.Add(name, taggedSymbol);

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
                ComplexTypes[already.Tag].Dump(writer, false);

                writer.Indent--;
                writer.WriteLine("This is the new definition:");
                writer.Indent++;
                writer.WriteLine(taggedSymbol.ToString());
                writer.WriteLine(taggedSymbol.AsCode(name));
                ComplexTypes[taggedSymbol.Tag].Dump(writer, false);

                writer.Indent--;

                throw new Exception($"Non-uniform definition of typedef {name}");
            }

            _typedefs.Add(name, taggedSymbol);
        }

        private bool ReadTopLevelDef(BinaryReader stream, int offset)
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
                case SymbolType.FileName:
                    switch (_objFilename)
                    {
                        case null:
                            _objFilename = name;
                            return true;
                        case "":
                            return false;
                        default:
                            if (_objFilename != name)
                                throw new Exception($"Mismatching object file names, start={_objFilename}, end={name}");

                            return false;
                    }
                default:
                    throw new Exception($"Failed to handle {nameof(TaggedSymbol)}");
            }

            return true;
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
            return Functions.FirstOrDefault(f => f.Address == addr);
        }

        public string ReverseTypedef(TaggedSymbol taggedSymbol, out int droppedDerived)
        {
            droppedDerived = 0;

            if (!ComplexTypes.TryGetValue(taggedSymbol.Tag, out var complexType))
                return null;

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

            for (var i = 0; i <= DerivedTypeDef.MaxDerivedTypes; ++i)
            {
                droppedDerived = i;
                var partialTypedefs = complexType.Typedefs
                    .Where(_ => _.Value.IsPartOf(taggedSymbol, i))
                    .ToList();
                if (partialTypedefs.Count >= 1)
                    return partialTypedefs[0].Key;
            }

            droppedDerived = 0;
            return null;
        }
    }
}
