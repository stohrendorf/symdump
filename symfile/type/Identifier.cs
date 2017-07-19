using System;
using core;

namespace symfile.type
{
    public class Identifier : IMemoryLayout, IEquatable<Identifier>
    {
        public int precedence => int.MinValue;

        public uint dataSize => 1; // TODO ???

        public string asDeclaration(string identifier, string argList)
        {
            return identifier;
        }

        public bool Equals(Identifier other)
        {
            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == this.GetType();
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public string getAccessPathTo(uint offset)
        {
            throw new NotImplementedException();
        }
    }
}
