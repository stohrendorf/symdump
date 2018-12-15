using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using core.util;
using symfile.type;

namespace symfile.memory
{
    public sealed class UnionLayout : CompoundLayout, IEquatable<UnionLayout>
    {
        private readonly List<CompoundMember> _members = new List<CompoundMember>();

        public UnionLayout(BinaryReader stream, string name, SymFile debugSource)
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

                        _members.Add(m);
                    }
                    else if (typedValue.Type == (0x80 | 22))
                    {
                        var m = new CompoundMember(typedValue, stream, true, debugSource);

                        if (m.TypeDecoration.ClassType == ClassType.EndOfStruct)
                        {
                            DataSize = (uint) m.FileEntry.Value;
                            break;
                        }

                        _members.Add(m);
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

        public override string FundamentalType => $"union {Name}";

        public override uint DataSize { get; }

        public override int Precedence => int.MinValue;

        public bool Equals(UnionLayout other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && _members.SequenceEqual(other._members) && DataSize == other.DataSize;
        }

        public override string AsIncompleteDeclaration(string identifier, string argList)
        {
            return identifier;
        }

        public void Dump(IndentedTextWriter writer)
        {
            writer.WriteLine($"union {Name} {{");
            ++writer.Indent;
            foreach (var m in _members)
                writer.WriteLine(m);
            --writer.Indent;
            writer.WriteLine("};");
        }

        public override string GetAccessPathTo(uint offset)
        {
            throw new NotImplementedException();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((UnionLayout) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ (_members != null ? _members.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int) DataSize;
                return hashCode;
            }
        }
    }
}