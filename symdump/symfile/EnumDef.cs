using System;
using System.IO;
using symdump;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.Linq;
using symfile.util;

namespace symfile
{
	public class EnumDef
	{
		public readonly string name;
		public readonly Dictionary<int, string> members = new Dictionary<int, string>();

		public EnumDef(FileStream fs, string name)
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
					else if(classx != ClassType.EnumMember)
						throw new Exception("Unexcpected class");

					members.Add(typedValue.value, memberName);
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
					else if(classx != ClassType.EnumMember)
						throw new Exception("Unexcpected class");

					members.Add(typedValue.value, memberName);
				} else {
					throw new Exception("Unexcpected entry");
				}
			}
		}

		public void dump(IndentedTextWriter writer)
		{
			writer.WriteLine($"enum {name} {{");
			++writer.Indent;
			foreach(var kvp in members)
				writer.WriteLine($"{kvp.Value} = {kvp.Key},");
			--writer.Indent;
			writer.WriteLine("};");
		}
	}
}
