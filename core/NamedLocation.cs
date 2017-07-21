using System.Diagnostics;
using JetBrains.Annotations;

namespace core
{
    public class NamedLocation
    {
        public NamedLocation(uint address, [NotNull] string name)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));

            this.address = address;
            this.name = name;
        }

        public readonly uint address;

        [NotNull]
        public string name { get; }

        public override string ToString()
        {
            return $"0x{address:X} {name}";
        }
    }
}