using symdump.symfile;
using symdump.symfile.type;

namespace symdump.exefile.expression
{
    public class RegisterNode : IExpressionNode
    {
        public readonly Register register;

        public ITypeDefinition typeDefinition => null;

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
