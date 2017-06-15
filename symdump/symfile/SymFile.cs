using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using symdump;
using symfile.util;

namespace symfile
{
    public class SymFile
    {
        private readonly Dictionary<int, List<Label>> labels = new Dictionary<int, List<Label>>();

        private readonly IndentedTextWriter writer;

        public SymFile(BinaryReader stream, TextWriter output)
        {
            writer = new IndentedTextWriter(output);

            stream.BaseStream.Seek(0, SeekOrigin.Begin);
            stream.skip(3);
            var version = stream.ReadByte();
            var targetUnit = stream.ReadByte();
            writer.WriteLine($"Version = {version}, targetUnit = {targetUnit}");
            stream.skip(3);
            while (stream.BaseStream.Position < stream.BaseStream.Length)
                dumpEntry(stream);
        }

        private void dumpEntry(BinaryReader stream)
        {
            var typedValue = new TypedValue(stream);
            if (typedValue.type == 8)
            {
                writer.WriteLine($"${typedValue.value:X} MX-info {stream.ReadByte():X}");
                return;
            }

            if (typedValue.isLabel)
            {
                var lbl = new Label(typedValue, stream);

                if (!labels.ContainsKey(lbl.offset))
                    labels.Add(lbl.offset, new List<Label>());

                labels[lbl.offset].Add(lbl);
                writer.WriteLine(lbl);
                return;
            }

            switch (typedValue.type & 0x7f)
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
                    stream.skip(1);
#endif
                    break;
                case 4:
#if WITH_SLD
                writer.WriteLine($"${typedValue.value:X} Inc SLD linenum by word {stream.ReadUInt16()}");
                #else
                    stream.skip(2);
#endif
                    break;
                case 6:
#if WITH_SLD
                writer.WriteLine($"${typedValue.value:X} Set SLD linenum to {stream.ReadUInt32()}");
                #else
                    stream.skip(4);
#endif
                    break;
                case 8:
#if WITH_SLD
                writer.WriteLine($"${typedValue.value:X} Set SLD to line {stream.ReadUInt32()} of file " +
                    stream.readPascalString());
                #else
                    stream.skip(4);
                    stream.skip(stream.ReadByte());
#endif
                    break;
                case 10:
#if WITH_SLD
                writer.WriteLine($"${typedValue.value:X} End SLD info");
                #endif
                    break;
                case 12:
                    dumpType12(stream, typedValue.value);
                    break;
                case 14:
                    --writer.Indent;
                    writer.WriteLine($"}} // end of function (offset 0x{typedValue.value:X}, line {stream.ReadUInt32()})");
                    break;
                case 16:
                    writer.WriteLine($"{{ // offset 0x{typedValue.value:X}, line {stream.ReadUInt32()}");
                    ++writer.Indent;
                    break;
                case 18:
                    --writer.Indent;
                    writer.WriteLine($"}} // offset 0x{typedValue.value:X}, line {stream.ReadUInt32()}");
                    break;
                case 20:
                    dumpType20(stream, typedValue.value);
                    break;
                case 22:
                    dumpType22(stream, typedValue.value);
                    break;
                default:
                    writer.WriteLine($"?? {typedValue.value} {typedValue.type & 0x7f} ??");
                    break;
            }
        }

        private void dumpType12(BinaryReader stream, int offset)
        {
            var fp = stream.ReadUInt16();
            var fsize = stream.ReadUInt32();
            var register = stream.ReadUInt16();
            var mask = stream.ReadUInt32();
            var maskOffs = stream.ReadUInt32();

            var line = stream.ReadUInt32();
            var file = stream.readPascalString();
            var name = stream.readPascalString();

            writer.WriteLine("/*");
            writer.WriteLine($" * Offset 0x{offset:X}");
            writer.WriteLine($" * {file} (line {line})");
            writer.WriteLine($" * Stack frame base ${fp}, size {fsize}");
            writer.WriteLine("*/");

            writer.WriteLine($"${register} {name}(...)");
            writer.WriteLine("{");
            ++writer.Indent;
        }

        private void dumpType20(BinaryReader stream, int offset)
        {
            var classx = stream.readClassType();
            var typex = stream.readTypeDef();
            var size = stream.ReadUInt32();
            var name = stream.readPascalString();

            if (classx == ClassType.Enum && typex.baseType == BaseType.EnumDef)
            {
                var e = new EnumDef(stream, name);
                e.dump(writer);
                return;
            }
            if (classx == ClassType.FileName)
            {
                return;
            }
            if (classx == ClassType.Struct && typex.baseType == BaseType.StructDef)
            {
                var s = new StructDef(stream, name);
                s.dump(writer);
                return;
            }
            else if (classx == ClassType.Union && typex.baseType == BaseType.UnionDef)
            {
                var s = new UnionDef(stream, name);
                s.dump(writer);
                return;
            }
            else if (classx == ClassType.Typedef)
            {
                writer.WriteLine($"typedef {typex.asCode(name, null, null)};");
                return;
            }
            else if (classx == ClassType.External)
            {
                writer.WriteLine($"extern {typex.asCode(name, null, null)};");
                return;
            }
            else if (classx == ClassType.AutoVar)
            {
                writer.WriteLine($"{typex.asCode(name, null, null)}; // stack offset {offset}");
                return;
            }
            else if (classx == ClassType.Register)
            {
                writer.WriteLine($"{typex.asCode(name, null, null)}; // register ${offset}");
                return;
            }
            else if (classx == ClassType.Argument)
            {
                writer.WriteLine($"{typex.asCode(name, null, null)}; // parameter, stack offset {offset}");
                return;
            }
            else if (classx == ClassType.RegParam)
            {
                writer.WriteLine($"{typex.asCode(name, null, null)}; // parameter, register ${offset}");
                return;
            }
            else if (classx == ClassType.Static)
            {
                writer.WriteLine($"static {typex.asCode(name, null, null)}; // offset 0x{offset:X}");
                return;
            }

            if (classx == ClassType.EndOfStruct)
                --writer.Indent;

            writer.WriteLine($"${offset:X} Def class={classx} type={typex} size={size} name={name}");

            if (classx == ClassType.Struct || classx == ClassType.Union || classx == ClassType.Enum)
                ++writer.Indent;

            return;
        }

        private void dumpType22(BinaryReader stream, int offset)
        {
            var classx = stream.readClassType();
            var typex = stream.readTypeDef();
            var size = stream.ReadUInt32();
            var dims = stream.ReadUInt16();
            var dimsData = new uint[dims];
            for (var i = 0; i < dims; ++i)
                dimsData[i] = stream.ReadUInt32();
            var tag = stream.readPascalString();
            var name = stream.readPascalString();

            if (classx == ClassType.Enum && typex.baseType == BaseType.EnumDef)
            {
                var e = new EnumDef(stream, name);
                e.dump(writer);
                return;
            }
            else if (classx == ClassType.Typedef)
            {
                writer.WriteLine($"typedef {typex.asCode(name, dimsData, tag)};");
                return;
            }
            else if (classx == ClassType.External)
            {
                writer.WriteLine($"extern {typex.asCode(name, dimsData, tag)};");
                return;
            }
            else if (classx == ClassType.AutoVar)
            {
                writer.WriteLine($"{typex.asCode(name, dimsData, tag)}; // stack offset {offset}");
                return;
            }
            else if (classx == ClassType.Register)
            {
                writer.WriteLine($"{typex.asCode(name, dimsData, tag)}; // register ${offset}");
                return;
            }
            else if (classx == ClassType.Argument)
            {
                writer.WriteLine($"{typex.asCode(name, dimsData, tag)}; // parameter, stack offset {offset}");
                return;
            }
            else if (classx == ClassType.RegParam)
            {
                writer.WriteLine($"{typex.asCode(name, dimsData, tag)}; // parameter, register ${offset}");
                return;
            }
            else if (classx == ClassType.Static)
            {
                writer.WriteLine($"static {typex.asCode(name, dimsData, tag)}; // offset 0x{offset:X}");
                return;
            }

            if (classx == ClassType.EndOfStruct)
                --writer.Indent;

            writer.WriteLine(
                $"${offset:X} Def class={classx} type={typex} size={size} dims=[{string.Join(",", dimsData)}] tag={tag} name={name}");

            if (classx == ClassType.Struct || classx == ClassType.Union || classx == ClassType.Enum)
                ++writer.Indent;
        }
    }
}