using System.Diagnostics;

namespace core.microcode
{
    public interface IMicroArg
    {
        /// <summary>
        ///     Size of this argument.
        /// </summary>
        byte Bits { get; }
    }

    public sealed class RegisterMemArg : IMicroArg
    {
        public readonly int Offset;

        public RegisterMemArg(uint register, int offset, byte bits)
        {
            Register = register;
            Offset = offset;
            Bits = bits;
        }

        public uint Register { get; }
        public byte Bits { get; }

        public override string ToString()
        {
            return $"$r{Register}(0x{Offset:X})";
        }
    }

    public sealed class RegisterArg : IMicroArg
    {
        public RegisterArg(uint register, byte bits)
        {
            Register = register;
            Bits = bits;
        }

        public uint Register { get; }
        public byte Bits { get; }

        public override string ToString()
        {
            return $"$r{Register}";
        }
    }

    public sealed class AddressValue : IMicroArg
    {
        private readonly string _name;
        public readonly ulong Address;

        public AddressValue(ulong address, string name)
        {
            _name = name;
            Address = address;
        }

        public byte Bits => 32;

        public override string ToString()
        {
            return $"0x{Address:X}[[{_name ?? "?"}]]";
        }

        public Deref Deref(byte bits)
        {
            return new Deref(this, bits);
        }
    }

    public sealed class Deref : IMicroArg
    {
        public readonly AddressValue Address;

        public Deref(AddressValue address, byte bits)
        {
            Debug.Assert(bits > 0);
            Address = address;
            Bits = bits;
        }

        public byte Bits { get; }

        public override string ToString()
        {
            return $"*{Address}<<{Bits}>>";
        }
    }

    public sealed class ConstValue : IMicroArg
    {
        public readonly ulong Value;

        public ConstValue(ulong value, byte bits)
        {
            var mask = (1ul << bits) - 1;
            Value = value & mask;
            Bits = bits;
        }

        private bool Signed => (Value & (1ul << (Bits - 1))) != 0;
        public byte Bits { get; }

        public override string ToString()
        {
            return $"0x{Value:X}<<{Bits}>>";
        }

        public ConstValue SignedResized(byte toBits)
        {
            if (toBits <= Bits)
                return new ConstValue(Value, toBits);

            var ext = Signed ? ulong.MaxValue << (toBits - 1) : 0;
            return new ConstValue(Value | ext, toBits);
        }
    }
}