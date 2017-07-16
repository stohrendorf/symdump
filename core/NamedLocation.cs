namespace core
{
    public class NamedLocation
    {
        public NamedLocation(uint address, string name)
        {
            this.address = address;
            this.name = name;
        }

        public readonly uint address;

        public string name { get; }

        public override string ToString()
        {
            return $"0x{address:X} {name}";
        }
    }
}
