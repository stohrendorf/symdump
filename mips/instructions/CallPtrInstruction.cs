using core;
using mips.disasm;
using mips.operands;

namespace mips.instructions
{
    public class CallPtrInstruction : Instruction
    {
        public override IOperand[] Operands { get; }

        public IOperand Target => Operands[0];
        public RegisterOperand ReturnAddressTarget => (RegisterOperand) (Operands.Length > 1 ? Operands[1] : null);

        public override uint? JumpTarget => (Target as LabelOperand)?.Address;

        // ReSharper disable once SuggestBaseTypeForParameter
        public CallPtrInstruction(IOperand target, RegisterOperand returnAddressTarget)
        {
            Operands = returnAddressTarget == null ? new[] {target} : new[] {target, returnAddressTarget};
        }

        public override string AsReadable()
        {
            if (ReturnAddressTarget != null)
            {
                return ReturnAddressTarget.Register == Register.ra ? $"{Target}()" : $"${ReturnAddressTarget} = __RET_ADDR; {Target}()";
            }

            return $"goto {Target}";
        }

        public override IExpressionNode ToExpressionNode(IDataFlowState dataFlowState)
        {
            throw new System.NotImplementedException();
        }
    }
}
