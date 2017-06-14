using System;
using System.CodeDom.Compiler;
using symfile.util;
using symdump;
using System.Collections.Generic;
using System.IO;

namespace symfile
{
	public class StructDef
	{
		public readonly string name;
		public readonly List<string> members = new List<string>();

		public StructDef(FileStream fs, string name)
		{
			this.name = name;
			while(true) {
				var typedValue = new TypedValue(fs);
				if(typedValue.type == (0x80 | 20)) {
					var classx = fs.readClassType();
					var typex = fs.readTypeDef();
					var size = fs.ReadU4();
					var memberName = fs.readPascalString();

					if(classx == ClassType.EndOfStruct)
						break;
					else if(classx == ClassType.Bitfield)
						members.Add(typex.asCode(memberName, null, null) +$" : {size}; // offset={typedValue.value/8}.{typedValue.value%8}");
					else if(classx == ClassType.StructMember)
						members.Add(typex.asCode(memberName, null, null) +$"; // size={size}, offset={typedValue.value}");
					else
						throw new Exception("Unexcpected class");
				} else if(typedValue.type == (0x80 | 22)) {
					var classx = fs.readClassType();
					var typex = fs.readTypeDef();
					var size = fs.ReadU4();
					var dims = fs.ReadU2();
					var dimsData = new uint[dims];
					for(int i = 0; i < dims; ++i)
						dimsData[i] = fs.ReadU4();
					var tag = fs.readPascalString();
					var memberName = fs.readPascalString();

					if(classx == ClassType.EndOfStruct)
						break;
					else if(classx == ClassType.Bitfield)
						members.Add(typex.asCode(memberName, dimsData, tag) +$" : {size}; // offset={typedValue.value/8}.{typedValue.value%8}");
					else if(classx == ClassType.StructMember)
						members.Add(typex.asCode(memberName, dimsData, tag) +$"; // size={size}, offset={typedValue.value}");
					else
						throw new Exception("Unexcpected class");
				} else {
					throw new Exception("Unexcpected entry");
				}
			}
		}

		public void dump(IndentedTextWriter writer)
		{
			writer.WriteLine($"struct {name} {{");
			++writer.Indent;
			foreach(var m in members)
				writer.WriteLine(m);
			--writer.Indent;
			writer.WriteLine("};");
		}
	}
}

