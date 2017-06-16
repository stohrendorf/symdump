using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using symdump;
using symfile.util;

namespace symfile
{
    public class UnionDef
    {
        public readonly List<string> members = new List<string>();
        public readonly string name;

        public UnionDef(BinaryReader stream, string name)
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

                    if (ti.classType == ClassType.UnionMember)
                        members.Add(ti.asCode(memberName) +
                                    $"; // size={ti.size}, offset={typedValue.value}");
                    else
                        throw new Exception("Unexcpected class");
                }
                else if (typedValue.type == (0x80 | 22))
                {
                    var ti = stream.readTypeInfo(true);
                    var memberName = stream.readPascalString();

                    if (ti.classType == ClassType.EndOfStruct)
                        break;

                    if (ti.classType == ClassType.UnionMember)
                        members.Add(ti.asCode(memberName) +
                                    $"; // size={ti.size}, offset={typedValue.value}");
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
            writer.WriteLine($"union {name} {{");
            ++writer.Indent;
            foreach (var m in members)
                writer.WriteLine(m);
            --writer.Indent;
            writer.WriteLine("};");
        }
    }
}