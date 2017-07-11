using System;
using System.Collections.Generic;
using symdump.exefile.expression;
using symdump.exefile.operands;
using symdump.symfile;

namespace symdump.exefile.instructions
{
    public class WordData : Instruction
    {
        public readonly uint data;

        public WordData(uint data)
        {
            this.data = data;
        }

        public override IOperand[] operands { get; } = new IOperand[0];

        public override string ToString()
        {
            return $".word 0x{data:x}";
        }

        public override string asReadable()
        {
            return ToString();
        }

        public override IExpressionNode toExpressionNode(IReadOnlyDictionary<Register, IExpressionNode> registers)
        {
            throw new Exception("Cannot convert word data to expression node");
        }
    }
}