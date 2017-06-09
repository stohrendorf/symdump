using System;
using System.IO;

namespace symdump
{
	public class SymFile
	{
		public SymFile(FileStream fs)
		{
			fs.Seek(0, SeekOrigin.Begin);
			fs.Skip(3);
			var version = fs.ReadU1();
			var targetUnit = fs.ReadU1();
			Console.WriteLine($"Version = {version}, targetUnit = {targetUnit}");
			fs.Skip(3);
			while (fs.Position < fs.Length)
			{
				dumpEntry(fs);
			}
		}

		private void dumpEntry(FileStream fs)
		{
			var offset = fs.ReadU4();
			var type = fs.ReadU1();
			if (type == 8)
			{
				Console.WriteLine($"${offset:X} MX-info {fs.ReadU1():X}");
				return;
			}

			if ((type & 0x80) == 0)
			{
				Console.WriteLine($"${offset:X} {fs.readPascalString()}");
				return;
			}

			switch (type & 0x7f)
			{
			case 0:
				Console.WriteLine($"${offset:X} Inc SLD linenum");
				break;
			case 2:
				Console.WriteLine($"${offset:X} Inc SLD linenum by byte {fs.ReadU1()}");
				break;
			case 4:
				Console.WriteLine($"${offset:X} Inc SLD linenum by word {fs.ReadU2()}");
				break;
			case 6:
				Console.WriteLine($"${offset:X} Set SLD linenum to {fs.ReadU4()}");
				break;
			case 8:
				Console.WriteLine($"${offset:X} Set SLD to line {fs.ReadU4()} of file " +
					fs.readPascalString());
				break;
			case 10:
				Console.WriteLine($"${offset:X} End SLD info", offset, type);
				break;
			case 12:
				Console.WriteLine($"${offset:X} Function start", offset, type);
				Console.WriteLine($"    fp = {fs.ReadU2()}");
				Console.WriteLine($"    fsize = {fs.ReadU4()}");
				Console.WriteLine($"    retreg = {fs.ReadU2()}");
				Console.WriteLine($"    mask = ${fs.ReadU4():X}");
				Console.WriteLine($"    maskoffs = ${fs.ReadU4():X}");
				Console.WriteLine($"    line = {fs.ReadU4()}");
				Console.WriteLine($"    file = {fs.readPascalString()}");
				Console.WriteLine($"    name = {fs.readPascalString()}");
				break;
			case 14:
				Console.WriteLine($"${offset:X} Function end   line {fs.ReadU4()}");
				break;
			case 16:
				Console.WriteLine($"${offset:X} Block start  line = {fs.ReadU4()}");
				break;
			case 18:
				Console.WriteLine($"${offset:X} Block end  line = {fs.ReadU4()}");
				break;
			case 20:
				dumpType20(fs, offset);
				break;
			case 22:
				dumpType22(fs, offset);
				break;
			default:
				Console.WriteLine($"?? {offset} {type&0x7f} ??");
				break;
			}
		}

		private void dumpType20(FileStream fs, uint offset)
		{
			var classx = fs.readClassType();
			var typex = fs.readTypeDef();
			var size = fs.ReadU4();
			var name = fs.readPascalString();
			Console.WriteLine($"${offset:X} Def class={classx} type={typex} size={size} name={name}");
		}

		private void dumpType22(FileStream fs, uint offset)
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
			Console.WriteLine(
				$"${offset:X} Def class={classx} type={typex} size={size} dims=[{string.Join(",", dimsData)}] tag={tag} name={name}");
		}
	}
}

