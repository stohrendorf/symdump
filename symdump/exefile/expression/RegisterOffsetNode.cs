using symdump.symfile;
using symdump.symfile.type;

namespace symdump.exefile.expression
{
    public class RegisterOffsetNode : IExpressionNode
    {
        public readonly Register register;
        public readonly int offset;

        public ITypeDefinition typeDefinition { get; set; }

        public RegisterOffsetNode(Register register, int offset)
        {
            this.register = register;
            this.offset = offset;
        }

        public string toCode()
        {
            return offset >= 0 ? $"*(${register}+{offset})" : $"*(${register}-{-offset})";
        }

        public string tryDeref()
        {
            if (typeDefinition is StructDef)
                return ((StructDef) typeDefinition).forOffset((uint) offset).ToString();

            return null;
        }
    }
}
