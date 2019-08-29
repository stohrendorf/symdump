using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using symdump.symfile.util;
using symdump.util;

namespace symdump.symfile
{
    public class StructDef : IEquatable<StructDef>
    {
        private readonly List<StructMember> _members = new List<StructMember>();
        private readonly string _name;

        public StructDef(BinaryReader stream, string name)
        {
            _name = name;
            while (true)
            {
                var typedValue = new TypedValue(stream);

                StructMember member;
                if (typedValue.Type == (0x80 | TypedValue.Definition))
                    member = new StructMember(typedValue, stream, false);
                else if (typedValue.Type == (0x80 | TypedValue.ArrayDefinition))
                    member = new StructMember(typedValue, stream, true);
                else
                    throw new Exception("Unexpected entry");

                if (member.MemberType.Type == SymbolType.EndOfStruct)
                    break;

                _members.Add(member);
            }
        }

        public bool IsFake => _name.IsFake();

        public bool Equals(StructDef other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _members.SequenceEqual(other._members) && string.Equals(_name, other._name);
        }

        public void ApplyInline(IDictionary<string, EnumDef> enums, IDictionary<string, StructDef> structs,
            IDictionary<string, UnionDef> unions)
        {
            foreach (var member in _members) member.ApplyInline(enums, structs, unions);
        }

        public void Dump(IndentedTextWriter writer, bool forInline)
        {
            writer.WriteLine(forInline ? "struct {" : $"struct {_name} {{");
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
            return Equals((StructDef) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((_members != null ? _members.GetHashCode() : 0) * 397) ^ (_name?.GetHashCode() ?? 0);
            }
        }

        public StructMember ForOffset(uint ofs)
        {
            return _members
                .Where(m => m.MemberType.Type != SymbolType.Bitfield && m.TypedValue.Value <= ofs)
                .OrderBy(m => m.TypedValue.Value)
                .FirstOrDefault();
        }
    }
}
