using symdump.exefile.instructions;
using symdump.symfile.type;

namespace symdump.exefile.expression
{
    public class ConditionalBranchNode : IExpressionNode
    {
        public Operation operation { get; }
        public readonly IExpressionNode lhs;
        public readonly IExpressionNode rhs;
        public readonly LabelNode target;

        public ITypeDefinition typeDefinition => null;

        public ConditionalBranchNode(Operation operation, IExpressionNode lhs, IExpressionNode rhs, LabelNode target)
        {
            this.operation = operation;
            this.lhs = lhs;
            this.rhs = rhs;
            this.target = target;
        }

        public string toCode()
        {
            var lhsCode = lhs.toCode();
            var rhsCode = rhs.toCode();

            var selfPrecedence = operation.getPrecedence();
            if (lhs is ExpressionNode)
            {
                if (selfPrecedence > ((ExpressionNode) lhs).operation.getPrecedence())
                    lhsCode = $"({lhsCode})";
            }

            if (rhs is ExpressionNode)
            {
                if (selfPrecedence > ((ExpressionNode) rhs).operation.getPrecedence())
                    rhsCode = $"({rhsCode})";
            }

            return $"if({lhsCode} {operation.toCode()} {rhsCode}) goto {target.toCode()}";
        }
    }
}
