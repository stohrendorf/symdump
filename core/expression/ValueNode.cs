﻿namespace core.expression
{
    public class ValueNode : IExpressionNode
    {
        public readonly long value;

        public IMemoryLayout memoryLayout => null;

        public ValueNode(long value)
        {
            this.value = value;
        }

        public string toCode()
        {
            return value.ToString();
        }
    }
}