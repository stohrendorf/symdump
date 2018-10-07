namespace symdump.exefile.operands
{
    public class ImmediateOperand : IOperand
    {
        private readonly long _value;

        public ImmediateOperand(long value)
        {
            _value = value;
        }

        public bool Equals(IOperand other)
        {
            var o = other as ImmediateOperand;
            return _value == o?._value;
        }

        public override string ToString()
        {
            return _value >= 0 ? $"0x{_value:X}" : $"-0x{-_value:X}";
        }
    }
}
