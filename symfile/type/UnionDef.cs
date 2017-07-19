using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using core;
using core.util;

namespace symfile.type
{
    public class UnionDef : IMemoryLayout, IEquatable<UnionDef>
    {
        public readonly List<StructMember> members = new List<StructMember>();
        public readonly string name;

        public uint dataSize { get; }

        public int precedence => int.MinValue;

        public string asDeclaration(string identifier, string argList)
        {
            return $"union {name}";
        }

        public UnionDef(BinaryReader stream, string name, SymFile symFile)
        {
            this.name = name;
            while (true)
            {
                var typedValue = new TypedValue(stream);
                if (typedValue.type == (0x80 | 20))
                {
                    var m = new StructMember(typedValue, stream, false, symFile);

                    if (m.typeInfo.classType == ClassType.EndOfStruct)
                    {
                        dataSize = (uint) m.typedValue.value;
                        break;
                    }

                    members.Add(m);
                }
                else if (typedValue.type == (0x80 | 22))
                {
                    var m = new StructMember(typedValue, stream, true, symFile);

                    if (m.typeInfo.classType == ClassType.EndOfStruct)
                    {
                        dataSize = (uint) m.typedValue.value;
                        break;
                    }

                    members.Add(m);
                }
                else
                {
                    throw new Exception("Unexcpected entry");
                }
            }
        }

        public bool isFake => new Regex(@"^\.\d+fake$").IsMatch(name);

        public bool Equals(UnionDef other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return members.SequenceEqual(other.members) && string.Equals(name, other.name);
        }

        public void dump(IndentedTextWriter writer)
        {
            writer.WriteLine($"union {name} {{");
            ++writer.indent;
            foreach (var m in members)
                writer.WriteLine(m);
            --writer.indent;
            writer.WriteLine("};");
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((UnionDef) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((members != null ? members.GetHashCode() : 0) * 397) ^ (name != null ? name.GetHashCode() : 0);
            }
        }

        public string getAccessPathTo(uint offset)
        {
            throw new NotImplementedException();
        }
    }
}
