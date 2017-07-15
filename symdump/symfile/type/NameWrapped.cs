using System;

namespace symdump.symfile.type
{
    public class NameWrapped : IWrappedType, IEquatable<NameWrapped>
    {
        public int precedence => int.MinValue;

        public IWrappedType inner => null;
        
        public string asCode(string name, string argList)
        {
            return name;
        }

        public bool Equals(NameWrapped other)
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
    }
}
