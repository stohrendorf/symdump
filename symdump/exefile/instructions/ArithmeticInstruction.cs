using System;
using System.Collections.Generic;
using symdump.exefile.expression;
using symdump.exefile.operands;
using symdump.symfile;

namespace symdump.exefile.instructions
{
    public class ArithmeticInstruction : Instruction
    {
        public readonly Operation operation;

        public ArithmeticInstruction(Operation operation, IOperand dest, IOperand lhs, IOperand rhs)
        {
            this.operation = operation;
            operands = new[] {dest, lhs, rhs};
        }

        public override IOperand[] operands { get; }

        public IOperand destination => operands[0];
        public IOperand lhs => operands[1];
        public IOperand rhs => operands[2];

        public bool isInplace => destination.Equals(lhs);

        public override string asReadable()
        {
            var op = operation.toCode();

            return isInplace
                ? $"{destination} {op}= {rhs}"
                : $"{destination} = {lhs} {op} {rhs}";
        }

        public override IExpressionNode toExpressionNode(IReadOnlyDictionary<Register, IExpressionNode> registers)
        {
            return new ExpressionNode(operation, lhs.toExpressionNode(registers), rhs.toExpressionNode(registers));
        }
    }
}