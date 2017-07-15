using symdump.exefile.instructions;
using symdump.symfile.type;

namespace symdump.exefile.expression
{
    public class ExpressionNode : IExpressionNode
    {
        public Operator @operator { get; }
        public readonly IExpressionNode lhs;
        public readonly IExpressionNode rhs;

        public ICompoundType compoundType => null;

        public ExpressionNode(Operator @operator, IExpressionNode lhs, IExpressionNode rhs)
        {
            this.@operator = @operator;
            this.lhs = lhs;
            this.rhs = rhs;
        }

        public string toCode()
        {
            var lhsCode = lhs.toCode();
            var rhsCode = rhs.toCode();

            var selfPrecedence = @operator.getPrecedence(false);
            if (selfPrecedence > (lhs as ExpressionNode)?.@operator.getPrecedence(false))
                lhsCode = $"({lhsCode})";
            
            if (selfPrecedence > (rhs as ExpressionNode)?.@operator.getPrecedence(false))
                rhsCode = $"({rhsCode})";

            return $"{lhsCode} {@operator.toCode()} {rhsCode}";
        }

        public string tryDeref()
        {
            if (@operator != Operator.Add || !(lhs is LabelNode) || !(rhs is ValueNode))
                return null;

            var sdef = (StructDef) ((LabelNode) lhs).compoundType;
            if (sdef == null)
                return null;
            
            var member = sdef.tryDeref(
                (uint) ((ValueNode)rhs).value
            );
            return ((LabelNode) lhs).label + "->" + member;
        }
    }
}
