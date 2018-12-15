using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using core;
using core.util;
using NLog;
using symfile.memory;
using symfile.type;
using symfile.util;
using Function = symfile.code.Function;

namespace symfile
{
    public class SymFile : IDebugSource
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<string, EnumDef> _enums = new Dictionary<string, EnumDef>();
        private readonly Dictionary<string, TypeDecoration> _externs = new Dictionary<string, TypeDecoration>();
        private readonly Dictionary<string, StructLayout> _structs = new Dictionary<string, StructLayout>();
        private readonly byte _targetUnit;
        private readonly Dictionary<string, TypeDecoration> _typedefs = new Dictionary<string, TypeDecoration>();
        private readonly Dictionary<string, UnionLayout> _unions = new Dictionary<string, UnionLayout>();
        private readonly byte _version;
        public readonly Dictionary<string, CompoundLayout> CurrentlyDefining = new Dictionary<string, CompoundLayout>();
        public readonly Dictionary<string, TypeDecoration> FuncTypes = new Dictionary<string, TypeDecoration>();

        // ReSharper disable once NotAccessedField.Local
        private string _mxInfo;

        public SymFile(BinaryReader reader)
        {
            logger.Info("Loading SYM file");

            reader.BaseStream.Seek(0, SeekOrigin.Begin);

            reader.Skip(3);
            _version = reader.ReadByte();
            _targetUnit = reader.ReadByte();

            reader.Skip(3);
            uint n = 0;
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                LoadEntry(reader);
                ++n;
            }

            logger.Info($"Loaded {n} top-level entries");
        }

        public IList<IFunction> Functions { get; } = new List<IFunction>();

        public SortedDictionary<uint, IList<NamedLocation>> Labels { get; } =
            new SortedDictionary<uint, IList<NamedLocation>>();

        public IMemoryLayout FindTypeDefinition(string tag)
        {
            IMemoryLayout def = FindStructDef(tag);
            def = def ?? FindUnionDef(tag);
            def = def ?? CurrentlyDefining.FirstOrDefault(kv => kv.Key == tag).Value;
            return def;
        }

        public IMemoryLayout FindTypeDefinitionForLabel(string label)
        {
            if (string.IsNullOrEmpty(label))
                return null;

            return !_externs.TryGetValue(label, out var ti) ? null : FindTypeDefinition(ti.Tag);
        }

        public IFunction FindFunction(uint globalAddress)
        {
            return Functions.FirstOrDefault(f => f.GlobalAddress == globalAddress);
        }

        public IFunction FindFunction(string name)
        {
            return Functions.FirstOrDefault(f => f.Name.Equals(name));
        }

        public string GetSymbolName(uint absoluteAddress)
        {
            // first try to find a memory layout which contains this address
            var typedLabel = Labels.LastOrDefault(kv => kv.Key <= absoluteAddress).Value.First();
            var memoryLayout = FindTypeDefinitionForLabel(typedLabel.Name);
            if (memoryLayout == null)
                return !Labels.TryGetValue(absoluteAddress, out var lbls)
                    ? $"lbl_{absoluteAddress:X}"
                    : lbls.First().Name;

            try
            {
                var path = memoryLayout.GetAccessPathTo(absoluteAddress - typedLabel.GlobalAddress);
                if (path != null)
                    return typedLabel.Name + "." + path;
            }
            catch
            {
                // ignored
            }

            return !Labels.TryGetValue(absoluteAddress, out var lbls2)
                ? $"lbl_{absoluteAddress:X}"
                : lbls2.First().Name;
        }

        private StructLayout FindStructDef(string tag)
        {
            if (tag == null)
                return null;

            return !_structs.TryGetValue(tag, out var result) ? null : result;
        }

        private UnionLayout FindUnionDef(string tag)
        {
            if (tag == null)
                return null;

            return !_unions.TryGetValue(tag, out var result) ? null : result;
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
                writer.WriteLine($"typedef {t.Value.AsDeclaration(t.Key)};");

            writer.WriteLine();
            writer.WriteLine($"// {Labels.Count} labels");
            foreach (var l in Labels)
            foreach (var l2 in l.Value)
                writer.WriteLine(l2);

            writer.WriteLine();
            writer.WriteLine($"// {_externs.Count} external declarations");
            foreach (var e in _externs)
                writer.WriteLine(e.Value.AsDeclaration(e.Key));

            writer.WriteLine();
            writer.WriteLine($"// {Functions.Count} functions");
            foreach (var f in Functions)
                f.Dump(writer);
        }

        private void LoadEntry(BinaryReader reader)
        {
            var fileEntry = new FileEntry(reader);
            if (fileEntry.Type == 8)
            {
                _mxInfo = $"${fileEntry.Value:X} MX-info {reader.ReadByte():X}";
                return;
            }

            if (fileEntry.IsLabel)
            {
                var lbl = new NamedLocation((uint) fileEntry.Value, reader.ReadPascalString());

                if (!Labels.ContainsKey(lbl.GlobalAddress))
                    Labels.Add(lbl.GlobalAddress, new List<NamedLocation>());

                Labels[lbl.GlobalAddress].Add(lbl);
                return;
            }

            switch (fileEntry.Type & 0x7f)
            {
                case 0:
#if WITH_SLD
                writer.WriteLine($"${typedValue.value:X} Inc SLD linenum");
#endif
                    break;
                case 2:
#if WITH_SLD
                writer.WriteLine($"${typedValue.value:X} Inc SLD linenum by byte {stream.ReadU1()}");
#else
                    reader.Skip(1);
#endif
                    break;
                case 4:
#if WITH_SLD
                writer.WriteLine($"${typedValue.value:X} Inc SLD linenum by word {stream.ReadUInt16()}");
#else
                    reader.Skip(2);
#endif
                    break;
                case 6:
#if WITH_SLD
                writer.WriteLine($"${typedValue.value:X} Set SLD linenum to {stream.ReadUInt32()}");
#else
                    reader.Skip(4);
#endif
                    break;
                case 8:
#if WITH_SLD
                writer.WriteLine($"${typedValue.value:X} Set SLD to line {stream.ReadUInt32()} of file " +
                    stream.readPascalString());
#else
                    reader.Skip(4);
                    reader.Skip(reader.ReadByte());
#endif
                    break;
                case 10:
#if WITH_SLD
                writer.WriteLine($"${typedValue.value:X} End SLD info");
#endif
                    break;
                case 12:
                    LoadFunction(reader, fileEntry.Value);
                    break;
                case 20:
                    LoadUserDefinedType(reader, false);
                    break;
                case 22:
                    LoadUserDefinedType(reader, true);
                    break;
                default:
                    throw new Exception("Sodom");
            }
        }

        private void LoadFunction(BinaryReader reader, int offset)
        {
            Functions.Add(new Function(reader, (uint) offset, this));
            //writer.WriteLine("{");
            //++writer.Indent;
        }

        private void ReadEnum(BinaryReader reader, string name)
        {
            var e = new EnumDef(reader, name, this);

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
            var e = new UnionLayout(reader, name, this);

            if (_unions.TryGetValue(name, out var already))
            {
                if (e.Equals(already))
                    return;

                if (!e.IsAnonymous)
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
            var e = new StructLayout(reader, name, this);

            if (_structs.TryGetValue(name, out var already))
            {
                if (e.Equals(already))
                    return;

                if (!e.IsAnonymous) logger.Warn($"WARNING: Non-uniform definitions of struct {name}");

                // generate new "fake fake" name
                var n = 0;
                while (_structs.ContainsKey($"{name}.{n}"))
                    ++n;

                _structs.Add($"{name}.{n}", e);

                return;
            }

            _structs.Add(name, e);
        }

        private void AddTypedef(string name, TypeDecoration typeDecoration)
        {
            if (_typedefs.TryGetValue(name, out var already))
            {
                if (!typeDecoration.Equals(already))
                    throw new Exception($"Non-uniform definitions of typedef for {name}");

                return;
            }

            _typedefs.Add(name, typeDecoration);
        }

        private void LoadUserDefinedType(BinaryReader stream, bool withDimensions)
        {
            var ti = stream.ReadTypeDecoration(withDimensions, this);
            var name = stream.ReadPascalString();

            switch (ti.ClassType)
            {
                case ClassType.FileName:
                    return;
                case ClassType.Enum when ti.BaseType == BaseType.EnumDef:
                    ReadEnum(stream, name);
                    break;
                case ClassType.Struct when ti.BaseType == BaseType.StructDef:
                    ReadStruct(stream, name);
                    break;
                case ClassType.Union when ti.BaseType == BaseType.UnionDef:
                    ReadUnion(stream, name);
                    break;
                case ClassType.Typedef:
                    AddTypedef(name, ti);
                    break;
                case ClassType.External:
                    if (ti.IsFunctionReturnType)
                        FuncTypes.Add(name, ti);
                    else
                        _externs.Add(name, ti);
                    break;
                case ClassType.Static:
                    if (ti.IsFunctionReturnType)
                        FuncTypes.Add(name, ti);
                    else
                        _externs.Add(name, ti);
                    break;
                default:
                    throw new Exception("Gomorrha");
            }
        }
    }
}