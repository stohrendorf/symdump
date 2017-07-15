using symdump.symfile;
using symdump.symfile.type;

namespace symdump.exefile.expression
{
    public interface IExpressionNode
    {
        string toCode();

        ICompoundType compoundType { get; }
    }
}
