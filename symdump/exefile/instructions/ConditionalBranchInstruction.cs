using System;
using System.Collections.Generic;
using symdump.exefile.expression;
using symdump.exefile.operands;
using symdump.symfile;

namespace symdump.exefile.instructions
{
    public class ConditionalBranchInstruction : Instruction
    {
        public readonly Operation operation;

        public ConditionalBranchInstruction(Operation operation, IOperand lhs, IOperand rhs, IOperand target)
        {
            this.operation = operation;
            operands = new[] {lhs, rhs, target};
        }

        public IOperand lhs => operands[0];
        public IOperand rhs => operands[1];
        public IOperand target => operands[2];

        public override IOperand[] operands { get; }

        public override string asReadable()
        {
            var op = operation.toCode();

            return $"if({lhs} {op} {rhs}) goto {target}";
        }

        public override IExpressionNode toExpressionNode(IReadOnlyDictionary<Register, IExpressionNode> registers)
        {
            throw new NotImplementedException();
        }
    }
}