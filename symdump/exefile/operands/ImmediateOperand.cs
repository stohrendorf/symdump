namespace symdump.exefile.operands
{
    public class ImmediateOperand(long value) : IOperand
    {
        public static readonly ImmediateOperand? Zero = new ImmediateOperand(0);

        public readonly long Value = value;

        public bool Equals(IOperand? other)
        {
            var o = other as ImmediateOperand;
            return Value == o?.Value;
        }

        public override string ToString()
        {
            return Value >= 0 ? $"0x{Value:X}" : $"-0x{-Value:X}";
        }
    }
}
