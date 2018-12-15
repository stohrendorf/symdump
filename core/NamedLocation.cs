using System.Diagnostics;
using JetBrains.Annotations;

namespace core
{
    public class NamedLocation
    {
        public readonly uint GlobalAddress;

        public NamedLocation(uint globalAddress, [NotNull] string name)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));

            GlobalAddress = globalAddress;
            Name = name;
        }

        [NotNull] public string Name { get; }

        public override string ToString()
        {
            return $"0x{GlobalAddress:X} {Name}";
        }
    }
}