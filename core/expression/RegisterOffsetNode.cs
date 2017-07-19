namespace core.expression
{
    public class RegisterOffsetNode : IExpressionNode
    {
        public readonly int registerId;
        public readonly int offset;

        public IMemoryLayout memoryLayout { get; set; }

        public RegisterOffsetNode(int registerId, int offset)
        {
            this.registerId = registerId;
            this.offset = offset;
        }

        public string toCode()
        {
            return offset >= 0 ? $"*(${registerId}+{offset})" : $"*(${registerId}-{-offset})";
        }

        public string tryDeref()
        {
            return memoryLayout.getAccessPathTo((uint) offset);
        }
    }
}
