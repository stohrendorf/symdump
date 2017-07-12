using System;
using symdump.exefile.dataflow;
using symdump.exefile.expression;

namespace symdump.exefile.operands
{
    public interface IOperand : IEquatable<IOperand>
    {
        IExpressionNode toExpressionNode(DataFlowState dataFlowState);
    }
}