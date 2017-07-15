using symdump.symfile;
using symdump.symfile.type;

namespace symdump.exefile.expression
{
    public class RegisterNode : IExpressionNode
    {
        public readonly Register register;

        public ICompoundType compoundType => null;

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
