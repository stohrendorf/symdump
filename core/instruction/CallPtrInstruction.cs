using System.Collections.Generic;
using System.Linq;
using core.operand;

namespace core.instruction
{
    public class CallPtrInstruction : Instruction
    {
        public override IOperand[] Operands { get; }

        public IOperand Target => Operands[0];
        public RegisterOperand ReturnAddressTarget => (RegisterOperand) (Operands.Length > 1 ? Operands[1] : null);

        public override uint? JumpTarget => (Target as LabelOperand)?.Address;

        public override IEnumerable<int> InputRegisters
            => Target.TouchedRegisters;

        public override IEnumerable<int> OutputRegisters
            => ReturnAddressTarget?.TouchedRegisters ?? Enumerable.Empty<int>();

        // ReSharper disable once SuggestBaseTypeForParameter
        public CallPtrInstruction(IOperand target, RegisterOperand returnAddressTarget)
        {
            Operands = returnAddressTarget == null ? new[] {target} : new[] {target, returnAddressTarget};
        }

        public override string AsReadable()
        {
            if (ReturnAddressTarget != null)
            {
                return $"{ReturnAddressTarget} = __RET_ADDR; {Target}()";
            }

            return $"goto {Target}";
        }

        public override IExpressionNode ToExpressionNode(IDataFlowState dataFlowState)
        {
            throw new System.NotImplementedException();
        }
    }
}
