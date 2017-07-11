using System;
using System.Collections.Generic;
using symdump.exefile.expression;
using symdump.symfile;

namespace symdump.exefile.operands
{
    public interface IOperand : IEquatable<IOperand>
    {
        IExpressionNode toExpressionNode(IReadOnlyDictionary<Register, IExpressionNode> registers);
    }
}