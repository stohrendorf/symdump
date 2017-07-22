using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using core;
using core.util;
using symfile.type;

namespace symfile.memory
{
    public class StructLayout : CompoundLayout, IEquatable<StructLayout>
    {
        public readonly List<CompoundMember> members = new List<CompoundMember>();

        public override string fundamentalType => $"struct {name}";

        public override uint dataSize { get; }

        public override int precedence => int.MinValue;

        public override IMemoryLayout pointee => null;

        public StructLayout(BinaryReader stream, string name, SymFile debugSource)
            : base(name)
        {
            debugSource.currentlyDefining.Add(name, this);

            try
            {
                while (true)
                {
                    var typedValue = new FileEntry(stream);
                    if (typedValue.type == (0x80 | 20))
                    {
                        var m = new CompoundMember(typedValue, stream, false, debugSource);

                        if (m.typeDecoration.classType == ClassType.EndOfStruct)
                        {
                            dataSize = (uint) m.fileEntry.value;
                            break;
                        }

                        members.Add(m);
                    }
                    else if (typedValue.type == (0x80 | 22))
                    {
                        var m = new CompoundMember(typedValue, stream, true, debugSource);

                        if (m.typeDecoration.classType == ClassType.EndOfStruct)
                        {
                            dataSize = (uint) m.fileEntry.value;
                            break;
                        }

                        members.Add(m);
                    }
                    else
                    {
                        throw new Exception("Unexpected entry");
                    }
                }
            }
            finally
            {
                debugSource.currentlyDefining.Remove(name);
            }
        }

        public override string asIncompleteDeclaration(string identifier, string argList)
        {
            return identifier;
        }

        public override string ToString()
        {
            return name;
        }

        public void dump(IndentedTextWriter writer)
        {
            writer.WriteLine($"struct {name} {{");
            ++writer.indent;
            foreach (var m in members)
                writer.WriteLine(m);
            --writer.indent;
            writer.WriteLine("};");
        }

        public override string getAccessPathTo(uint ofs)
        {
            var member = members
                .LastOrDefault(m => m.typeDecoration.classType != ClassType.Bitfield && m.fileEntry.value <= ofs);

            if (member == null)
                return null;

            if (member.memoryLayout == null)
                return member.name;

            ofs -= (uint) member.fileEntry.value;
            var memberAccessPath = member.memoryLayout.getAccessPathTo(ofs);
            if (memberAccessPath == null)
                return member.name;
            
            if(member.memoryLayout is Array)
                return member.name + memberAccessPath;

            return member.name + "." + memberAccessPath;
        }

        public bool Equals(StructLayout other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && members.SequenceEqual(other.members) && dataSize == other.dataSize;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((StructLayout) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ (members != null ? members.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int) dataSize;
                return hashCode;
            }
        }
    }
}
