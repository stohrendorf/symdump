using symdump.symfile.type;

namespace symdump.exefile.expression
{
    public class LabelNode : IExpressionNode
    {
        public readonly string label;

        public ITypeDefinition typeDefinition { get; }

        public LabelNode(string label, ITypeDefinition typeDefinition)
        {
            this.label = label;
            this.typeDefinition = typeDefinition;
        }

        public string toCode()
        {
            return typeDefinition == null ? label : typeDefinition.ToString();
        }
    }
}
