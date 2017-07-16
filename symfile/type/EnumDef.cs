using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using core.util;
using symfile.util;

namespace symfile.type
{
    public class EnumDef : IEquatable<EnumDef>
    {
        private readonly Dictionary<string, int> m_members = new Dictionary<string, int>();
        private readonly string m_name;
        private readonly uint m_size;

        public EnumDef(BinaryReader stream, string name)
        {
            m_name = name;
            while (true)
            {
                var typedValue = new TypedValue(stream);
                if (typedValue.type == (0x80 | 20))
                {
                    var ti = stream.readTypeInfo(false);
                    var memberName = stream.readPascalString();

                    if (ti.classType == ClassType.EndOfStruct)
                        break;

                    if (ti.classType != ClassType.EnumMember)
                        throw new Exception("Unexpected class");

                    m_members.Add(memberName, typedValue.value);
                }
                else if (typedValue.type == (0x80 | 22))
                {
                    var ti = stream.readTypeInfo(true);
                    if (ti.typeDef.baseType != BaseType.Null)
                        throw new Exception($"Expected baseType={BaseType.Null}, but it's {ti.typeDef.baseType}");

                    if (ti.dims.Length != 0)
                        throw new Exception($"Expected dims=0, but it's {ti.dims.Length}");

                    if (ti.tag != name)
                        throw new Exception($"Expected name={name}, but it's {ti.tag}");

                    var tag = stream.readPascalString();
                    if (tag != ".eos")
                        throw new Exception($"Expected tag=.eos, but it's {tag}");

                    if (ti.classType != ClassType.EndOfStruct)
                        throw new Exception($"Expected classType={ClassType.EndOfStruct}, but it's {ti.classType}");

                    m_size = ti.size;
                    break;
                }
                else
                {
                    throw new Exception("Unexpected entry");
                }
            }
        }

        public bool Equals(EnumDef other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return m_members.SequenceEqual(other.m_members) && string.Equals(m_name, other.m_name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((EnumDef) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (m_members.GetHashCode() * 397) ^ m_name.GetHashCode();
            }
        }

        public void dump(IndentedTextWriter writer)
        {
            string ctype;
            switch (m_size)
            {
                case 1:
                    ctype = "char";
                    break;
                case 2:
                    ctype = "short";
                    break;
                case 4:
                    ctype = "int";
                    break;
                default:
                    throw new Exception($"Cannot determine primitive type for size {m_size}");
            }

            writer.WriteLine($"enum {m_name} : {ctype} {{");
            ++writer.indent;
            foreach (var kvp in m_members)
                writer.WriteLine($"{kvp.Key} = {kvp.Value},");
            --writer.indent;
            writer.WriteLine("};");
        }
    }
}
