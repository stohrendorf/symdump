using symdump.symfile;

namespace symdump.exefile.expression
{
    public class RegisterNode : IExpressionNode
    {
        public readonly Register register;

        public RegisterNode(Register register)
        {
            this.register = register;
        }

        public string toCode()
        {
            return $"${register}";
        }
    }
}