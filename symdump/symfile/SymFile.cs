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
        public readonly List<Function> Functions = new List<Function>();
        private readonly Dictionary<string, string> _funcTypes = new Dictionary<string, string>();
        internal readonly Dictionary<uint, List<Label>> Labels = new Dictionary<uint, List<Label>>();
        private readonly Dictionary<string, StructDef> _structs = new Dictionary<string, StructDef>();
        private readonly byte _targetUnit;
        private readonly Dictionary<string, TypeInfo> _typedefs = new Dictionary<string, TypeInfo>();
        private readonly Dictionary<string, UnionDef> _unions = new Dictionary<string, UnionDef>();
        private readonly byte _version;
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
                case 0:
#if WITH_SLD
                writer.WriteLine($"${typedValue.value:X} Inc SLD linenum");
                #endif
                    break;
                case 2:
#if WITH_SLD
                writer.WriteLine($"${typedValue.value:X} Inc SLD linenum by byte {stream.ReadU1()}");
                #else
                    stream.Skip(1);
#endif
                    break;
                case 4:
#if WITH_SLD
                writer.WriteLine($"${typedValue.value:X} Inc SLD linenum by word {stream.ReadUInt16()}");
#else
                    stream.Skip(2);
#endif
                    break;
                case 6:
#if WITH_SLD
                writer.WriteLine($"${typedValue.value:X} Set SLD linenum to {stream.ReadUInt32()}");
#else
                    stream.Skip(4);
#endif
                    break;
                case 8:
#if WITH_SLD
                writer.WriteLine($"${typedValue.value:X} Set SLD to line {stream.ReadUInt32()} of file " +
                    stream.readPascalString());
#else
                    stream.Skip(4);
                    stream.Skip(stream.ReadByte());
#endif
                    break;
                case 10:
#if WITH_SLD
                writer.WriteLine($"${typedValue.value:X} End SLD info");
#endif
                    break;
                case 12:
                    DumpType12(stream, typedValue.Value);
                    break;
                case 20:
                    DumpType20(stream, typedValue.Value);
                    break;
                case 22:
                    DumpType22(stream, typedValue.Value);
                    break;
                default:
                    throw new Exception("Sodom");
            }
        }

        private void DumpType12(BinaryReader stream, int offset)
        {
            Functions.Add(new Function(stream, (uint) offset, _funcTypes));
            //writer.WriteLine("{");
            //++writer.Indent;
        }

        private void ReadEnum(BinaryReader reader, string name)
        {
            var e = new EnumDef(reader, name);

            EnumDef already;
            if (_enums.TryGetValue(name, out already))
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

            UnionDef already;
            if (_unions.TryGetValue(name, out already))
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

            StructDef already;
            if (_structs.TryGetValue(name, out already))
            {
                if (e.Equals(already))
                    return;

                if (!e.IsFake)
                    Console.WriteLine($"WARNING: Non-uniform definitions of struct {name}");

                // generate new "fake fake" name
                var n = 0;
                while (_structs.ContainsKey($"{name}.{n}"))
                    ++n;

                _structs.Add($"{name}.{n}", e);

                return;
            }

            _structs.Add(name, e);
        }

        private void AddTypedef(string name, TypeInfo typeInfo)
        {
            TypeInfo already;
            if (_typedefs.TryGetValue(name, out already))
            {
                if (!typeInfo.Equals(already))
                    throw new Exception($"Non-uniform definitions of typedef for {name}");

                return;
            }

            _typedefs.Add(name, typeInfo);
        }

        private void DumpType20(BinaryReader stream, int offset)
        {
            var ti = stream.ReadTypeInfo(false);
            var name = stream.ReadPascalString();

            if (ti.ClassType == ClassType.Enum && ti.TypeDef.BaseType == BaseType.EnumDef)
            {
                ReadEnum(stream, name);
                return;
            }

            if (ti.ClassType == ClassType.FileName)
                return;
            if (ti.ClassType == ClassType.Struct && ti.TypeDef.BaseType == BaseType.StructDef)
                ReadStruct(stream, name);
            else if (ti.ClassType == ClassType.Union && ti.TypeDef.BaseType == BaseType.UnionDef)
                ReadUnion(stream, name);
            else if (ti.ClassType == ClassType.Typedef)
                AddTypedef(name, ti);
            else if (ti.ClassType == ClassType.External)
                if (ti.TypeDef.IsFunctionReturnType)
                    _funcTypes[name] = ti.AsCode("").Trim();
                else
                    _externs.Add($"extern {ti.AsCode(name)}; // offset 0x{offset:X}");
            else if (ti.ClassType == ClassType.Static)
                if (ti.TypeDef.IsFunctionReturnType)
                    _funcTypes[name] = ti.AsCode("").Trim();
                else
                    _externs.Add($"static {ti.AsCode(name)}; // offset 0x{offset:X}");
            else
                throw new Exception("Gomorrha");
        }

        private void DumpType22(BinaryReader stream, int offset)
        {
            var ti = stream.ReadTypeInfo(true);
            var name = stream.ReadPascalString();

            if (ti.ClassType == ClassType.Enum && ti.TypeDef.BaseType == BaseType.EnumDef)
                ReadEnum(stream, name);
            else if (ti.ClassType == ClassType.Typedef)
                AddTypedef(name, ti);
            else if (ti.ClassType == ClassType.External)
                if (ti.TypeDef.IsFunctionReturnType)
                    _funcTypes[name] = ti.AsCode("").Trim();
                else
                    _externs.Add($"extern {ti.AsCode(name)}; // offset 0x{offset:X}");
            else if (ti.ClassType == ClassType.Static)
                if (ti.TypeDef.IsFunctionReturnType)
                    _funcTypes[name] = ti.AsCode("").Trim();
                else
                    _externs.Add($"static {ti.AsCode(name)}; // offset 0x{offset:X}");
            else
                throw new Exception("Gomorrha");
        }

        public Function FindFunction(uint addr)
        {
            return Functions.FirstOrDefault(f => f.Address == addr);
        }
    }
}
