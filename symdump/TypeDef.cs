using System.IO;
using System.Linq;
using symfile.util;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace symdump
{
	public class TypeDef
	{
		public readonly BaseType baseType;
		public readonly DerivedType[] derivedTypes = new DerivedType[6];

		public TypeDef(BinaryReader fs)
		{
			var val = fs.ReadUInt16();
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

		public string asCode(string name, uint[] dims, string tag)
		{
			Debug.Assert(!string.IsNullOrEmpty(name));

		    int dimIdx = 0;

			string ctype;
			switch(baseType) {
			case BaseType.StructDef:
				Debug.Assert(!string.IsNullOrEmpty(tag));
				ctype = $"struct {tag}";
				break;
			case BaseType.UnionDef:
				Debug.Assert(!string.IsNullOrEmpty(tag));
				ctype = $"union {tag}";
				break;
			case BaseType.EnumDef:
				Debug.Assert(!string.IsNullOrEmpty(tag));
				ctype = $"enum {tag}";
				break;
			case BaseType.Char:
				ctype = "char";
				break;
			case BaseType.Short:
				ctype = "short";
				break;
			case BaseType.Int:
				ctype = "int";
				break;
			case BaseType.Long:
				ctype = "long";
				break;
			case BaseType.Float:
				ctype = "float";
				break;
			case BaseType.Double:
				ctype = "double";
				break;
			case BaseType.UChar:
				ctype = "unsigned char";
				break;
			case BaseType.UShort:
				ctype = "unsigned short";
				break;
			case BaseType.UInt:
				ctype = "unsigned int";
				break;
			case BaseType.ULong:
				ctype = "unsigned long";
				break;
			case BaseType.Void:
				ctype = "void";
				break;
			default:
				throw new Exception($"Unexpected base type {baseType}");
			}

			foreach(var dt in derivedTypes) {
				switch(dt) {
				case DerivedType.None:
					continue;
				case DerivedType.Array:
					Debug.Assert(name != null);
					Debug.Assert(dims != null);
					name += $"[{dims[dimIdx]}]";
				    ++dimIdx;
					break;
				case DerivedType.FunctionReturnType:
					name = $"({name})()"; //TODO function args?
					break;
				case DerivedType.Pointer:
					name = $"*{name}";
					break;
				}
			}

			return $"{ctype} {name}";
		}
	}
}