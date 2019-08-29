using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using symdump.symfile.util;
using symdump.util;

namespace symdump.symfile
{
    public class UnionDef : IEquatable<UnionDef>
    {
        private readonly List<StructMember> _members = new List<StructMember>();
        public string Name;

        public UnionDef(BinaryReader stream, string name)
        {
            Name = name;
            while (true)
            {
                var typedValue = new TypedValue(stream);
                if (typedValue.Type == (0x80 | TypedValue.Definition))
                {
                    var m = new StructMember(typedValue, stream, false);

                    if (m.MemberType.Type == SymbolType.EndOfStruct)
                        break;

                    _members.Add(m);
                }
                else if (typedValue.Type == (0x80 | TypedValue.ArrayDefinition))
                {
                    var m = new StructMember(typedValue, stream, true);

                    if (m.MemberType.Type == SymbolType.EndOfStruct)
                        break;

                    _members.Add(m);
                }
                else
                {
                    throw new Exception("Unexpected entry");
                }
            }
        }

        public bool IsFake => Name.IsFake();

        public bool Equals(UnionDef other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _members.SequenceEqual(other._members) && string.Equals(Name, other.Name);
        }

        public void ApplyInline(IDictionary<string, EnumDef> enums, IDictionary<string, StructDef> structs,
            IDictionary<string, UnionDef> unions)
        {
            foreach (var member in _members) member.ApplyInline(enums, structs, unions);
        }

        public void Dump(IndentedTextWriter writer, bool forInline)
        {
            writer.WriteLine(forInline ? "union {" : $"union {Name} {{");
            ++writer.Indent;
            foreach (var m in _members)
                writer.WriteLine(m);
            --writer.Indent;
            if (forInline)
                writer.Write("}");
            else
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
                       (Name != null ? Name.GetHashCode() : 0);
            }
        }
    }
}
