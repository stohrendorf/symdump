using System.Collections.Generic;
using JetBrains.Annotations;

namespace core
{
    public abstract class Instruction
    {
        public bool IsBranchDelaySlot;

        [NotNull]
        [ItemNotNull]
        public abstract IOperand[] Operands { get; }

        [NotNull]
        public abstract string AsReadable();

        [NotNull]
        public abstract IExpressionNode ToExpressionNode([NotNull] IDataFlowState dataFlowState);

        [CanBeNull]
        public abstract uint? JumpTarget { get; }

        [NotNull]
        public abstract IEnumerable<int> InputRegisters { get; }
        
        [NotNull]
        public abstract IEnumerable<int> OutputRegisters { get; }
    }
}
