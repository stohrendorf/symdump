using System.IO;
using System.Linq;
using symfile.util;

namespace symdump
{
	public class TypeDef
	{
		public readonly BaseType baseType;
		public readonly DerivedType[] derivedTypes = new DerivedType[6];

		public TypeDef(FileStream fs)
		{
			var val = fs.ReadU2();
			baseType = (BaseType)(val & 0x0f);
			for(var i = 0; i < 6; ++i) {
				var x = (val >> (i * 2 + 4)) & 3;
				derivedTypes[i] = (DerivedType)x;
			}
		}

		public override string ToString()
		{
			var attribs = string.Join(",", derivedTypes.Where(e => e != DerivedType.None));
			return attribs.Length == 0 ? baseType.ToString() : $"{baseType}({attribs})";
		}
	}
}