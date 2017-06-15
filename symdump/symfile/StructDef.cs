using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using symdump;
using symfile.util;

namespace symfile
{
    public class StructDef
    {
        public readonly List<string> members = new List<string>();
        public readonly string name;

        public StructDef(BinaryReader stream, string name)
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
                    if (classx == ClassType.Bitfield)
                        members.Add(typex.asCode(memberName, null, null) +
                                    $" : {size}; // offset={typedValue.value / 8}.{typedValue.value % 8}");
                    else if (classx == ClassType.StructMember)
                        members.Add(typex.asCode(memberName, null, null) +
                                    $"; // size={size}, offset={typedValue.value}");
                    else
                        throw new Exception("Unexcpected class");
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
                    if (classx == ClassType.Bitfield)
                        members.Add(typex.asCode(memberName, dimsData, tag) +
                                    $" : {size}; // offset={typedValue.value / 8}.{typedValue.value % 8}");
                    else if (classx == ClassType.StructMember)
                        members.Add(typex.asCode(memberName, dimsData, tag) +
                                    $"; // size={size}, offset={typedValue.value}");
                    else
                        throw new Exception("Unexcpected class");
                }
                else
                {
                    throw new Exception("Unexcpected entry");
                }
            }
        }

        public void dump(IndentedTextWriter writer)
        {
            writer.WriteLine($"struct {name} {{");
            ++writer.Indent;
            foreach (var m in members)
                writer.WriteLine(m);
            --writer.Indent;
            writer.WriteLine("};");
        }
    }
}