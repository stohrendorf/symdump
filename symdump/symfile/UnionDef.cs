using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using symdump.util;

namespace symdump.symfile
{
    public class UnionDef : IEquatable<UnionDef>
    {
        private readonly List<StructMember> _members = new List<StructMember>();
        private readonly string _name;

        public UnionDef(BinaryReader stream, string name)
        {
            _name = name;
            while (true)
            {
                var typedValue = new TypedValue(stream);
                if (typedValue.Type == (0x80 | 20))
                {
                    var m = new StructMember(typedValue, stream, false);

                    if (m.TypeInfo.ClassType == ClassType.EndOfStruct)
                        break;

                    _members.Add(m);
                }
                else if (typedValue.Type == (0x80 | 22))
                {
                    var m = new StructMember(typedValue, stream, true);

                    if (m.TypeInfo.ClassType == ClassType.EndOfStruct)
                        break;

                    _members.Add(m);
                }
                else
                {
                    throw new Exception("Unexpected entry");
                }
            }
        }

        public bool IsFake => new Regex(@"^\.\d+fake$").IsMatch(_name);

        public bool Equals(UnionDef other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _members.SequenceEqual(other._members) && string.Equals(_name, other._name);
        }

        public void Dump(IndentedTextWriter writer)
        {
            writer.WriteLine($"union {_name} {{");
            ++writer.Indent;
            foreach (var m in _members)
                writer.WriteLine(m);
            --writer.Indent;
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
                return ((_members != null ? _members.GetHashCode() : 0) * 397) ^
                       (_name != null ? _name.GetHashCode() : 0);
            }
        }
    }
}
