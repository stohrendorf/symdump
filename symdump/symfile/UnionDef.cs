using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using symdump;
using symfile.util;

namespace symfile
{
    public class UnionDef : IEquatable<UnionDef>
    {
        public readonly List<StructMember> members = new List<StructMember>();
        public readonly string name;

        public bool isFake => new Regex(@"^\.\d+fake$").IsMatch(name);

        public UnionDef(BinaryReader stream, string name)
        {
            this.name = name;
            while (true)
            {
                var typedValue = new TypedValue(stream);
                if (typedValue.type == (0x80 | 20))
                {
                    var m = new StructMember(typedValue, stream, false);

                    if (m.typeInfo.classType == ClassType.EndOfStruct)
                        break;

                    members.Add(m);
                }
                else if (typedValue.type == (0x80 | 22))
                {
                    var m = new StructMember(typedValue, stream, true);

                    if (m.typeInfo.classType == ClassType.EndOfStruct)
                        break;

                    members.Add(m);
                }
                else
                {
                    throw new Exception("Unexcpected entry");
                }
            }
        }

        public void dump(IndentedTextWriter writer)
        {
            writer.WriteLine($"union {name} {{");
            ++writer.Indent;
            foreach (var m in members)
                writer.WriteLine(m);
            --writer.Indent;
            writer.WriteLine("};");
        }

        public bool Equals(UnionDef other)
        {
            if(ReferenceEquals(null, other)) return false;
            if(ReferenceEquals(this, other)) return true;
            return members.SequenceEqual(other.members) && string.Equals(name, other.name);
        }

        public override bool Equals(object obj)
        {
            if(ReferenceEquals(null, obj)) return false;
            if(ReferenceEquals(this, obj)) return true;
            if(obj.GetType() != this.GetType()) return false;
            return Equals((UnionDef)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((members != null ? members.GetHashCode() : 0) * 397) ^ (name != null ? name.GetHashCode() : 0);
            }
        }
    }
}