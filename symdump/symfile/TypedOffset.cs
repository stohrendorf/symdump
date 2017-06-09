using System;
using System.IO;
using symfile.util;

namespace symfile
{
	public class TypedOffset
	{
		public readonly int offset;
		public readonly byte type;

		public TypedOffset(FileStream fs)
		{
			offset = fs.ReadI4();
			type = fs.ReadU1();
		}
	}
}
