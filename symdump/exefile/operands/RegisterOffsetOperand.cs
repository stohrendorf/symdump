using System.Collections.Generic;
using symdump.exefile.dataflow;
using symdump.exefile.expression;
using symdump.exefile.instructions;
using symdump.symfile;

namespace symdump.exefile.operands
{
    public class RegisterOffsetOperand : IOperand
    {
        public readonly int offset;
        public readonly Register register;

        public RegisterOffsetOperand(Register register, int offset)
        {
            this.register = register;
            this.offset = offset;
        }

        public RegisterOffsetOperand(uint data, int shift, int offset)
            : this((Register) ((data >> shift) & 0x1f), offset)
        {
        }

        public bool Equals(IOperand other)
        {
            var o = other as RegisterOffsetOperand;
            return register == o?.register && offset == o.offset;
        }

        public IExpressionNode toExpressionNode(DataFlowState dataFlowState)
        {
            var expression = dataFlowState.getRegisterExpression(register);
            if (expression != null)
            {
                return new DerefNode(new ExpressionNode(Operation.Add, expression, new ValueNode(offset)));
            }
            
            return new RegisterOffsetNode(register, offset);
        }

        public override string ToString()
        {
            return $"{offset}(${register})";
        }
    }
}
