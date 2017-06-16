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
                    var ti = stream.readTypeInfo(false);
                    var memberName = stream.readPascalString();

                    if (ti.classType == ClassType.EndOfStruct)
                        break;

                    switch(ti.classType)
                    {
                        case ClassType.Bitfield:
                            members.Add(ti.asCode(memberName) +
                                        $" : {ti.size}; // offset={typedValue.value / 8}.{typedValue.value % 8}");
                            break;
                        case ClassType.StructMember:
                            members.Add(ti.asCode(memberName) +
                                        $"; // size={ti.size}, offset={typedValue.value}");
                            break;
                        default:
                            throw new Exception("Unexpected class");
                    }
                }
                else if (typedValue.type == (0x80 | 22))
                {
                    var ti = stream.readTypeInfo(true);
                    var memberName = stream.readPascalString();

                    if (ti.classType == ClassType.EndOfStruct)
                        break;

                    switch(ti.classType)
                    {
                        case ClassType.Bitfield:
                            members.Add(ti.asCode(memberName) +
                                        $" : {ti.size}; // offset={typedValue.value / 8}.{typedValue.value % 8}");
                            break;
                        case ClassType.StructMember:
                            members.Add(ti.asCode(memberName) +
                                        $"; // size={ti.size}, offset={typedValue.value}");
                            break;
                        default:
                            throw new Exception("Unexpected class");
                    }
                }
                else
                {
                    throw new Exception("Unexpected entry");
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