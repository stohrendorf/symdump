using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using core;
using core.util;
using symfile.type;

namespace symfile.memory
{
    public class StructLayout : CompoundLayout, IEquatable<StructLayout>
    {
        public readonly List<CompoundMember> Members = new List<CompoundMember>();

        public override string FundamentalType => $"struct {Name}";

        public override uint DataSize { get; }

        public override int Precedence => int.MinValue;

        public override IMemoryLayout Pointee => null;

        public StructLayout(BinaryReader stream, string name, SymFile debugSource)
            : base(name)
        {
            debugSource.CurrentlyDefining.Add(name, this);

            try
            {
                while (true)
                {
                    var typedValue = new FileEntry(stream);
                    if (typedValue.Type == (0x80 | 20))
                    {
                        var m = new CompoundMember(typedValue, stream, false, debugSource);

                        if (m.TypeDecoration.ClassType == ClassType.EndOfStruct)
                        {
                            DataSize = (uint) m.FileEntry.Value;
                            break;
                        }

                        Members.Add(m);
                    }
                    else if (typedValue.Type == (0x80 | 22))
                    {
                        var m = new CompoundMember(typedValue, stream, true, debugSource);

                        if (m.TypeDecoration.ClassType == ClassType.EndOfStruct)
                        {
                            DataSize = (uint) m.FileEntry.Value;
                            break;
                        }

                        Members.Add(m);
                    }
                    else
                    {
                        throw new Exception("Unexpected entry");
                    }
                }
            }
            finally
            {
                debugSource.CurrentlyDefining.Remove(name);
            }
        }

        public override string AsIncompleteDeclaration(string identifier, string argList)
        {
            return identifier;
        }

        public override string ToString()
        {
            return Name;
        }

        public void Dump(IndentedTextWriter writer)
        {
            writer.WriteLine($"struct {Name} {{");
            ++writer.Indent;
            foreach (var m in Members)
                writer.WriteLine(m);
            --writer.Indent;
            writer.WriteLine("};");
        }

        public override string GetAccessPathTo(uint ofs)
        {
            var member = Members
                .LastOrDefault(m => m.TypeDecoration.ClassType != ClassType.Bitfield && m.FileEntry.Value <= ofs);

            if (member == null)
                return null;

            if (member.MemoryLayout == null)
                return member.Name;

            ofs -= (uint) member.FileEntry.Value;
            var memberAccessPath = member.MemoryLayout.GetAccessPathTo(ofs);
            if (memberAccessPath == null)
                return member.Name;
            
            if(member.MemoryLayout is Array)
                return member.Name + memberAccessPath;

            return member.Name + "." + memberAccessPath;
        }

        public bool Equals(StructLayout other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && Members.SequenceEqual(other.Members) && DataSize == other.DataSize;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((StructLayout) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ (Members != null ? Members.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int) DataSize;
                return hashCode;
            }
        }
    }
}
