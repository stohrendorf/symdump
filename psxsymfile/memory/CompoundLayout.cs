using System;
using System.Text.RegularExpressions;
using core;

namespace symfile.memory
{
    public abstract class CompoundLayout : IMemoryLayout, IEquatable<CompoundLayout>
    {
        protected readonly string Name;

        protected CompoundLayout(string name)
        {
            Name = name;
        }

        public bool IsAnonymous => new Regex(@"^\.\d+fake$").IsMatch(Name);

        public bool Equals(CompoundLayout other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            if (IsAnonymous != other.IsAnonymous)
                return false;

            return IsAnonymous || string.Equals(Name, other.Name);
        }

        public abstract uint DataSize { get; }
        public abstract int Precedence { get; }
        public abstract string FundamentalType { get; }
        public abstract string AsIncompleteDeclaration(string identifier, string argList);
        public abstract string GetAccessPathTo(uint offset);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((CompoundLayout) obj);
        }

        public override int GetHashCode()
        {
            return Name != null && !IsAnonymous ? Name.GetHashCode() : 0;
        }
    }
}