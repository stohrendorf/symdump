using core;
using mips.disasm;
using mips.operands;

namespace mips.instructions
{
    public class CallPtrInstruction : Instruction
    {
        public override IOperand[] operands { get; }

        public IOperand target => operands[0];
        public RegisterOperand returnAddressTarget => (RegisterOperand) (operands.Length > 1 ? operands[1] : null);

        public CallPtrInstruction(IOperand target, RegisterOperand returnAddressTarget)
        {
            operands = returnAddressTarget == null ? new[] {target} : new[] {target, returnAddressTarget};
        }

        public override string asReadable()
        {
            if (returnAddressTarget != null)
            {
                return returnAddressTarget.register == Register.ra ? $"{target}()" : $"${returnAddressTarget} = __RET_ADDR; {target}()";
            }

            return $"goto {target}";
        }

        public override IExpressionNode toExpressionNode(IDataFlowState dataFlowState)
        {
            throw new System.NotImplementedException();
        }
    }
}
