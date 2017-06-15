using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using symdump;
using symfile.util;

namespace symfile
{
    public class EnumDef
    {
        public readonly Dictionary<int, string> members = new Dictionary<int, string>();
        public readonly string name;

        public EnumDef(BinaryReader stream, string name)
        {
            this.name = name;
            while (true)
            {
                var typedValue = new TypedValue(stream);
                if (typedValue.type == (0x80 | 20))
                {
                    var classx = stream.readClassType();
                    var typex = stream.readTypeDef();
                    var size = stream.ReadUInt32();
                    var memberName = stream.readPascalString();

                    if (classx == ClassType.EndOfStruct)
                        break;
                    if (classx != ClassType.EnumMember)
                        throw new Exception("Unexcpected class");

                    members.Add(typedValue.value, memberName);
                }
                else if (typedValue.type == (0x80 | 22))
                {
                    var classx = stream.readClassType();
                    var typex = stream.readTypeDef();
                    var size = stream.ReadUInt32();
                    var dims = stream.ReadUInt16();
                    var dimsData = new uint[dims];
                    for (var i = 0; i < dims; ++i)
                        dimsData[i] = stream.ReadUInt32();
                    var tag = stream.readPascalString();
                    var memberName = stream.readPascalString();

                    if (classx == ClassType.EndOfStruct)
                        break;
                    if (classx != ClassType.EnumMember)
                        throw new Exception("Unexcpected class");

                    members.Add(typedValue.value, memberName);
                }
                else
                {
                    throw new Exception("Unexcpected entry");
                }
            }
        }

        public void dump(IndentedTextWriter writer)
        {
            writer.WriteLine($"enum {name} {{");
            ++writer.Indent;
            foreach (var kvp in members)
                writer.WriteLine($"{kvp.Value} = {kvp.Key},");
            --writer.Indent;
            writer.WriteLine("};");
        }
    }
}