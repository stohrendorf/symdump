// #define WITH_SLD

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using NLog;
using symdump.symfile.util;
using symdump.util;

namespace symdump.symfile
{
    public class SymFile
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private readonly IList<ObjectFile> _files = new List<ObjectFile>();
        private readonly string _mxInfo;

        private readonly ISet<string> _overlays = new SortedSet<string>();

        // The general structure of a SYM file seems to be:
        // 1. Overlay definitions or global labels
        // 2. Object file symbols (repeated)
        //    a) (optional) set overlay
        //    b) SLD info with filename, dependent on overlay
        //    c) List of SLD entries
        //    d) SDL end entry
        //    e) definitions
        // 3. List of filenames (object/library files)
        private readonly byte _targetUnit;
        private readonly byte _version;

        public readonly IDictionary<uint, List<Function>> Functions;

        public readonly IDictionary<uint, IList<Label>> Labels;

        public SymFile(BinaryReader stream, string flatFilename)
        {
            logger.Info("Loading SYM file...");
            stream.BaseStream.Seek(0, SeekOrigin.Begin);

            stream.Skip(3);
            _version = stream.ReadByte();
            _targetUnit = stream.ReadByte();

            stream.Skip(3);
            if (flatFilename == null)
            {
                while (true)
                {
                    var basePos = stream.BaseStream.Position;
                    logger.Info($"Skipping overlay definitions at source file offset 0x{basePos:x8}");
                    var typedValue = new TypedValue(stream);
                    if (typedValue.Type == (0x80 | TypedValue.Overlay))
                    {
                        ReadOverlayDef(stream, typedValue.Value);
                    }
                    else
                    {
                        stream.BaseStream.Position = basePos;
                        break;
                    }
                }

                while (stream.BaseStream.Position < stream.BaseStream.Length) _files.Add(new ObjectFile(stream));

                Labels = _files
                    .SelectMany(_ => _.Labels)
                    .GroupBy(_ => _.Key)
                    .Select(group =>
                        new KeyValuePair<uint, IList<Label>>(group.Key, group.SelectMany(_ => _.Value).ToList()))
                    .ToImmutableDictionary(kv => kv.Key, kv => kv.Value);

                Functions = _files
                    .SelectMany(_ => _.Functions)
                    .GroupBy(_ => _.Address)
                    .ToImmutableDictionary(kv => kv.Key, kv => kv.ToList());
            }
            else
            {
                using (var f = File.CreateText(flatFilename))
                {
                    var writer = new IndentedTextWriter(f);
                    while (stream.BaseStream.Position < stream.BaseStream.Length)
                    {
                        var typedValue = new TypedValue(stream);
                        writer.Write($"0x{typedValue.Value:x8} ");

                        if (typedValue.Type == 8)
                        {
                            _mxInfo = $"MX-info {stream.ReadByte():X}";
                            writer.WriteLine(_mxInfo);
                            continue;
                        }

                        if (typedValue.IsLabel)
                        {
                            var lbl = new Label(typedValue, stream);
                            writer.WriteLine($"Label {lbl.Name}");
                            continue;
                        }

                        switch (typedValue.Type & 0x7f)
                        {
                            case TypedValue.IncSLD:
                                writer.WriteLine("SLD++");
                                break;
                            case TypedValue.AddSLD1:
                                writer.WriteLine($"SLD += {stream.ReadByte()}");
                                break;
                            case TypedValue.AddSLD2:
                                writer.WriteLine($"SLD += {stream.ReadUInt16()}");
                                break;
                            case TypedValue.SetSLD:
                                writer.WriteLine($"SLD = {stream.ReadUInt32()}");
                                break;
                            case TypedValue.BeginSLD:
                                writer.Indent = 0;
                                writer.Write($"Begin SLD = {stream.ReadUInt32()} ; ");
                                writer.WriteLine(stream.ReadPascalString());
                                writer.Indent++;
                                break;
                            case TypedValue.EndSLDInfo:
                                writer.Indent--;
                                writer.WriteLine("SLD End.");
                                break;
                            case TypedValue.Function:
                            {
                                writer.WriteLine("Function");
                                writer.Indent++;
                                var fp = stream.ReadInt16();
                                var fsize = stream.ReadInt32();
                                var retreg = stream.ReadInt16();
                                var mask = stream.ReadUInt32();
                                var maskoffs = stream.ReadInt32();
                                var line = stream.ReadInt32();
                                var file = stream.ReadPascalString();
                                var name = stream.ReadPascalString();

                                writer.WriteLine($"src         {name} @ {file}:{line}");
                                writer.WriteLine($"retreg      {retreg} = ${(Register) retreg}");
                                writer.Write(
                                    $"stack frame size={fsize} @ fp=${(Register) fp} (saved registers @ fp{maskoffs:0:+#;-#;+0} =");
                                for (var i = 0; i < 32; ++i)
                                    if (((1 << i) & mask) != 0)
                                        writer.Write($" ${(Register) i}");
                                writer.WriteLine(")");
                                break;
                            }
                            case TypedValue.FunctionEnd:
                                writer.WriteLine($"Function End, line={stream.ReadInt32()}");
                                writer.Indent--;
                                break;
                            case TypedValue.Block:
                                writer.WriteLine($"Block start, line={stream.ReadInt32()}");
                                writer.Indent++;
                                break;
                            case TypedValue.BlockEnd:
                                writer.Indent--;
                                writer.WriteLine($"Block end, line={stream.ReadInt32()}");
                                break;
                            case TypedValue.Definition:
                            {
                                writer.Write("Def ");
                                var @class = PrintClass(stream, writer);
                                PrintType(stream, writer);
                                writer.Write($"size={stream.ReadInt32()} ");
                                writer.Write($"name={stream.ReadPascalString()}");
                                switch (@class)
                                {
                                    case SymbolType.RegParam:
                                    case SymbolType.Register:
                                        writer.Write($" register=${(Register) typedValue.Value}");
                                        break;
                                }

                                writer.WriteLine();
                                break;
                            }
                            case TypedValue.ArrayDefinition:
                            {
                                writer.Write("Def2 ");
                                var @class = PrintClass(stream, writer);
                                PrintType(stream, writer);
                                writer.Write($"size={stream.ReadInt32()} ");
                                writer.Write("dims=");
                                var n = stream.ReadInt16();
                                if (n < 0) throw new Exception();
                                while (n-- != 0) writer.Write($"{stream.ReadInt32()}");

                                writer.Write($" tag={stream.ReadPascalString()} ");
                                writer.Write($"name={stream.ReadPascalString()}");
                                switch (@class)
                                {
                                    case SymbolType.RegParam:
                                    case SymbolType.Register:
                                        writer.Write($" register=${(Register) typedValue.Value}");
                                        break;
                                }

                                writer.WriteLine();
                                break;
                            }
                            case TypedValue.Overlay:
                                writer.Write($"overlay length=0x{stream.ReadInt32():x8} ");
                                writer.WriteLine($"id=0x{stream.ReadInt32():x8}");
                                break;
                            case TypedValue.SetOverlay:
                                writer.WriteLine("set overlay");
                                break;
                            default:
                                throw new Exception($"Unhandled debug type 0x{typedValue.Type:X}");
                        }
                    }
                }
            }
        }

        private static SymbolType PrintClass(BinaryReader stream, IndentedTextWriter writer)
        {
            var type = (SymbolType) stream.ReadInt16();
            writer.Write($"class={type} ");
            switch (type)
            {
                case SymbolType.EndOfStruct:
                case SymbolType.EndFunction:
                    writer.Indent--;
                    break;
                case SymbolType.Function:
                case SymbolType.Enum:
                case SymbolType.Union:
                case SymbolType.Struct:
                    writer.Indent++;
                    break;
            }

            return type;
        }

        private static void PrintType(BinaryReader stream, TextWriter writer)
        {
            writer.Write($"type={new DerivedTypeDef(stream)} ");
        }

        public void Dump(TextWriter output)
        {
            var writer = new IndentedTextWriter(output);
            writer.WriteLine($"Version = {_version}, targetUnit = {_targetUnit}");

            foreach (var objectFile in _files)
                objectFile.Dump(writer);
        }


        private void ReadOverlayDef(BinaryReader stream, int offset)
        {
            var overlay = new Overlay(stream);
            if (!_overlays.Add($"Overlay {overlay} // offset 0x{offset:X}"))
                throw new Exception("Duplicate overlay definition");
        }
    }
}
