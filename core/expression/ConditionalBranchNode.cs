﻿using core.util;

namespace core.expression
{
    public class ConditionalBranchNode : IExpressionNode
    {
        public Operator @operator { get; }
        public readonly IExpressionNode lhs;
        public readonly IExpressionNode rhs;
        public readonly LabelNode target;

        public IMemoryLayout memoryLayout => null;

        public ConditionalBranchNode(Operator @operator, IExpressionNode lhs, IExpressionNode rhs, LabelNode target)
        {
            this.@operator = @operator;
            this.lhs = lhs;
            this.rhs = rhs;
            this.target = target;
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

            return $"if({lhsCode} {@operator.toCode()} {rhsCode}) goto {target.toCode()}";
        }
    }
}
