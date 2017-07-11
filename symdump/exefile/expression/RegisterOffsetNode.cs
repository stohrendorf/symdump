using symdump.symfile;

namespace symdump.exefile.expression
{
    public class RegisterOffsetNode : IExpressionNode
    {
        public readonly Register register;
        public readonly int offset;

        public RegisterOffsetNode(Register register, int offset)
        {
            this.register = register;
            this.offset = offset;
        }

        public string toCode()
        {
            return offset >= 0 ? $"${register}+{offset}" : $"${register}-{-offset}";
        }
    }
}