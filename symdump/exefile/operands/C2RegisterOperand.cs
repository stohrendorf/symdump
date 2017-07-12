using symdump.exefile.dataflow;
using symdump.exefile.disasm;
using symdump.exefile.expression;

namespace symdump.exefile.operands
{
    public class C2RegisterOperand : IOperand
    {
        public readonly C2Register register;

        public C2RegisterOperand(C2Register register)
        {
            this.register = register;
        }

        public C2RegisterOperand(uint data, int offset)
            : this((C2Register) (((int) data >> offset) & 0x1f))
        {
        }

        public bool Equals(IOperand other)
        {
            var o = other as C2RegisterOperand;
            return register == o?.register;
        }

        public IExpressionNode toExpressionNode(DataFlowState dataFlowState)
        {
            throw new System.NotImplementedException();
        }

        public override string ToString()
        {
            return $"${register}";
        }
    }
}