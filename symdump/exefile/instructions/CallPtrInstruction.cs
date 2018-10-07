using symdump.exefile.operands;
using symdump.symfile;

namespace symdump.exefile.instructions
{
    public class CallPtrInstruction : Instruction
    {
        public override IOperand[] Operands { get; }

        private IOperand Target => Operands[0];
        private RegisterOperand ReturnAddressTarget => (RegisterOperand) (Operands.Length > 1 ? Operands[1] : null);

        public CallPtrInstruction(IOperand target, IOperand returnAddressTarget)
        {
            Operands = returnAddressTarget == null ? new[] {target} : new[] {target, returnAddressTarget};
        }

        public override string AsReadable()
        {
            if (ReturnAddressTarget != null)
            {
                return ReturnAddressTarget.Register == Register.ra
                    ? $"{Target}()"
                    : $"{ReturnAddressTarget} = __RET_ADDR; {Target}()";
            }

            return $"goto {Target}";
        }
    }
}
