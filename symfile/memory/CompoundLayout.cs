using System;
using System.Text.RegularExpressions;
using core;

namespace symfile.memory
{
    public abstract class CompoundLayout : IMemoryLayout, IEquatable<CompoundLayout>
    {
        public readonly string name;

        public bool isAnonymous => new Regex(@"^\.\d+fake$").IsMatch(name);

        public abstract uint dataSize { get; }
        public abstract int precedence { get; }
        public abstract string fundamentalType { get; }
        public abstract string asIncompleteDeclaration(string identifier, string argList);
        public abstract string getAccessPathTo(uint offset);
        public abstract IMemoryLayout pointee { get; }

        protected CompoundLayout(string name)
        {
            this.name = name;
        }

        public bool Equals(CompoundLayout other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            if (isAnonymous != other.isAnonymous)
                return false;

            return isAnonymous || string.Equals(name, other.name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CompoundLayout) obj);
        }

        public override int GetHashCode()
        {
            return (name != null && !isAnonymous ? name.GetHashCode() : 0);
        }
    }
}
