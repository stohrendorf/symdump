using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using symdump;
using symfile.util;

namespace symfile
{
    public class Function
    {
        private readonly uint offset;
        private readonly ushort fp;
        private readonly uint fsize;
        private readonly ushort register;
        private readonly uint mask;
        private readonly uint maskOffs;
        private readonly uint line;
        private readonly string file;
        private readonly string name;
        private readonly uint lastLine;
        private readonly string returnType;

        internal readonly List<string> parameters = new List<string>();
        private readonly List<Block> blocks = new List<Block>();
        
        public Function(BinaryReader reader, uint ofs, Dictionary<string, string> funcTypes)
        {
            offset = ofs;
            
            fp = reader.ReadUInt16();
            fsize = reader.ReadUInt32();
            register = reader.ReadUInt16();
            mask = reader.ReadUInt32();
            maskOffs = reader.ReadUInt32();

            line = reader.ReadUInt32();
            file = reader.readPascalString();
            name = reader.readPascalString();

            if(!funcTypes.TryGetValue(name, out returnType))
                returnType = "__UNKNOWN__";

            while(true)
            {
                var typedValue = new TypedValue(reader);

                if(reader.skipSLD(typedValue))
                    continue;
                
                TypeInfo ti = null;
                string memberName = null;
                switch(typedValue.type & 0x7f)
                {
                    case 14: // end of function
                        lastLine = reader.ReadUInt32();
                        return;
                    case 16: // begin of block
                        blocks.Add(new Block(reader, (uint)typedValue.value, reader.ReadUInt32(), this));
                        continue;
                    case 20:
                        ti = reader.readTypeInfo(false);
                        memberName = reader.readPascalString();
                        break;
                    case 22:
                        ti = reader.readTypeInfo(true);
                        memberName = reader.readPascalString();
                        break;
                    default:
                        throw new Exception("Nope");
                }

                if(ti == null || memberName == null)
                    break;
                
                if(ti.classType == ClassType.Argument)
                {
                    parameters.Add($"{ti.asCode(memberName)} /*sp {typedValue.value}*/");
                }
                else if(ti.classType == ClassType.RegParam)
                {
                    parameters.Add($"{ti.asCode(memberName)} /*reg ${typedValue.value}*/");
                }
            }
        }

        public void dump(IndentedTextWriter writer)
        {
            writer.WriteLine("/*");
            writer.WriteLine($" * Offset 0x{offset:X}");
            writer.WriteLine($" * {file} (line {line})");
            writer.WriteLine($" * Stack frame base ${fp}, size {fsize}");
            writer.WriteLine(" */");

            writer.WriteLine($"{returnType} /*reg ${register}*/ {name}({string.Join(", ", parameters)})");
            
            blocks.ForEach(b => b.dump(writer));
        }
    }
}
