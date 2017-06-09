using System;
using System.IO;
using symfile.util;
using System.Collections.Generic;
using System.CodeDom.Compiler;

namespace symfile
{
	public class SymFile
	{
		private readonly Dictionary<int, List<Label>> labels = new Dictionary<int, List<Label>>();

		private readonly IndentedTextWriter writer;

		public SymFile(FileStream fs, TextWriter output)
		{
			writer = new IndentedTextWriter(output);

			fs.Seek(0, SeekOrigin.Begin);
			fs.Skip(3);
			var version = fs.ReadU1();
			var targetUnit = fs.ReadU1();
			writer.WriteLine($"Version = {version}, targetUnit = {targetUnit}");
			fs.Skip(3);
			while (fs.Position < fs.Length)
			{
				dumpEntry(fs);
			}
		}

		private void dumpEntry(FileStream fs)
		{
			var typedValue = new TypedValue(fs);
			if (typedValue.type == 8)
			{
				writer.WriteLine($"${typedValue.value:X} MX-info {fs.ReadU1():X}");
				return;
			}

			if (typedValue.isLabel)
			{
				var lbl = new Label(typedValue, fs);

				if(!labels.ContainsKey(lbl.offset))
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
				writer.WriteLine($"${typedValue.value:X} Inc SLD linenum by byte {fs.ReadU1()}");
				#else
				fs.Skip(1);
				#endif
				break;
			case 4:
				#if WITH_SLD
				writer.WriteLine($"${typedValue.value:X} Inc SLD linenum by word {fs.ReadU2()}");
				#else
				fs.Skip(2);
				#endif
				break;
			case 6:
				#if WITH_SLD
				writer.WriteLine($"${typedValue.value:X} Set SLD linenum to {fs.ReadU4()}");
				#else
				fs.Skip(4);
				#endif
				break;
			case 8:
				#if WITH_SLD
				writer.WriteLine($"${typedValue.value:X} Set SLD to line {fs.ReadU4()} of file " +
					fs.readPascalString());
				#else
				fs.Skip(4);
				fs.Skip(fs.ReadU1());
				#endif
				break;
			case 10:
				#if WITH_SLD
				writer.WriteLine($"${typedValue.value:X} End SLD info");
				#endif
				break;
			case 12:
				writer.WriteLine($"${typedValue.value:X} Function start");
				writer.WriteLine($"    fp = {fs.ReadU2()}");
				writer.WriteLine($"    fsize = {fs.ReadU4()}");
				writer.WriteLine($"    retreg = {fs.ReadU2()}");
				writer.WriteLine($"    mask = ${fs.ReadU4():X}");
				writer.WriteLine($"    maskoffs = ${fs.ReadU4():X}");
				writer.WriteLine($"    line = {fs.ReadU4()}");
				writer.WriteLine($"    file = {fs.readPascalString()}");
				writer.WriteLine($"    name = {fs.readPascalString()}");
				break;
			case 14:
				writer.WriteLine($"${typedValue.value:X} Function end   line {fs.ReadU4()}");
				break;
			case 16:
				writer.WriteLine($"${typedValue.value:X} Block start  line = {fs.ReadU4()}");
				break;
			case 18:
				writer.WriteLine($"${typedValue.value:X} Block end  line = {fs.ReadU4()}");
				break;
			case 20:
				dumpType20(fs, typedValue.value);
				break;
			case 22:
				dumpType22(fs, typedValue.value);
				break;
			default:
				writer.WriteLine($"?? {typedValue.value} {typedValue.type&0x7f} ??");
				break;
			}
		}

		private void dumpType20(FileStream fs, int offset)
		{
			var classx = fs.readClassType();
			var typex = fs.readTypeDef();
			var size = fs.ReadU4();
			var name = fs.readPascalString();

			if( classx == symdump.ClassType.Enum && typex.baseType == symdump.BaseType.EnumDef ) {
				var e = new EnumDef(fs, name);
				e.dump(writer);
				return;
			}

			if(classx == symdump.ClassType.EndOfStruct)
				--writer.Indent;
			
			writer.WriteLine($"${offset:X} Def class={classx} type={typex} size={size} name={name}");

			if(classx == symdump.ClassType.Struct || classx == symdump.ClassType.Union || classx == symdump.ClassType.Enum)
				++writer.Indent;
		}

		private void dumpType22(FileStream fs, int offset)
		{
			var classx = fs.readClassType();
			var typex = fs.readTypeDef();
			var size = fs.ReadU4();
			var dims = fs.ReadU2();
			var dimsData = new uint[dims];
			for (int i = 0; i < dims; ++i)
				dimsData[i] = fs.ReadU4();
			var tag = fs.readPascalString();
			var name = fs.readPascalString();

			if( classx == symdump.ClassType.Enum && typex.baseType == symdump.BaseType.EnumDef ) {
				var e = new EnumDef(fs, name);
				e.dump(writer);
				return;
			}

			if(classx == symdump.ClassType.EndOfStruct)
				--writer.Indent;

			writer.WriteLine(
				$"${offset:X} Def class={classx} type={typex} size={size} dims=[{string.Join(",", dimsData)}] tag={tag} name={name}");

			if(classx == symdump.ClassType.Struct || classx == symdump.ClassType.Union || classx == symdump.ClassType.Enum)
				++writer.Indent;
		}
	}
}
