using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using symdump;
using symfile.util;

namespace symfile
{
    public class Block
    {
        public readonly List<Block> subBlocks = new List<Block>();
        public readonly List<string> vars = new List<string>();

        public readonly uint startOffset;
        public readonly uint startLine;
        public readonly uint endOffset;
        public readonly uint endLine;

        public readonly Function function;

        public Block(BinaryReader reader, uint ofs, uint ln, Function f)
        {
            startOffset = ofs;
            startLine = ln;
            function = f;

            while(true)
            {
                var typedValue = new TypedValue(reader);

                if(reader.skipSLD(typedValue))
                    continue;

                switch(typedValue.type & 0x7f)
                {
                    case 16:
                        subBlocks.Add(new Block(reader, (uint)typedValue.value, reader.ReadUInt32(), function));
                        break;
                    case 18:
                        endOffset = (uint)typedValue.value;
                        endLine = reader.ReadUInt32();
                        return;
                    case 20:
                    {
                        var ti = reader.readTypeInfo(false);
                        var memberName = reader.readPascalString();
                        Debug.Assert(!string.IsNullOrEmpty(memberName));
                        if(ti.classType == ClassType.AutoVar)
                        {
                            vars.Add($"{ti.asCode(memberName)}; // stack offset {typedValue.value}");
                        }
                        else if(ti.classType == ClassType.Register)
                        {
                            vars.Add($"{ti.asCode(memberName)}; // ${(Register)typedValue.value}");
                        }
                        else if(ti.classType == ClassType.Static)
                        {
                            vars.Add($"static {ti.asCode(memberName)}; // offset 0x{typedValue.value:x}");
                        }
                        else
                        {
                            throw new Exception("Ctulhu");
                        }
                        break;
                    }
                    case 22:
                    {
                        var ti = reader.readTypeInfo(true);
                        var memberName = reader.readPascalString();
                        Debug.Assert(!string.IsNullOrEmpty(memberName));
                        if(ti.classType == ClassType.AutoVar)
                        {
                            vars.Add($"{ti.asCode(memberName)}; // stack offset {typedValue.value}");
                        }
                        else if(ti.classType == ClassType.Register)
                        {
                            vars.Add($"{ti.asCode(memberName)}; // ${(Register)typedValue.value}");
                        }
                        else if(ti.classType == ClassType.Static)
                        {
                            vars.Add($"static {ti.asCode(memberName)}; // offset 0x{typedValue.value:x}");
                        }
                        else
                        {
                            throw new Exception("Ctulhu");
                        }
                        break;
                    }
                }
            }
        }

        public void dump(IndentedTextWriter writer)
        {
            writer.WriteLine($"{{ // line {startLine}, offset 0x{startOffset:x}");
            ++writer.Indent;
            vars.ForEach(writer.WriteLine);
            subBlocks.ForEach(b => b.dump(writer));
            --writer.Indent;
            writer.WriteLine($"}} // line {endLine}, offset 0x{endOffset:x}");
        }
    }
}