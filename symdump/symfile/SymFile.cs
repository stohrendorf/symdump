using System;
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
        private readonly Dictionary<string, EnumDef> enums = new Dictionary<string, EnumDef>();
        private readonly Dictionary<string, TypeInfo> typedefs = new Dictionary<string, TypeInfo>();

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
            while(stream.BaseStream.Position < stream.BaseStream.Length)
                dumpEntry(stream);
            
            writer.WriteLine($"// {enums.Count} enums");
            foreach(var e in enums.Values)
                e.dump(writer);
            
            writer.WriteLine($"// {typedefs.Count} typedefs");
            foreach(var t in typedefs)
                writer.WriteLine($"typedef {t.Value.asCode(t.Key)};");
        }

        private void dumpEntry(BinaryReader stream)
        {
            var typedValue = new TypedValue(stream);
            if(typedValue.type == 8)
            {
                writer.WriteLine($"${typedValue.value:X} MX-info {stream.ReadByte():X}");
                return;
            }

            if(typedValue.isLabel)
            {
                var lbl = new Label(typedValue, stream);

                if(!labels.ContainsKey(lbl.offset))
                    labels.Add(lbl.offset, new List<Label>());

                labels[lbl.offset].Add(lbl);
                writer.WriteLine(lbl);
                return;
            }

            switch(typedValue.type & 0x7f)
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
                    writer.WriteLine(
                        $"}} // end of function (offset 0x{typedValue.value:X}, line {stream.ReadUInt32()})");
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

        private void readEnum(BinaryReader reader, string name)
        {
            var e = new EnumDef(reader, name);

            EnumDef already;
            if(enums.TryGetValue(name, out already))
            {
                if(!e.Equals(already))
                    throw new Exception($"Non-uniform definitions of enum {name}");

                return;
            }

            enums.Add(name, e);
        }

        private void addTypedef(string name, TypeInfo typeInfo)
        {
            TypeInfo already;
            if(typedefs.TryGetValue(name, out already))
            {
                if(!typeInfo.Equals(already))
                    throw new Exception($"Non-uniform definitions of typedef for {name}");

                return;
            }

            typedefs.Add(name, typeInfo);
        }

        private void dumpType20(BinaryReader stream, int offset)
        {
            var ti = stream.readTypeInfo(false);
            var name = stream.readPascalString();

            if(ti.classType == ClassType.Enum && ti.typeDef.baseType == BaseType.EnumDef)
            {
                readEnum(stream, name);
                return;
            }
            if(ti.classType == ClassType.FileName)
            {
                return;
            }
            if(ti.classType == ClassType.Struct && ti.typeDef.baseType == BaseType.StructDef)
            {
                var s = new StructDef(stream, name);
                s.dump(writer);
                return;
            }
            else if(ti.classType == ClassType.Union && ti.typeDef.baseType == BaseType.UnionDef)
            {
                var s = new UnionDef(stream, name);
                s.dump(writer);
                return;
            }
            else if(ti.classType == ClassType.Typedef)
            {
                addTypedef(name, ti);
                return;
            }
            else if(ti.classType == ClassType.External)
            {
                writer.WriteLine($"extern {ti.asCode(name)};");
                return;
            }
            else if(ti.classType == ClassType.AutoVar)
            {
                writer.WriteLine($"{ti.asCode(name)}; // stack offset {offset}");
                return;
            }
            else if(ti.classType == ClassType.Register)
            {
                writer.WriteLine($"{ti.asCode(name)}; // register ${offset}");
                return;
            }
            else if(ti.classType == ClassType.Argument)
            {
                writer.WriteLine($"{ti.asCode(name)}; // parameter, stack offset {offset}");
                return;
            }
            else if(ti.classType == ClassType.RegParam)
            {
                writer.WriteLine($"{ti.asCode(name)}; // parameter, register ${offset}");
                return;
            }
            else if(ti.classType == ClassType.Static)
            {
                writer.WriteLine($"static {ti.asCode(name)}; // offset 0x{offset:X}");
                return;
            }

            if(ti.classType == ClassType.EndOfStruct)
                --writer.Indent;

            writer.WriteLine($"${offset:X} Def class={ti.classType} type={ti.typeDef} size={ti.size} name={name}");

            if(ti.classType == ClassType.Struct || ti.classType == ClassType.Union || ti.classType == ClassType.Enum)
                ++writer.Indent;
        }

        private void dumpType22(BinaryReader stream, int offset)
        {
            var ti = stream.readTypeInfo(true);
            var name = stream.readPascalString();

            if(ti.classType == ClassType.Enum && ti.typeDef.baseType == BaseType.EnumDef)
            {
                readEnum(stream, name);
                return;
            }
            else if(ti.classType == ClassType.Typedef)
            {
                addTypedef(name, ti);
                return;
            }
            else if(ti.classType == ClassType.External)
            {
                writer.WriteLine($"extern {ti.asCode(name)};");
                return;
            }
            else if(ti.classType == ClassType.AutoVar)
            {
                writer.WriteLine($"{ti.asCode(name)}; // stack offset {offset}");
                return;
            }
            else if(ti.classType == ClassType.Register)
            {
                writer.WriteLine($"{ti.asCode(name)}; // register ${offset}");
                return;
            }
            else if(ti.classType == ClassType.Argument)
            {
                writer.WriteLine($"{ti.asCode(name)}; // parameter, stack offset {offset}");
                return;
            }
            else if(ti.classType == ClassType.RegParam)
            {
                writer.WriteLine($"{ti.asCode(name)}; // parameter, register ${offset}");
                return;
            }
            else if(ti.classType == ClassType.Static)
            {
                writer.WriteLine($"static {ti.asCode(name)}; // offset 0x{offset:X}");
                return;
            }

            if(ti.classType == ClassType.EndOfStruct)
                --writer.Indent;

            writer.WriteLine(
                $"${offset:X} Def class={ti.classType} type={ti.typeDef} size={ti.size} dims=[{string.Join(",", ti.dims)}] tag={ti.tag} name={name}");

            if(ti.classType == ClassType.Struct || ti.classType == ClassType.Union || ti.classType == ClassType.Enum)
                ++writer.Indent;
        }
    }
}
