using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using symdump.symfile.util;
using symdump.util;

namespace symdump.symfile
{
    public class StructDef : IEquatable<StructDef>, IComplexType
    {
        private readonly List<CompoundMember> _members = new List<CompoundMember>();

        public StructDef(BinaryReader stream, string name)
        {
            Name = name;
            while (true)
            {
                var typedValue = new TypedValue(stream);

                CompoundMember member;
                if (typedValue.Type == (0x80 | TypedValue.Definition))
                    member = new CompoundMember(typedValue, stream, false);
                else if (typedValue.Type == (0x80 | TypedValue.ArrayDefinition))
                    member = new CompoundMember(typedValue, stream, true);
                else
                    throw new Exception("Unexpected entry");

                if (member.MemberType.Type == SymbolType.EndOfStruct)
                    break;

                _members.Add(member);
            }
        }

        public string Name { get; }
        public bool IsFake => Name.IsFake();
        public IDictionary<string, TaggedSymbol> Typedefs { get; set; } = new SortedDictionary<string, TaggedSymbol>();
        public bool Inlined { get; set; }

        public void Dump(IndentedTextWriter writer, bool forInline)
        {
            if (forInline && Typedefs.Count > 0)
            {
                writer.Write(string.Join(", ", Typedefs.Select(_ => _.Value.AsCode(_.Key, true))));
                return;
            }

            writer.WriteLine(forInline ? "struct {" : $"struct {Name} {{");
            ++writer.Indent;
            foreach (var m in _members)
                writer.WriteLine(m);
            --writer.Indent;
            if (forInline)
                writer.Write("}");
            else
                writer.WriteLine("};");
        }

        public void ResolveTypedefs(ObjectFile objectFile)
        {
            foreach (var member in _members) member.ResolveTypedef(objectFile);
        }

        public bool Equals(StructDef other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _members.SequenceEqual(other._members) && string.Equals(Name, other.Name);
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
                return ((_members != null ? _members.GetHashCode() : 0) * 397) ^ (Name?.GetHashCode() ?? 0);
            }
        }
    }
}
