using System.Diagnostics;
using JetBrains.Annotations;

namespace core
{
    public class NamedLocation
    {
        public NamedLocation(uint address, [NotNull] string name)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));

            Address = address;
            Name = name;
        }

        public readonly uint Address;

        [NotNull]
        public string Name { get; }

        public override string ToString()
        {
            return $"0x{Address:X} {Name}";
        }
    }
}
