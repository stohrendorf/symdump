using System;
using System.Collections.Generic;
using System.Linq;
using core;

namespace mips.instructions
{
    public class WordData : Instruction
    {
        public readonly uint Data;

        public override uint? JumpTarget => null;
        public override IEnumerable<int> InputRegisters => Enumerable.Empty<int>();
        public override IEnumerable<int> OutputRegisters => Enumerable.Empty<int>();

        public WordData(uint data)
        {
            Data = data;
        }

        public override IOperand[] Operands { get; } = new IOperand[0];

        public override string ToString()
        {
            return $".word 0x{Data:x}";
        }

        public override string AsReadable()
        {
            return ToString();
        }

        public override IExpressionNode ToExpressionNode(IDataFlowState dataFlowState)
        {
            throw new Exception("Cannot convert word data to expression node");
        }
    }
}
