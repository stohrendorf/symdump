using core.util;
using JetBrains.Annotations;

namespace core
{
    public interface IDataFlowState
    {
        [CanBeNull] IDebugSource DebugSource { get; }
        
        [CanBeNull] IExpressionNode GetRegisterExpression(int registerId);

        bool Apply([NotNull] Instruction insn, [CanBeNull] Instruction nextInsn);

        void DumpState(IndentedTextWriter writer);
    }
}
