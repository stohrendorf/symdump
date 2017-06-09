using System;
using System.IO;
using symfile.util;

namespace symfile
{
	public class TypedValue
	{
		public readonly int value;
		public readonly byte type;

		public bool isLabel => (type & 0x80) == 0;

		public TypedValue(FileStream fs)
		{
			value = fs.ReadI4();
			type = fs.ReadU1();
		}
	}
}
